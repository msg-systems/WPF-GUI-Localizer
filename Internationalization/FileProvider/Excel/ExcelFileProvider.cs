using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Internationalization.Converter;
using Internationalization.Exception;
using Internationalization.FileProvider.Interface;
using Internationalization.Model;
using Internationalization.Utilities;
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

        private readonly Dictionary<CultureInfo, Dictionary<string, string>> _dictOfDicts = new Dictionary<CultureInfo, Dictionary<string, string>>();

        // 0: initialization is not yet started or completed
        // 1: initialization is already started and running
        private int _isInitializing = 0;
        private BackgroundWorker _backgroundWorker;
        private int _numKeyParts;

        /// <summary>Saves file as Excel, no backup will be created</summary>
        /// <param name="translationFileFileName">File that will be worked on being worked on</param>
        public ExcelFileProvider(string translationFileFileName)
        {
            TranslationFileFileName = translationFileFileName;

            Initialize();
        }

        /// <summary>Saves file as Excel, a backup will be created before the file is edited</summary>
        /// <param name="translationFileFileName">File that will be worked on being worked on</param>
        /// <param name="oldTranslationFileFileName">A copy of the original sheet will be put here if no copy exists jet</param>
        public ExcelFileProvider(string translationFileFileName, string oldTranslationFileFileName) {
            TranslationFileFileName = translationFileFileName;
            OldTranslationFileFileName = oldTranslationFileFileName;
            
            Initialize();
        }

        public ProviderStatus Status { get; private set; }

        public Dictionary<CultureInfo, Dictionary<string, string>> GetDictionary()
        {
            if (Status != ProviderStatus.Initialized)
            {
                throw new FileProviderNotInitializedException();
            }

            return _dictOfDicts;
        }

        public void Update(string key, IEnumerable<TextLocalization> texts)
        {
            foreach (TextLocalization textLocalization in texts)
            {
                _dictOfDicts.TryGetValue(textLocalization.Language, out Dictionary<string, string> langDict);
                if (langDict == null)
                {
                    langDict = new Dictionary<string, string>();
                    _dictOfDicts.Add(textLocalization.Language, langDict);
                }

                if (langDict.ContainsKey(key))
                {
                    langDict.Remove(key);
                }
                langDict.Add(key, textLocalization.Text);
            }

            if (Status == ProviderStatus.InitializationInProgress && !File.Exists(Path.GetFullPath(TranslationFileFileName)))
            {
                ExcelCreateNew(key, texts);

                //not great I know
                GC.Collect();
            }
        }

        public void SaveDictionary()
        {
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

        private string TranslationFileFileName {
            get;
        }

        private string OldTranslationFileFileName {
            get;
        }

        private void ExcelCreateNew(string key, IEnumerable<TextLocalization> texts)
        {
            ExcelInterop.Application excel = new ExcelInterop.Application();
            ExcelInterop.Workbook workbook = excel.Workbooks.Add();
            ExcelInterop.Worksheet worksheet = (ExcelInterop.Worksheet)workbook.ActiveSheet;
            try
            {
                string[] keyParts = key.Split(Properties.Settings.Default.Seperator_for_partial_Literalkeys);
                _numKeyParts = keyParts.Length;

                int currentColumn;

                for (currentColumn = 1; currentColumn < keyParts.Length + 1; currentColumn++)
                {
                    worksheet.Cells[2, currentColumn] = keyParts[currentColumn - 1];
                }

                foreach (TextLocalization textLocalization in texts)
                {
                    worksheet.Cells[1, currentColumn] = $@"{textLocalization.Language.NativeName} ({textLocalization.Language.Name})";
                    worksheet.Cells[2, currentColumn] = textLocalization.Text;
                    currentColumn++;
                }

                //save excel without popup
                try
                {
                    //throws IOException if file exists with same path as Path.GetDirectoryName(TranslationFileFileName)
                    Directory.CreateDirectory(Path.GetDirectoryName(TranslationFileFileName));
                }
                catch (IOException) { }
                excel.DisplayAlerts = false;
                workbook.SaveAs(Path.GetFullPath(TranslationFileFileName));

                Status = ProviderStatus.Initialized;
            }
            finally
            {
                workbook.Close(true, Type.Missing, Type.Missing);
                excel.Quit();
            }
        }

        private void ExcelWriteActions()
        {
            ExcelInterop.Application excel = null;
            ExcelInterop.Workbooks workbooks = null;
            ExcelInterop.Workbook workbook = null;
            ExcelInterop.Worksheet worksheetGui = null;

            excel = new ExcelInterop.Application();
            workbooks = excel.Workbooks;

            if (File.Exists(Path.GetFullPath(TranslationFileFileName)))
            {
                workbook = workbooks.Open(Path.GetFullPath(TranslationFileFileName));
            }
            else
            {
                Console.WriteLine($@"Unable to write Langage File ({Path.GetFullPath(TranslationFileFileName)}).");
            }

            try
            {
                worksheetGui = (ExcelInterop.Worksheet)workbook.Worksheets[1];
                var textLocalizations = TextLocalizationsUtils.FlipLocalizationsDictionary(_dictOfDicts);
                WriteGuiTranslations(worksheetGui, textLocalizations);

                //save modified excel without popup
                excel.DisplayAlerts = false;
                workbook.Save();
            }
            finally
            {
                workbook?.Close(true, Type.Missing, Type.Missing);
                workbooks.Close();
                excel.Quit();
            }
        }

        private void ReadGuiTranslations(ExcelInterop.Worksheet worksheetGui) {
            //first row only contains column titles, no data
            int row = 2;

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

            for (int langIndex = _numKeyParts + 1; langIndex <= maxColumn; langIndex++)
            {
                CultureInfo lang = CultureInfoUtil.GetCultureInfo(ExcelCellToString(values[1, langIndex]), true);

                if (!_dictOfDicts.ContainsKey(lang))
                {
                    _dictOfDicts.Add(lang, new Dictionary<string, string>());
                }
            }

            while (row <= maxRow && values[row, 1] != null) {

                //check if current row has a comment
                if (values[row, 2] != null)
                {
                    string[] keyColumnCells = new string[_numKeyParts];
                    for (int i = 0; i < _numKeyParts; i++)
                    {
                        keyColumnCells[i] = ExcelCellToString(values[row, i + 1]);
                    }

                    string key = CreateGuiDictionaryKey(keyColumnCells);
                    for (int langIndex = _numKeyParts + 1; langIndex <= maxColumn; langIndex++)
                    {
                        CultureInfo lang = CultureInfoUtil.GetCultureInfo(ExcelCellToString(values[1, langIndex]), true);
                        _dictOfDicts[lang].Add(key, ExcelCellToString(values[row, langIndex]));
                    }
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
                ExcelInterop.Range firstDialogFind = null;
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
                        newKeyParts[_numKeyParts - 1] += keyParts[_numKeyParts + overflowKeyPart];
                    }
                }

                //find first row, matching beginning of key
                ExcelInterop.Range currentDialogFind = usedRange.Find(keyParts[0], Type.Missing,
                    ExcelInterop.XlFindLookIn.xlValues, ExcelInterop.XlLookAt.xlPart,
                    ExcelInterop.XlSearchOrder.xlByRows, ExcelInterop.XlSearchDirection.xlNext, false,
                    Type.Missing, Type.Missing);
                firstDialogFind = currentDialogFind;

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
                    if (currentDialogFind.Address[ExcelInterop.XlReferenceStyle.xlA1] ==
                        firstDialogFind.Address[ExcelInterop.XlReferenceStyle.xlA1])
                    {
                        break;
                    }

                }

                if (updatedRow)
                {
                    continue;
                }
                //if no row was found, a new one needs to be created

                ExcelInterop.Range newRow;
                //try writing new line next to others with same key beginning
                if (lastFindForDialogIndex >= 0)
                {
                    newRow = worksheetGui.Rows[lastFindForDialogIndex + 1];
                    newRow.Insert();
                    //get inserted row
                    newRow = worksheetGui.Rows[lastFindForDialogIndex + 1];
                }
                //if first part (or whole key for single key fragment setups like ResourceLiteralProvider) can't be found write new line at end of sheet
                else
                {
                    ExcelInterop.Range lastRow =
                        worksheetGui.Cells.SpecialCells(ExcelInterop.XlCellType.xlCellTypeLastCell, Type.Missing);
                    int indexlastRow = lastRow.Row;
                    newRow = worksheetGui.Rows[indexlastRow + 1];
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
                }

                ExcelInterop.Range targetCellToUpdate = worksheetGui.Cells[currentRow, langIndex];
                targetCellToUpdate.Value = text.Text;
            }
        }

        private string ExcelCellToString(object cellValue)
        {
            return cellValue == null ? string.Empty : cellValue.ToString();
        }

        private void LoadExcelLanguageFileAsync(object sender, DoWorkEventArgs e) {
            var bw = sender as BackgroundWorker;

            ExcelInterop.Application excel = new ExcelInterop.Application();
            ExcelInterop.Workbook workbook = excel.Workbooks.Open(Path.GetFullPath(TranslationFileFileName));

            try
            {
                if (!bw.CancellationPending)
                {
                    ExcelInterop.Worksheet worksheetGui = (ExcelInterop.Worksheet) workbook.Worksheets[1];
                    ReadGuiTranslations(worksheetGui);
                }
            }
            finally
            {
                workbook.Close(false, Type.Missing, Type.Missing);
                excel.Quit();
            }

            e.Cancel = bw.CancellationPending;
        }

        private void LoadExcelLanguageFileAsyncCompleted(object sender, RunWorkerCompletedEventArgs e) {
            Interlocked.Exchange(ref _isInitializing, 0);

            //not great I know
            GC.Collect();

            switch (Status)
            {
                case ProviderStatus.CancellationInProgress:
                    Status = ProviderStatus.CancellationComplete;
                    break;
                case ProviderStatus.InitializationInProgress:
                    Status = ProviderStatus.Initialized;
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
            Status = ProviderStatus.InitializationInProgress;
            CopyOldExcelFile();

            if (Status == ProviderStatus.InitializationInProgress && Interlocked.Exchange(ref _isInitializing, 1) == 0) {
                if (!File.Exists(TranslationFileFileName)) {
                    string message = $"Unable to open Langauge file ({Path.GetFullPath(TranslationFileFileName)}).";
                    /*should be logger*/Console.WriteLine(message);
                    return;
                }

                _backgroundWorker = new BackgroundWorker();
                _backgroundWorker.DoWork += LoadExcelLanguageFileAsync;
                _backgroundWorker.RunWorkerCompleted += LoadExcelLanguageFileAsyncCompleted;
                _backgroundWorker.WorkerSupportsCancellation = true;

                _backgroundWorker.RunWorkerAsync();
            }
        }

        /// <summary>
        /// save main sheet as old, if old doesn't exist jet
        /// </summary>
        private void CopyOldExcelFile()
        {
            if (OldTranslationFileFileName == null) return;

            string translationFileFileNameFullPath = Path.GetFullPath(TranslationFileFileName);
            string oldTranslationFileFileNameFullPath = Path.GetFullPath(OldTranslationFileFileName);
            if (!File.Exists(oldTranslationFileFileNameFullPath))
            {
                try
                {
                    File.Copy(translationFileFileNameFullPath, oldTranslationFileFileNameFullPath, true);
                }
                catch (IOException)
                {
                    Console.WriteLine($@"Unable to save Langage File as '{OldTranslationFileFileName}'.");
                }
            }
        }

    }
}
