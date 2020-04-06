using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Internationalization.Exception;
using Internationalization.FileProvider.Interface;
using Internationalization.Model;
using Internationalization.Utilities;
using Microsoft.Extensions.Logging;
using ExcelInterop = Microsoft.Office.Interop.Excel;

namespace Internationalization.FileProvider.Excel {

    /// <summary>
    /// Saves its files using an Excel Application.
    /// This Excel Application will under normal circumstances always be closed,
    /// if however the execution is aborted (because of an exception or the stop
    /// debugging button) while an Excel process is still running,
    /// it will stick around and will need to be terminated using the Task Manager.
    /// </summary>
    public class ExcelFileProvider : IFileProvider
    {

        private static ILogger _logger;

        private readonly Dictionary<CultureInfo, Dictionary<string, string>> _dictOfDicts = new Dictionary<CultureInfo, Dictionary<string, string>>();

        // 0: initialization is not yet started or completed
        // 1: initialization is already started and running
        private int _isInitializing;
        private BackgroundWorker _backgroundWorker;
        private int _numKeyParts;
        private readonly string _glossaryTag;

        /// <summary>Saves file as Excel, a backup will be created before the file is edited</summary>
        /// <param name="translationFilePath">File that will be worked on being worked on</param>
        /// <param name="glossaryTag">
        /// Entries in the Excel table that start with this tag will be interpreted as part of the glossary
        /// </param>
        /// <param name="oldTranslationFilePath">A copy of the original sheet will be put here if no copy exists jet</param>
        public ExcelFileProvider(string translationFilePath, string glossaryTag = null, string oldTranslationFilePath = null)
        {
            Status = ProviderStatus.InitializationInProgress;

            _logger = GlobalSettings.LibraryLoggerFactory.CreateLogger<ExcelFileProvider>();
            _logger.Log(LogLevel.Trace, "Initializing ExcelFileProvider.");
            _glossaryTag = glossaryTag;

            TranslationFilePath = InspectPath(translationFilePath);
            OldTranslationFilePath = oldTranslationFilePath;
            
            Initialize();
        }

        public ProviderStatus Status { get; private set; }

        public Dictionary<CultureInfo, Dictionary<string, string>> GetDictionary()
        {
            if (Status != ProviderStatus.Initialized)
            {
                _logger.Log(LogLevel.Error, "Dictionary was accessed without ExcelFileProvider being initialized.");
                throw new FileProviderNotInitializedException();
            }

            return _dictOfDicts;
        }

        public void Update(string key, IEnumerable<TextLocalization> texts)
        {
            //in order to guarantee only one enumeration even if ExcelCreateFirst needs to be called
            IList<TextLocalization> textsEnumerated = texts.ToList();

            string textsString = string.Join(", ", textsEnumerated.Select(l => l.ToString()));
            _logger.Log(LogLevel.Trace, $"Update was called with {{{textsString}}} as translations for key ({key}).");

            foreach (TextLocalization textLocalization in textsEnumerated)
            {
                _dictOfDicts.TryGetValue(textLocalization.Language, out Dictionary<string, string> langDict);
                if (langDict == null)
                {
                    langDict = new Dictionary<string, string>();
                    _dictOfDicts.Add(textLocalization.Language, langDict);
                    _logger.Log(LogLevel.Trace, $"New language dictionary was created for {textLocalization.Language.EnglishName}.");
                }

                if (langDict.ContainsKey(key))
                {
                    langDict.Remove(key);
                    _logger.Log(LogLevel.Trace, "Updated existing entry for given value.");
                }
                else
                {
                    _logger.Log(LogLevel.Trace, "Created new entry for given value.");
                }
                langDict.Add(key, textLocalization.Text);
            }

            //if file was created by ExcelFileProvider itself
            if (Status == ProviderStatus.InitializationInProgress && _isInitializing == 0)
            {
                _logger.Log(LogLevel.Debug, "First update after empty sheet was created.");
                ExcelCreateFirst(key, textsEnumerated);

                //not great I know
                GC.Collect();
            }
        }

        public void SaveDictionary()
        {
            _logger.Log(LogLevel.Trace, "SaveDictionary was called.");
            ExcelWriteActions();

            //not great I know
            GC.Collect();
        }

        public void CancelInitialization()
        {
            if (_backgroundWorker == null) return;

            if (_backgroundWorker.IsBusy)
            {
                Status = ProviderStatus.CancellationInProgress;
                _backgroundWorker.CancelAsync();
            }
        }

        private string TranslationFilePath {
            get;
        }

        private string OldTranslationFilePath {
            get;
        }

        private void ExcelCreateNew(string path)
        {
            ExcelInterop.Application excel = new ExcelInterop.Application();
            ExcelInterop.Workbook workbook = excel.Workbooks.Add();

            bool fail = true;
            try
            {
                excel.DisplayAlerts = false;
                workbook.SaveAs(Path.GetFullPath(path));
                fail = false;
                _logger.Log(LogLevel.Debug, $"Successfully created empty excel file ({path}).");
            }
            catch
            {
                _logger.Log(LogLevel.Warning, $"Unable to create empty excel file ({path}).");
            }
            finally
            {
                workbook.Close();
                excel.Quit();

                if (!fail)
                {
                    //to siganl that even with Status == IsInitaializing, no more reading is needed
                    Interlocked.Exchange(ref _isInitializing, 0);
                }
            }
        }

        private void ExcelCreateFirst(string key, IEnumerable<TextLocalization> texts)
        {
            ExcelInterop.Application excel = new ExcelInterop.Application();
            ExcelInterop.Workbooks workbooks = excel.Workbooks;
            ExcelInterop.Workbook workbook;

            if (File.Exists(Path.GetFullPath(TranslationFilePath)))
            {
                workbook = workbooks.Open(Path.GetFullPath(TranslationFilePath));
            }
            else
            {
                //As this function can and will automatically be called again after this failiure, logging in info
                _logger.Log(LogLevel.Information,
                    $@"Unable to find langage file ({Path.GetFullPath(TranslationFilePath)}).");
                return;
            }

            try
            {
                ExcelInterop.Worksheet worksheet = (ExcelInterop.Worksheet) workbook.Worksheets[1];
                string[] keyParts = key.Split(Properties.Settings.Default.Seperator_for_partial_Literalkeys);
                _numKeyParts = keyParts.Length;

                int currentColumn;

                for (currentColumn = 1; currentColumn < keyParts.Length + 1; currentColumn++)
                {
                    worksheet.Cells[2, currentColumn] = keyParts[currentColumn - 1];
                }

                foreach (TextLocalization textLocalization in texts)
                {
                    worksheet.Cells[1, currentColumn] =
                        $@"{textLocalization.Language.NativeName} ({textLocalization.Language.Name})";
                    worksheet.Cells[2, currentColumn] = textLocalization.Text;
                    currentColumn++;
                }

                excel.DisplayAlerts = false;
                workbook.Save();

                _logger.Log(LogLevel.Trace, "Successfully created initial entry in excel sheet.");
                Status = ProviderStatus.Initialized;
            }
            catch
            {
                //As this function can and will automatically be called again after this failiure, logging in info
                _logger.Log(LogLevel.Information, "Failed to write initial entry of excel sheet.");
            }
            finally
            {
                workbook.Close();
                workbooks.Close();
                excel.Quit();
            }
        }

        private void ExcelWriteActions()
        {
            ExcelInterop.Application excel = new ExcelInterop.Application();
            ExcelInterop.Workbooks workbooks = excel.Workbooks;
            ExcelInterop.Workbook workbook;

            if (File.Exists(Path.GetFullPath(TranslationFilePath)))
            {
                workbook = workbooks.Open(Path.GetFullPath(TranslationFilePath));
            }
            else
            {
                _logger.Log(LogLevel.Warning, 
                    $@"Unable to find langage file ({Path.GetFullPath(TranslationFilePath)}).");
                return;
            }

            try
            {
                ExcelInterop.Worksheet worksheetGui = (ExcelInterop.Worksheet) workbook.Worksheets[1];
                var textLocalizations = TextLocalizationsUtils.FlipLocalizationsDictionary(_dictOfDicts);
                WriteGuiTranslations(worksheetGui, textLocalizations);

                excel.DisplayAlerts = false;
                workbook.Save();
            }
            catch(System.Exception e)
            {
                _logger.Log(LogLevel.Warning,
                    $"Failed to write changed dictionary to excel file ({Path.GetFullPath(TranslationFilePath)}). {e.GetType()} ({e.Message}).");
            }
            finally
            {
                workbook.Close();
                workbooks.Close();
                excel.Quit();
            }
        }

        private void ReadGuiTranslations(ExcelInterop.Worksheet worksheetGui) {
            //first row only contains column titles (which can be null in first column), no data
            int row = 2;

            int numberOfGlossaryEntries = 0;
            object[,] values = worksheetGui.UsedRange.get_Value();
            int maxRow = values.GetUpperBound(0);
            int maxColumn = values.GetUpperBound(1);

            for (int column = 1; column < maxColumn; column++)
            {
                try
                {
                    CultureInfoUtil.GetCultureInfo(ExcelCellToString(values[1, column]), true);
                    _numKeyParts = column - 1;
                    break;
                }
                catch (InvalidCultureNameException) { }
            }
            _logger.Log(LogLevel.Debug,
                $"Found {_numKeyParts} columns for key parts and {(maxColumn - _numKeyParts)} language columns.");

            for (int langIndex = _numKeyParts + 1; langIndex <= maxColumn; langIndex++)
            {
                CultureInfo lang = CultureInfoUtil.GetCultureInfo(ExcelCellToString(values[1, langIndex]), true);

                if (!_dictOfDicts.ContainsKey(lang))
                {
                    _dictOfDicts.Add(lang, new Dictionary<string, string>());
                }
            }

            _logger.Log(LogLevel.Trace, "Now reading rows from excel sheet.");
            while (row <= maxRow && values[row, 1] != null)
            {
                bool isGlossaryEntry = _glossaryTag != null && _glossaryTag.Equals(values[row, 1]);

                //check if current row has a comment
                //or part of glossary (assuming a glossary is being used)
                if (values[row, 2] != null || isGlossaryEntry)
                {
                    string key;
                    if (isGlossaryEntry)
                    {
                        key = _glossaryTag + numberOfGlossaryEntries;
                        numberOfGlossaryEntries++;
                    }
                    else
                    {
                        string[] keyColumnCells = new string[_numKeyParts];
                        for (int i = 0; i < _numKeyParts; i++)
                        {
                            keyColumnCells[i] = ExcelCellToString(values[row, i + 1]);
                        }
                        key = CreateGuiDictionaryKey(keyColumnCells);
                    }

                    for (int langIndex = _numKeyParts + 1; langIndex <= maxColumn; langIndex++)
                    {
                        CultureInfo lang = CultureInfoUtil.GetCultureInfo(ExcelCellToString(values[1, langIndex]), true);
                        _dictOfDicts[lang].Add(key, ExcelCellToString(values[row, langIndex]));
                    }
                }
                else
                {
                    _logger.Log(LogLevel.Debug, $"Skipped row #{row}, because it is a comment.");
                }

                row++;
            }
        }

        private void WriteGuiTranslations(ExcelInterop.Worksheet worksheetGui, Dictionary<string, List<TextLocalization>> texts) {

            ExcelInterop.Range usedRange = worksheetGui.UsedRange;

            object[,] values = usedRange.get_Value();
            int maxColumn = values.GetUpperBound(1);

            foreach (var translation in texts)
            {
                //if current entry is part of glossary, skip writing
                Regex glossaryKey = new Regex($"^{_glossaryTag}\\d*$");
                if (_glossaryTag != null && glossaryKey.IsMatch(translation.Key))
                {
                    continue;
                }

                bool updatedRow = false;
                int lastFindForDialogIndex = -1;

                string[] keyParts = translation.Key.Split(Properties.Settings.Default.Seperator_for_partial_Literalkeys);

                //squeeze key parts into columns if necessary
                if (keyParts.Length > _numKeyParts)
                {
                    string[] newKeyParts = new string[_numKeyParts];
                    Array.Copy(keyParts, newKeyParts, _numKeyParts);

                    int diff = keyParts.Length - _numKeyParts;
                    for (int overflowKeyPart = 0; overflowKeyPart < diff; overflowKeyPart++)
                    {
                        newKeyParts[_numKeyParts - 1] += Properties.Settings.Default.Seperator_for_partial_Literalkeys;
                        newKeyParts[_numKeyParts - 1] += keyParts[_numKeyParts + overflowKeyPart];
                    }
                }

                //find first row, matching beginning of key
                ExcelInterop.Range currentDialogFind = usedRange.Find(keyParts[0], Type.Missing,
                    ExcelInterop.XlFindLookIn.xlValues, ExcelInterop.XlLookAt.xlPart,
                    ExcelInterop.XlSearchOrder.xlByRows, ExcelInterop.XlSearchDirection.xlNext, false);
                var firstDialogFind = currentDialogFind;

                //search for match with key
                while (currentDialogFind != null)
                {
                    //get rest of key from sheet
                    ExcelInterop.Range currentRow = worksheetGui.Rows[currentDialogFind.Row];
                    string[] keyColumnsCells = new string[_numKeyParts];
                    for (int i = 0; i < _numKeyParts; i++)
                    {
                        keyColumnsCells[i] = currentRow.Cells[i + 1].Value;
                    }
                    //check if whole key matches
                    if (keyColumnsCells.SequenceEqual(keyParts))
                    {
                        //now write to cell, values array and maxColumns may change if Excel sheet needs to be altered
                        WriteToCell(translation.Value, ref values, ref maxColumn, currentRow.Row, worksheetGui);
                        updatedRow = true;
                        break;
                    }

                    //remember current
                    lastFindForDialogIndex = currentDialogFind.Row;
                    //get new current
                    currentDialogFind = usedRange.FindNext(currentDialogFind);

                    //compare new and old current
                    if (currentDialogFind.Address == firstDialogFind.Address)
                    {
                        break;
                    }

                }

                if (updatedRow)
                {
                    continue;
                }
                //if no row was found, a new one needs to be created
                _logger.Log(LogLevel.Trace, $"New Entry will be created in Excel sheet for key ({translation.Key}).");

                ExcelInterop.Range newRow;
                //try writing new line next to others with same key beginning
                if (lastFindForDialogIndex >= 0)
                {
                    newRow = worksheetGui.Rows[lastFindForDialogIndex + 1];
                    newRow.Insert();
                    //get inserted row
                    newRow = worksheetGui.Rows[lastFindForDialogIndex + 1];
                    _logger.Log(LogLevel.Trace, "Entry was inserted after similar keys.");
                }
                //if first part (or whole key for single key fragment setups like ResourceLiteralProvider) can't be found write new line at end of sheet
                else
                {
                    ExcelInterop.Range lastRow =
                        worksheetGui.Cells.SpecialCells(ExcelInterop.XlCellType.xlCellTypeLastCell);
                    int indexlastRow = lastRow.Row;
                    newRow = worksheetGui.Rows[indexlastRow + 1];
                    _logger.Log(LogLevel.Trace, "Entry was added to end of excel sheet.");
                }

                //write new key parts
                for (int i = 0; i < _numKeyParts; i++)
                {
                    newRow.Cells[i + 1] = keyParts[i];
                }

                //write new texts, values array and maxColumns may change if Excel sheet needs to be altered
                WriteToCell(translation.Value, ref values, ref maxColumn, newRow.Row, worksheetGui);
            }
        }

        private void WriteToCell(IEnumerable<TextLocalization> texts, ref object[,] values, ref int maxColumn, int currentRow, ExcelInterop.Worksheet worksheetGui)
        {
            foreach (TextLocalization text in texts)
            {
                //identify index for current Language
                int langIndex;
                for (langIndex = _numKeyParts + 1; langIndex <= maxColumn; langIndex++)
                {
                    if (Equals(CultureInfoUtil.GetCultureInfo(ExcelCellToString(values[1, langIndex]), true),
                        text.Language))
                    {
                        break;
                    }
                }

                if (langIndex > maxColumn)
                {
                    //if language doesn't exist in sheet write new language string in header row of new column
                    worksheetGui.Cells[1, langIndex].Value = $@"({text.Language.Name})";
                    maxColumn++;
                    values = worksheetGui.UsedRange.get_Value();
                    _logger.Log(LogLevel.Information,
                        $"New language ({text.Language.EnglishName}) previously not part of excel sheet detected and added to excel sheet");
                }

                ExcelInterop.Range targetCellToUpdate = worksheetGui.Cells[currentRow, langIndex];
                targetCellToUpdate.Value = text.Text;
            }
        }

        private string ExcelCellToString(object cellValue)
        {
            return cellValue == null ? string.Empty : cellValue.ToString();
        }

        /// <summary>
        /// Work for BackgroundWorker, can also be called without BackgroundWorker
        /// </summary>
        private void LoadExcelLanguageFileAsync(object sender, DoWorkEventArgs e) {
            BackgroundWorker bw = sender as BackgroundWorker;

            if (bw == null)
            {
                _logger.Log(LogLevel.Information,
                    "LoadExcelLanguageFileAsync functions was called without BackgroundWorker.");
            }
            else
            {
                _logger.Log(LogLevel.Trace, "LoadExcelLanguageFileAsync functions was called by BackgroundWorker.");
            }

            ExcelInterop.Application excel = new ExcelInterop.Application();
            //already checked in Initialize if file exists
            ExcelInterop.Workbook workbook = excel.Workbooks.Open(Path.GetFullPath(TranslationFilePath));

            try
            {
                if (bw == null || !bw.CancellationPending)
                {
                    _logger.Log(LogLevel.Trace,"Reading excel file not aborted.");
                    ExcelInterop.Worksheet worksheetGui = (ExcelInterop.Worksheet) workbook.Worksheets[1];
                    ReadGuiTranslations(worksheetGui);
                }
                else
                {
                    _logger.Log(LogLevel.Information, "Reading excel file was aborted.");
                }
            }
            finally
            {
                workbook.Close(false);
                excel.Quit();
            }

            if (bw != null)
            {
                e.Cancel = bw.CancellationPending;
            }
        }

        /// <summary>
        /// Clean up after BackgroundWorker finished, can also be called without BackgroundWorker
        /// </summary>
        private void LoadExcelLanguageFileAsyncCompleted(object sender, RunWorkerCompletedEventArgs e) {
            Interlocked.Exchange(ref _isInitializing, 0);

            //not great I know
            GC.Collect();

            switch (Status)
            {
                case ProviderStatus.CancellationInProgress:
                    Status = ProviderStatus.CancellationComplete;
                    _logger.Log(LogLevel.Trace, "Finished cancellation.");
                    break;
                case ProviderStatus.InitializationInProgress:
                    Status = ProviderStatus.Initialized;
                    _logger.Log(LogLevel.Trace, "Finished initialization.");
                    break;
            }
        }

        private static string CreateGuiDictionaryKey(string[] keyParts)
        {
            StringBuilder stringBuilder = new StringBuilder();
            for (int i = 0; i < keyParts.Length - 1; i++)
            {
                stringBuilder.Append(keyParts[i]);
                stringBuilder.Append(Properties.Settings.Default.Seperator_for_partial_Literalkeys);
            }

            if (keyParts.Length > 0)
            {
                stringBuilder.Append(keyParts[keyParts.Length - 1]);
            }

            return stringBuilder.ToString();
        }

        private void Initialize()
        {
            _logger.Log(LogLevel.Trace, "Entering Initialize function.");
            CopyOldExcelFile();

            if (Status == ProviderStatus.InitializationInProgress && Interlocked.Exchange(ref _isInitializing, 1) == 0) {
                _logger.Log(LogLevel.Trace, "Starting initialization.");
                if (!File.Exists(TranslationFilePath)) {
                    _logger.Log(LogLevel.Debug, 
                        $@"Unable to find langauge file ({Path.GetFullPath(TranslationFilePath)}).");

                    ExcelCreateNew(TranslationFilePath);
                    //not great I know
                    GC.Collect();
                    _logger.Log(LogLevel.Trace, "Ended new excel file creation.");
                    
                    return;
                }

                _backgroundWorker = new BackgroundWorker();
                _backgroundWorker.DoWork += LoadExcelLanguageFileAsync;
                _backgroundWorker.RunWorkerCompleted += LoadExcelLanguageFileAsyncCompleted;
                _backgroundWorker.WorkerSupportsCancellation = true;

                _logger.Log(LogLevel.Trace, "Starting BackgraoundWorker.");
                _backgroundWorker.RunWorkerAsync();
            }
            else
            {
                _logger.Log(LogLevel.Information, "Initialize function was called multiple times.");
            }
        }

        private string InspectPath(string path)
        {
            if (path == null)
            {
                _logger.Log(LogLevel.Warning, "Cannot access language file, bacause path is null.");
                return null;
            }

            if (!path.EndsWith(".xlsx"))
            {
                _logger.Log(LogLevel.Debug, $"Added '.xlsx' to path ({path}).");
                path += ".xlsx";
            }

            if (File.Exists(Path.GetFullPath(path)))
            {
                return path;
            }
            _logger.Log(LogLevel.Debug, $"Dictionary for Excel file will be created ({path}).");

            string directory = Path.GetDirectoryName(path);

            if (directory != null)
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch
                {
                    _logger.Log(LogLevel.Warning, $"Failed to create dictionary ({directory}) for path ({path}).");
                }
            }

            return path;
        }

        /// <summary>
        /// save main sheet as old, if old doesn't exist jet
        /// </summary>
        private void CopyOldExcelFile()
        {
            if (OldTranslationFilePath == null)
            {
                _logger.Log(LogLevel.Trace, "No backup file will be created.");
                return;
            }

            string translationFilePathFullPath = Path.GetFullPath(TranslationFilePath);
            string oldTranslationFilePathFullPath = Path.GetFullPath(OldTranslationFilePath);
            if (!File.Exists(oldTranslationFilePathFullPath))
            {
                try
                {
                    File.Copy(translationFilePathFullPath, oldTranslationFilePathFullPath, true);
                }
                catch (System.Exception e)
                {
                    _logger.Log(LogLevel.Warning, 
                        $@"Unable to save langage file ({OldTranslationFilePath}). {e.GetType()} ({e.Message}).");
                }
            }
            else
            {
                _logger.Log(LogLevel.Trace, "Backup file already created, No new backup was made.");
            }
        }

    }
}
