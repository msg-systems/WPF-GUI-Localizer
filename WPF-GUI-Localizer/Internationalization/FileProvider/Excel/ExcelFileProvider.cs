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
using Internationalization.FileProvider.FileHandler;
using Internationalization.FileProvider.Interface;
using Internationalization.Model;
using Internationalization.Utilities;
using Microsoft.Extensions.Logging;
using ExcelInterop = Microsoft.Office.Interop.Excel;

namespace Internationalization.FileProvider.Excel
{
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

        private readonly string _glossaryTag;
        private readonly string _path;
        private readonly string _backupPath;
        private readonly UniversalFileHandler _fileHandler;
        private readonly Dictionary<CultureInfo, Dictionary<string, string>> _dictOfDicts =
            new Dictionary<CultureInfo, Dictionary<string, string>>();

        //0: initialization is not yet started or completed.
        //1: initialization is already started and running.
        private int _isInitializing;
        private BackgroundWorker _backgroundWorker;
        private int _numKeyParts;

        public ProviderStatus Status { get; private set; }

        /// <summary>Saves file as Excel, a backup will be created before the file is edited.</summary>
        /// <param name="translationFilePath">File that will be worked on being worked on.</param>
        /// <param name="glossaryTag">
        /// Entries in the Excel table that start with this tag will be interpreted as part of the glossary.
        /// </param>
        /// <param name="oldTranslationFilePath">
        /// A copy of the original sheet will be put here if no copy exists jet.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown, if <paramref name="translationFilePath"/> is null.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown, if the permissions are missing that are needed to create the directory for
        /// <paramref name="translationFilePath"/> / <paramref name="oldTranslationFilePath"/> or one of them
        /// is write-only, read-only, a directory, hidden, the needed permissions for opening or writing are
        /// missing or the operation is not supported on the current platform.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// Thrown, if certain permissions are missing. (CLR level)
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown, if <paramref name="translationFilePath"/> / <paramref name="oldTranslationFilePath"/>
        /// does not exist or cannot be found.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown, if an unknown I/O-Error occurres.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown, if <paramref name="translationFilePath"/> / <paramref name="oldTranslationFilePath"/>
        /// contains a colon anywhere other than as part of a volume identifier ("C:\").
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// Thrown, if <paramref name="translationFilePath"/> / <paramref name="oldTranslationFilePath"/>
        /// is too long.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown, if the directory was not found.
        /// For example because it is on an unmapped device.
        /// </exception>
        public ExcelFileProvider(string translationFilePath, string glossaryTag = null,
            string oldTranslationFilePath = null)
        {
            //set Status.
            Status = ProviderStatus.InitializationInProgress;

            //easy initializations.
            _logger = GlobalSettings.LibraryLoggerFactory.CreateLogger<ExcelFileProvider>();
            _logger.Log(LogLevel.Trace, "Initializing ExcelFileProvider.");
            _glossaryTag = glossaryTag;
            _fileHandler = new UniversalFileHandler(typeof(ExcelFileProvider), ".xlsx");

            //null check.
            if (translationFilePath == null)
            {
                var e = new ArgumentNullException(nameof(translationFilePath), "Unable to open null path.");
                _logger.Log(LogLevel.Error, e,
                    $"ExcelFileProvider recived null parameter in constructor for {nameof(translationFilePath)}.");
                throw e;
            }

            //start proper initialization.
            if(oldTranslationFilePath != null) {
                _backupPath = _fileHandler.GetPathAndHandleProblems(oldTranslationFilePath);
            }
            _path = _fileHandler.GetPathAndHandleProblems(translationFilePath);
            Initialize();
        }

        /// <summary>
        /// Returns the internal dictionary.
        /// </summary>
        /// <exception cref="FileProviderNotInitializedException">
        /// Will be thrown if the object has not found a language file to pull translations from.
        /// </exception>
        public Dictionary<CultureInfo, Dictionary<string, string>> GetDictionary()
        {
            if (Status != ProviderStatus.Initialized)
            {
                var e = new FileProviderNotInitializedException(
                    "Dictionary was accessed, without ExcelFileProvider being initialized.");
                _logger.Log(LogLevel.Error, e,
                    "Dictionary was accessed, without ExcelFileProvider being initialized.");
                throw e;
            }

            return _dictOfDicts;
        }

        /// <summary>
        /// Updates internal dictionary at <paramref name="key"/> with the given dictionary.
        /// Only languages contained in <paramref name="texts"/> will be updated.
        /// Will automatically write to file, if this is the first Update call
        /// and no file existed upon creation of this object.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown, if <paramref name="key"/> is null.</exception>
        /// <param name="key">The entry for which translations should be updated.</param>
        /// <param name="texts">
        /// The new translations. If list is null or empty, no changes will be made to the dictionary.
        /// </param>
        public void Update(string key, IEnumerable<TextLocalization> texts)
        {
            //null checks.
            if (key == null)
            {
                var e = new ArgumentNullException(nameof(key), "Key recived in Update call is null.");
                _logger.Log(LogLevel.Warning, e, "Unable to update dictionary for null key.");
                throw e;
            }
            if (texts == null)
            {
                //no exception has to be thrown here, because null is treated like empty list and
                //no translations will be updated.
                _logger.Log(LogLevel.Debug, "Unable to update dictionary for null translations. "
                                            + "No translations were updated.");
                return;
            }

            //logging.
            IList<TextLocalization> textsEnumerated = texts.ToList();
            var textsString = string.Join(", ", textsEnumerated.Select(l => l.ToString()));
            _logger.Log(LogLevel.Trace, $"Update was called with {{{textsString}}} as translations for key ({key}).");

            //dictionary updates.
            if (!UpdateDictionary(key, textsEnumerated))
            {
                _logger.Log(LogLevel.Debug, "Did not update dictionary.");
                return;
            }

            //create file based on first entry,
            //if dictionary was updated and the file was created by JsonFileProvider itself.
            if (Status == ProviderStatus.InitializationInProgress && _isInitializing == 0)
            {
                _logger.Log(LogLevel.Debug, "First update after empty sheet was created.");
                _numKeyParts = key.Split(Properties.Settings.Default.Seperator_for_partial_Literalkeys).Length;
                SaveDictionary();

                Status = ProviderStatus.Initialized;
            }
            else
            {
                _logger.Log(LogLevel.Trace, "Finished updating dictionary.");
            }
        }

        /// <summary>
        /// Makes the current dictionary persistent, by writing it to an excel file (.xlsx).
        /// This function only logs Exceptions that may occur and does not throw any.
        /// </summary>
        public void SaveDictionary()
        {
            _logger.Log(LogLevel.Trace, "SaveDictionary was called.");
            ExcelWriteActions();

            //not great, I know.
            GC.Collect();
        }

        public void CancelInitialization()
        {
            if (_backgroundWorker == null) return;

            if (_backgroundWorker.IsBusy)
            {
                Status = ProviderStatus.CancellationInProgress;
                _logger.Log(LogLevel.Trace, "Cancellation started.");
                _backgroundWorker.CancelAsync();
            }
        }


        /// <summary>
        /// Updates <see cref="_dictOfDicts"/> using the given values and returns true, if any updates were made.
        /// </summary>
        /// <param name="key">
        /// The entry for which translations should be updated.
        /// Assumed to be not null, because this function is only used once.
        /// </param>
        /// <param name="textLocalizations">
        /// The new translations. If list is null or empty, no changes will be made to the dictionary.
        /// Assumed to be not null, because this function is only used once.
        /// </param>
        /// <returns>
        /// True, if at least one languages translation was updated.
        /// False, if <paramref name="textLocalizations"/> cantained no entries.
        /// </returns>
        private bool UpdateDictionary(string key, IEnumerable<TextLocalization> textLocalizations)
        {
            var readSuccess = false;

            foreach (var textLocalization in textLocalizations)
            {
                _dictOfDicts.TryGetValue(textLocalization.Language, out var langDict);
                if (langDict == null)
                {
                    langDict = new Dictionary<string, string>();
                    _dictOfDicts.Add(textLocalization.Language, langDict);
                    _logger.Log(LogLevel.Trace,
                        $"New language dictionary was created for {textLocalization.Language.EnglishName}.");
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

                readSuccess = true;
            }

            return readSuccess;
        }

        private void ExcelCreateNew(string path)
        {
            var excel = new ExcelInterop.Application();
            var workbook = excel.Workbooks.Add();

            var fail = true;
            try
            {
                excel.DisplayAlerts = false;
                workbook.SaveAs(Path.GetFullPath(path));
                fail = false;
                _logger.Log(LogLevel.Debug, $"Successfully created empty excel file ({path}).");
            }
            catch
            {
                _logger.Log(LogLevel.Error, $"Unable to create empty excel file ({path}).");
            }
            finally
            {
                workbook.Close();
                excel.Quit();

                //status updates
                if (fail)
                {
                    _logger.Log(LogLevel.Trace, "Failed to create empty excel sheet.\n"
                                                + "ExcelFileProvider is still in State IsInitializing and "
                                                + "unable to create a first entry, but calling SaveDictionary"
                                                + "is still possible.");
                }
                else
                {
                    //to siganl that even with Status == IsInitaializing, no more reading is needed.
                    Interlocked.Exchange(ref _isInitializing, 0);
                    _logger.Log(LogLevel.Trace, "Successfully created empty excel sheet."
                                                + "ExcelFileProvider is still in State IsInitializing, but can create first entry.");
                }
            }
        }

        private void ExcelCreateFirst(string key, IEnumerable<TextLocalization> texts)
        {
            var excel = new ExcelInterop.Application();
            ExcelInterop.Workbook workbook;

            if (File.Exists(Path.GetFullPath(_path)))
            {
                workbook = excel.Workbooks.Open(Path.GetFullPath(_path));
            }
            else
            {
                //as this function can and will automatically be called again after this failure, logging in warn.
                _logger.Log(LogLevel.Warning,
                    $"Unable to find langage file ({Path.GetFullPath(_path)}).");
                return;
            }

            try
            {
                var worksheet = (ExcelInterop.Worksheet) workbook.Worksheets[1];
                var keyParts = key.Split(Properties.Settings.Default.Seperator_for_partial_Literalkeys);
                _numKeyParts = keyParts.Length;

                int currentColumn;

                for (currentColumn = 1; currentColumn < keyParts.Length + 1; currentColumn++)
                {
                    worksheet.Cells[2, currentColumn] = keyParts[currentColumn - 1];
                }

                foreach (var textLocalization in texts)
                {
                    worksheet.Cells[1, currentColumn] =
                        $"{textLocalization.Language.NativeName} ({textLocalization.Language.Name})";
                    worksheet.Cells[2, currentColumn] = textLocalization.Text;
                    currentColumn++;
                }

                excel.DisplayAlerts = false;
                workbook.Save();

                _logger.Log(LogLevel.Trace, "Successfully created initial entry in excel sheet."
                                            + "ExcelFileProvider is now in State Initialized.");
                Status = ProviderStatus.Initialized;
            }
            catch
            {
                //as this function can and will automatically be called again after this failiure, logging in warning not error.
                _logger.Log(LogLevel.Warning, "Failed to write initial entry of excel sheet.");
            }
            finally
            {
                workbook.Close();
                excel.Quit();
            }
        }

        /// <summary>
        /// Can be used to update the excel sheet with the current dictionary, write the first
        /// entry into an empty sheet or create a new excel sheet based on the dictionary.
        /// </summary>
        private void ExcelWriteActions()
        {
            var excel = new ExcelInterop.Application();
            ExcelInterop.Workbook workbook;
            bool creatingNew;

            if (File.Exists(Path.GetFullPath(_path)))
            {
                workbook = excel.Workbooks.Open(Path.GetFullPath(_path));
                creatingNew = false;
            }
            else
            {
                _logger.Log(LogLevel.Information,
                    $"Unable to find langage file ({Path.GetFullPath(_path)}). A new one will be created.");

                workbook = excel.Workbooks.Add();
                creatingNew = true;
            }

            try
            {
                var worksheetGui = (ExcelInterop.Worksheet) workbook.Worksheets[1];
                var textLocalizations =
                    TextLocalizationsUtils.FlipLocalizationsDictionary(_dictOfDicts);

                WriteGuiTranslations(worksheetGui, textLocalizations);

                excel.DisplayAlerts = false;
                if (creatingNew)
                {
                    workbook.SaveAs(Path.GetFullPath(_path));
                }
                else
                {
                    workbook.Save();
                }
            }
            catch (System.Exception e)
            {
                //catching Exception, because Exceptions thrown by Excel Interop are
                //not as well documented
                _logger.Log(LogLevel.Error, e,
                    "Failed to write {0} excel file ({1}). {2} ({3}). "
                    + "Application was not interrupted.",
                    creatingNew ? "dictionary to new" : "changed dictionary to",
                    Path.GetFullPath(_path), e.GetType(), e.Message);
            }
            finally
            {
                workbook.Close();
                excel.Quit();
            }
        }

        private void ReadGuiTranslations(ExcelInterop.Worksheet worksheetGui)
        {
            //first row only contains column titles (which can be null in cell 1,1), no data.
            var row = 2;

            var numberOfGlossaryEntries = 0;
            object[,] values = worksheetGui.UsedRange.get_Value();
            var maxRow = values.GetUpperBound(0);
            var maxColumn = values.GetUpperBound(1);

            _numKeyParts = GetNumberOfKeyParts(values);

            _logger.Log(LogLevel.Debug,
                $"Found {_numKeyParts} columns for key parts and {maxColumn - _numKeyParts} language columns.");

            for (var langIndex = _numKeyParts + 1; langIndex <= maxColumn; langIndex++)
            {
                var lang = CultureInfoUtil.GetCultureInfo(ExcelCellToString(values[1, langIndex]), true);

                if (!_dictOfDicts.ContainsKey(lang))
                {
                    _dictOfDicts.Add(lang, new Dictionary<string, string>());
                }
            }

            _logger.Log(LogLevel.Trace, "Now reading rows from excel sheet.");
            while (row <= maxRow && values[row, 1] != null)
            {
                var isGlossaryEntry = _glossaryTag != null && _glossaryTag.Equals(values[row, 1]);

                //check if current row has a comment
                //or part of glossary (assuming a glossary is being used).
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
                        var keyColumnCells = new string[_numKeyParts];
                        for (var i = 0; i < _numKeyParts; i++)
                        {
                            keyColumnCells[i] = ExcelCellToString(values[row, i + 1]);
                        }

                        key = CreateGuiDictionaryKey(keyColumnCells);
                    }

                    for (var langIndex = _numKeyParts + 1; langIndex <= maxColumn; langIndex++)
                    {
                        var lang = CultureInfoUtil.GetCultureInfo(ExcelCellToString(values[1, langIndex]), true);
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

        /// <summary>
        /// Counts how many columns are reserved for the translation key, based on the
        /// first column that can be identified as a language column.
        /// </summary>
        /// <param name="excelCells">The array conatining all cell values out of the sheet</param>
        /// <returns></returns>
        private static int GetNumberOfKeyParts(object[,] excelCells)
        {
            //null checks.
            if (excelCells == null)
            {

                var e = new ArgumentNullException(nameof(excelCells),
                    "Unable to find number of key parts in null array.");
                _logger.Log(LogLevel.Error, e,
                    "ExcelFileProvider recived null parameter in constructor for translationFilePath.");
                throw e;
            }

            var maxColumn = excelCells.GetUpperBound(1);

            for (var column = 1; column < maxColumn; column++)
            {
                var culture = CultureInfoUtil.TryGetCultureInfo(ExcelCellToString(excelCells[1, column]), true);
                
                if (culture != null)
                {
                    return column - 1;
                }
            }

            return maxColumn;
        }

        private void WriteGuiTranslations(ExcelInterop.Worksheet worksheetGui,
            Dictionary<string, List<TextLocalization>> texts)
        {
            var usedRange = worksheetGui.UsedRange;

            object[,] values = usedRange.get_Value();
            var maxColumn = values.GetUpperBound(1);

            foreach (var translation in texts)
            {
                //if current entry is part of glossary, skip writing.
                var glossaryKey = new Regex($"^{_glossaryTag}\\d*$");
                if (_glossaryTag != null && glossaryKey.IsMatch(translation.Key))
                {
                    continue;
                }

                var updatedRow = false;
                var lastFindForDialogIndex = -1;
                var keyParts = translation.Key.Split(Properties.Settings.Default.Seperator_for_partial_Literalkeys);

                //squeeze key parts into columns, if necessary.
                if (keyParts.Length > _numKeyParts)
                {
                    var newKeyParts = new string[_numKeyParts];
                    Array.Copy(keyParts, newKeyParts, _numKeyParts);

                    var diff = keyParts.Length - _numKeyParts;
                    for (var overflowKeyPart = 0; overflowKeyPart < diff; overflowKeyPart++)
                    {
                        newKeyParts[_numKeyParts - 1] += Properties.Settings.Default.Seperator_for_partial_Literalkeys;
                        newKeyParts[_numKeyParts - 1] += keyParts[_numKeyParts + overflowKeyPart];
                    }
                }

                //find first row, matching beginning of key.
                var currentDialogFind = usedRange.Find(keyParts[0], Type.Missing,
                    ExcelInterop.XlFindLookIn.xlValues, ExcelInterop.XlLookAt.xlPart,
                    ExcelInterop.XlSearchOrder.xlByRows, ExcelInterop.XlSearchDirection.xlNext, false);
                var firstDialogFind = currentDialogFind;

                //search for match with key.
                while (currentDialogFind != null)
                {
                    //get rest of key from sheet.
                    ExcelInterop.Range currentRow = worksheetGui.Rows[currentDialogFind.Row];
                    var keyColumnsCells = new string[_numKeyParts];
                    for (var i = 0; i < _numKeyParts; i++)
                    {
                        keyColumnsCells[i] = currentRow.Cells[i + 1].Value;
                    }

                    //check if whole key matches.
                    if (keyColumnsCells.SequenceEqual(keyParts))
                    {
                        //now write to cell, values array and maxColumns may change if Excel sheet needs to be altered.
                        WriteToCell(translation.Value, ref values, ref maxColumn, currentRow.Row, worksheetGui);
                        updatedRow = true;
                        break;
                    }

                    lastFindForDialogIndex = currentDialogFind.Row;
                    currentDialogFind = usedRange.FindNext(currentDialogFind);

                    //compare new and old current.
                    if (currentDialogFind.Address == firstDialogFind.Address)
                    {
                        break;
                    }
                }

                if (updatedRow)
                {
                    continue;
                }

                //if no row was found, a new one needs to be created.
                _logger.Log(LogLevel.Trace, $"New Entry will be created in Excel sheet for key ({translation.Key}).");

                ExcelInterop.Range newRow;
                //try writing new line next to others with same key beginning.
                if (lastFindForDialogIndex >= 0)
                {
                    newRow = worksheetGui.Rows[lastFindForDialogIndex + 1];
                    newRow.Insert();
                    //get inserted row.
                    newRow = worksheetGui.Rows[lastFindForDialogIndex + 1];
                    _logger.Log(LogLevel.Trace, "Entry was inserted after similar keys.");
                }
                //if first part (or whole key for single key fragment setups like ResourceLiteralProvider)
                //can't be found write new line at end of sheet.
                else
                {
                    var lastRow =
                        worksheetGui.Cells.SpecialCells(ExcelInterop.XlCellType.xlCellTypeLastCell);
                    var indexlastRow = lastRow.Row;
                    newRow = worksheetGui.Rows[indexlastRow + 1];
                    _logger.Log(LogLevel.Trace, "Entry was added to end of excel sheet.");
                }

                //write new key parts.
                for (var i = 0; i < _numKeyParts; i++)
                {
                    newRow.Cells[i + 1] = keyParts[i];
                }

                //write new texts, values array and maxColumns may change if Excel sheet needs to be altered.
                WriteToCell(translation.Value, ref values, ref maxColumn, newRow.Row, worksheetGui);
            }
        }

        private void WriteToCell(IEnumerable<TextLocalization> texts, ref object[,] values, ref int maxColumn,
            int currentRow, ExcelInterop.Worksheet worksheetGui)
        {
            foreach (var text in texts)
            {
                //identify index for current language.
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
                    //if language doesn't exist in sheet, write new language string in header row of new column.
                    worksheetGui.Cells[1, langIndex].Value = $"({text.Language.Name})";
                    maxColumn++;
                    values = worksheetGui.UsedRange.get_Value();
                    _logger.Log(LogLevel.Information,
                        $"New language ({text.Language.EnglishName}) previously not part of excel sheet detected and added to excel sheet");
                }

                ExcelInterop.Range targetCellToUpdate = worksheetGui.Cells[currentRow, langIndex];
                targetCellToUpdate.Value = text.Text;
            }
        }

        private static string ExcelCellToString(object cellValue)
        {
            return cellValue == null ? string.Empty : cellValue.ToString();
        }

        /// <summary>
        /// Work for BackgroundWorker, can also be called without BackgroundWorker.
        /// </summary>
        private void LoadExcelLanguageFileAsync(object sender, DoWorkEventArgs e)
        {
            var bw = sender as BackgroundWorker;

            if (bw == null)
            {
                _logger.Log(LogLevel.Information,
                    "LoadExcelLanguageFileAsync functions was called without BackgroundWorker.");
            }
            else
            {
                _logger.Log(LogLevel.Trace, "LoadExcelLanguageFileAsync functions was called by BackgroundWorker.");
            }

            var excel = new ExcelInterop.Application();
            //already checked in Initialize, if file exists.
            var workbook = excel.Workbooks.Open(Path.GetFullPath(_path));

            //try only needed for finally statement, normally no exceptions should occur.
            try
            {
                if (bw == null || !bw.CancellationPending)
                {
                    _logger.Log(LogLevel.Trace, "Reading excel file not aborted.");
                    var worksheetGui = (ExcelInterop.Worksheet) workbook.Worksheets[1];
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
        /// Clean up after BackgroundWorker finished, can also be called without BackgroundWorker.
        /// </summary>
        private void LoadExcelLanguageFileAsyncCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Interlocked.Exchange(ref _isInitializing, 0);

            //not great, I know.
            GC.Collect();

            switch (Status)
            {
                case ProviderStatus.CancellationInProgress:

                    Status = ProviderStatus.CancellationComplete;
                    _logger.Log(LogLevel.Trace,
                        "Finished cancellation. ExcelFileProvider is now in State CancellationComplete.");
                    break;
                case ProviderStatus.InitializationInProgress:

                    if (_dictOfDicts == null || _dictOfDicts.Count == 0)
                    {
                        _logger.Log(LogLevel.Trace, "Was unable to collect information from file.\nExcelFileProvider "
                                                    + "is still in State IsInitializing and will override file content "
                                                    + "with next Update call.");
                    }
                    else
                    {
                        Status = ProviderStatus.Initialized;
                        _logger.Log(LogLevel.Trace,
                            "Finished initialization. ExcelFileProvider is now in State Initialized.");
                    }

                    break;
            }
        }

        private static string CreateGuiDictionaryKey(string[] keyParts)
        {
            var stringBuilder = new StringBuilder();
            for (var i = 0; i < keyParts.Length - 1; i++)
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
            //logging.
            _logger.Log(LogLevel.Trace, "Entering Initialize function.");

            //creating backup.
            if (_backupPath == null)
            {
                _logger.Log(LogLevel.Trace, "No backup file will be created.");
            }
            else
            {
                _fileHandler.CopyBackupWrapper(_path, _backupPath);
            }

            //setting up BackgroundWorker.
            if (Status == ProviderStatus.InitializationInProgress && _isInitializing == 0)
            {
                Interlocked.Exchange(ref _isInitializing, 1);

                if (!File.Exists(_path))
                {
                    _logger.Log(LogLevel.Debug,
                        $"Unable to find langauge file ({Path.GetFullPath(_path)}).");

                    ExcelCreateNew(_path);

                    //not great, I know.
                    GC.Collect();
                    _logger.Log(LogLevel.Trace, "Ended new excel file creation.");
                }
                else
                {
                    _backgroundWorker = new BackgroundWorker();
                    _backgroundWorker.DoWork += LoadExcelLanguageFileAsync;
                    _backgroundWorker.RunWorkerCompleted += LoadExcelLanguageFileAsyncCompleted;
                    _backgroundWorker.WorkerSupportsCancellation = true;

                    _logger.Log(LogLevel.Trace, "Starting BackgroundWorker.");
                    _backgroundWorker.RunWorkerAsync();
                }
            }
            else
            {
                _logger.Log(LogLevel.Information, "Initialize function was called multiple times.");
            }
        }
    }
}