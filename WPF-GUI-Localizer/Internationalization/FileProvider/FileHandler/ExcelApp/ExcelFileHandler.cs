﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Internationalization.Enum;
using Internationalization.FileProvider.FileHandler.Universal;
using Internationalization.Model;
using Internationalization.Utilities;
using Microsoft.Extensions.Logging;
using ExcelInterop = Microsoft.Office.Interop.Excel;

namespace Internationalization.FileProvider.FileHandler.ExcelApp
{
    public class ExcelFileHandler : UniversalFileHandler
    {
        private static ILogger _logger;

        private readonly string _glossaryTag;

        /// <summary>
        /// The path of the excel table.
        /// It is not assumed that the <see cref="UniversalFileHandler.GetPathAndHandleProblems"/> function
        /// was called previously.
        /// <see cref="UniversalFileHandler.GetPathAndHandleProblems"/> will automatically be called, if
        /// <see cref="Path"/> is used.
        /// </summary>
        public string Path { get; set; }

        public ExcelFileHandler(Type typeOfUser, string glossaryTag)
            : base($"({nameof(ExcelFileHandler)} for {typeOfUser.Name})", ".xlsx")
        {
            _logger = GlobalSettings.LibraryLoggerFactory.CreateLogger<ExcelFileHandler>();
            _glossaryTag = glossaryTag;
        }

        /// <summary>
        /// Work for BackgroundWorker.
        /// Loads the excel sheet at <see cref="Path"/> and saves it as its result.
        /// <see cref="Path"/> has to be set to a value, before this handler is used.
        /// </summary>
        /// <exception cref="ArgumentNullException">
        /// Thrown, if <paramref name="sender"/> or <paramref name="e"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown, if <paramref name="sender"/> is not of type BackgroundWorker.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown, if <see cref="Path"/> is not set before this function is called.
        /// </exception>
        public void LoadExcelLanguageFileAsync(object sender, DoWorkEventArgs e)
        {
            var bw = sender as BackgroundWorker;

            //null and argument checks.
            if (e == null)
            {
                var ex = new ArgumentNullException(
                    "DoWorkEventArgs for DoWork event handler is null.", nameof(e));
                _logger.Log(LogLevel.Error, ex,
                    "LoadExcelLanguageFileAsync functions was called without DoWorkEventArgs.");
                throw ex;
            }

            if (bw == null)
            {
                if (sender == null)
                {
                    var ex = new ArgumentNullException(
                        "Sender for DoWork event handler is null.", nameof(sender));
                    _logger.Log(LogLevel.Error, ex,
                        "LoadExcelLanguageFileAsync functions was called without BackgroundWorker.");
                    throw ex;
                }
                else
                {
                    var ex = new ArgumentException(
                        "Sender for DoWork event handler is not of type BackgroundWorker.", nameof(sender));
                    _logger.Log(LogLevel.Error, ex,
                        "LoadExcelLanguageFileAsync functions was called without BackgroundWorker.");
                    throw ex;
                }
            }

            if (Path == null)
            {
                var ex = new NotSupportedException("Path property for DoWork event handler was not set.");
                _logger.Log(LogLevel.Error, ex,
                    "LoadExcelLanguageFileAsync functions was called without Path property being set beforehand.");
                throw ex;
            }

            _logger.Log(LogLevel.Trace, "LoadExcelLanguageFileAsync functions was called by BackgroundWorker.");

            LoadExcelLanguageFileAsyncInternal(bw, e);
        }

        /// <summary>
        /// Clean up after BackgroundWorker finished.
        /// Throws Exceptions, but cannot interupt main thread.
        /// </summary>
        /// <exception cref="FileFormatException">
        /// Thrown, if file at <see cref="Path"/> is not a valid Excel sheet.
        /// </exception>
        public void LoadExcelLanguageFileAsyncCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (e.Cancelled)
            {
                _logger.Log(LogLevel.Trace, "Loading of the language file was stoped.");
            }
            else if (e.Error != null)
            {
                if (e.Error.HResult == -2146827284)
                {
                    var ex = new FileFormatException(new Uri(Path),
                        "Expected Excel file format.", e.Error);
                    _logger.Log(LogLevel.Error, ex,
                        "File at given path may be corrupted or not have correct format. " +
                        "Expected Excel sheet (.xlsx, .xls, ...).");
                    throw ex;
                }
                else
                {
                    _logger.Log(LogLevel.Error, e.Error, "Unknown error occurred during language file loading.");
                }
            }
            else
            {
                _logger.Log(LogLevel.Trace, "BackgroundWorker successfully finished loading the language file.");
            }
        }

        /// <summary>
        /// Can be used to update the excel sheet at <see cref="Path"/> with
        /// <paramref name="translationsDictionary"/>, write the first entry into an empty sheet
        /// or create a new excel sheet based on <paramref name="translationsDictionary"/>.
        /// This function does not currently catch or log any Exceptions.
        /// </summary>
        /// <param name="translationsDictionary">
        /// The dictionary that should be written into the excel sheet. If empty or null, a new and empty
        /// sheet will be created at <see cref="Path"/>.
        /// </param>
        public void ExcelWriteActions(Dictionary<CultureInfo, Dictionary<string, string>> translationsDictionary)
        {
            string path = GetPathAndHandleProblems(Path);
            FileCreationType fcType;

            if (translationsDictionary == null || translationsDictionary.Count == 0)
            {
                fcType = FileCreationType.CreateEmptyFile;
            }
            else if (File.Exists(System.IO.Path.GetFullPath(path)))
            {
                fcType = FileCreationType.UpdateExistingFile;
            }
            else if (!Directory.Exists(System.IO.Path.GetFullPath(path)))
            {
                fcType = FileCreationType.CreateNewFile;
            }
            else
            {
                fcType = FileCreationType.CreateNoFile;
            }

            switch (fcType)
            {
                case FileCreationType.UpdateExistingFile:
                    _logger.Log(LogLevel.Trace,
                        $"Existing translation file will be updated ({System.IO.Path.GetFullPath(path)}).");

                    break;
                case FileCreationType.CreateNewFile:
                    _logger.Log(LogLevel.Information,
                        $"Unable to find language file ({System.IO.Path.GetFullPath(path)}). A new one will be created.");

                    break;
                case FileCreationType.CreateEmptyFile:
                    _logger.Log(LogLevel.Trace, $"New empty file will be created ({System.IO.Path.GetFullPath(path)}).");

                    break;
                default:
                    _logger.Log(LogLevel.Debug,
                        $"No files will be updated for given FileCreationType #{fcType} "
                        + $"({System.IO.Path.GetFullPath(path)}).");

                    //no further processing needed.
                    return;
            }

            CreateExelFileBasedOnCreationType(fcType, translationsDictionary, path);
        }

        private void LoadExcelLanguageFileAsyncInternal(BackgroundWorker bw, DoWorkEventArgs e)
        {
            //set up everything for ReadWorksheetTranslations.
            string path = GetPathAndHandleProblems(Path);
            var excel = new ExcelInterop.Application();
            var workbook = excel.Workbooks.Open(System.IO.Path.GetFullPath(path));
            var resultDict = new Dictionary<CultureInfo, Dictionary<string, string>>();

            try
            {
                if (!bw.CancellationPending)
                {
                    _logger.Log(LogLevel.Trace, "Reading excel file not aborted.");
                    var worksheet = (ExcelInterop.Worksheet) workbook.Worksheets[1];
                    resultDict = ReadWorksheetTranslations(worksheet);
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

            //finish work.
            e.Cancel = bw.CancellationPending;
            e.Result = resultDict;
        }

        private Dictionary<CultureInfo, Dictionary<string, string>> ReadWorksheetTranslations(ExcelInterop.Worksheet worksheet)
        {
            //initialize needed variables.
            var readDict = new Dictionary<CultureInfo, Dictionary<string, string>>();
            var numberOfGlossaryEntries = 0;
            object[,] values = worksheet.UsedRange.get_Value();
            var maxRow = values.GetUpperBound(0);
            var maxColumn = values.GetUpperBound(1);
            int numKeyParts = ExcelCellToDictionaryUtils.GetNumberOfKeyParts(values);
            //first row only contains column titles (can be null in cell 1,1), no data.
            var row = 2;

            ExcelCellToDictionaryUtils.FillSubDictionaries(readDict, values, maxColumn, numKeyParts);

            //logging.
            _logger.Log(LogLevel.Debug,
                $"Found {numKeyParts} columns for key parts and {maxColumn - numKeyParts} language columns.");
            _logger.Log(LogLevel.Trace, "Now reading rows from excel sheet.");

            //reading rows.
            while (row <= maxRow && values[row, 1] != null)
            {
                var isGlossaryEntry = _glossaryTag != null && _glossaryTag.Equals(values[row, 1]);

                //check if current row has a comment
                //or is part of glossary (assuming a glossary is being used).
                if (values[row, 2] == null && !isGlossaryEntry)
                {
                    _logger.Log(LogLevel.Debug, $"Skipped row #{row}, because it is a comment.");
                    row++;
                    continue;
                }

                //get key.
                string key = ExcelCellToDictionaryUtils.ExcelCellToDictionaryKey(
                    values, row, numKeyParts, isGlossaryEntry, _glossaryTag, ref numberOfGlossaryEntries);

                //add translations to dictionary.
                for (var langIndex = numKeyParts + 1; langIndex <= maxColumn; langIndex++)
                {
                    var lang = CultureInfoUtil.GetCultureInfo(
                        ExcelCellToDictionaryUtils.ExcelCellToString(values[1, langIndex]),
                        true);
                    readDict[lang].Add(key,
                        ExcelCellToDictionaryUtils.ExcelCellToString(values[row, langIndex]));
                }

                row++;
            }

            return readDict;
        }

        /// <summary>
        /// Creates objects needed for the writing process and starts it.
        /// </summary>
        /// <param name="fileCreationType">
        /// The strategy for creating the excel file.
        /// It is assumed that the function is only called with <see cref="FileCreationType.UpdateExistingFile"/>,
        /// <see cref="FileCreationType.CreateEmptyFile"/> or <see cref="FileCreationType.CreateNewFile"/>
        /// as possible values.
        /// </param>
        /// <param name="translationsDictionary">
        /// The translations that should be written into the dictionary.
        /// </param>
        /// <param name="path">The path of the excel sheet.</param>
        private void CreateExelFileBasedOnCreationType(FileCreationType fileCreationType,
            Dictionary<CultureInfo, Dictionary<string, string>> translationsDictionary, string path)
        {
            var excel = new ExcelInterop.Application();
            ExcelInterop.Workbook workbook = null;

            try
            {
                //FileCreationType.UpdateExistingFile
                if (fileCreationType == FileCreationType.UpdateExistingFile)
                {
                    workbook = excel.Workbooks.Open(System.IO.Path.GetFullPath(path));
                }
                //FileCreationType.CreateEmptyFile or FileCreationType.CreateNewFile
                else
                {
                    workbook = excel.Workbooks.Add();
                }

                //FileCreationType.UpdateExistingFile or FileCreationType.CreateNewFile
                if (fileCreationType != FileCreationType.CreateEmptyFile)
                {
                    //get parameters.
                    var worksheet = (ExcelInterop.Worksheet) workbook.Worksheets[1];
                    var textLocalizations =
                        TextLocalizationsUtils.FlipLocalizationsDictionary(translationsDictionary);

                    //write to sheet.
                    WriteTranslationsToWorksheet(worksheet, textLocalizations);
                }

                //saving.
                excel.DisplayAlerts = false;
                //FileCreationType.UpdateExistingFile.
                if (fileCreationType == FileCreationType.UpdateExistingFile)
                {
                    workbook.Save();
                }
                //FileCreationType.CreateEmptyFile or FileCreationType.CreateNewFile.
                else
                {
                    workbook.SaveAs(System.IO.Path.GetFullPath(path));
                }
            }
            //TODO no catch clause, due to missing documentation for Exceptions thrown ba excel Interop
            finally
            {
                workbook?.Close();
                excel.Quit();
            }
        }

        private void WriteTranslationsToWorksheet(ExcelInterop.Worksheet worksheet,
            Dictionary<string, List<TextLocalization>> texts)
        {
            //assignments that always work.
            var glossaryKey = new Regex($"^{_glossaryTag}\\d*$");
            var usedRange = worksheet.UsedRange;
            object[,] values = usedRange.Value;

            //assignments that fail, if sheet is empty or was just created.
            int maxColumn;
            Dictionary<CultureInfo, int> languageColumnLookup;
            int numberOfKeyParts;
            if (values == null)
            {
                numberOfKeyParts = texts.First().Key
                    .Split(Properties.Settings.Default.Seperator_for_partial_Literalkeys).Length;
                languageColumnLookup = new Dictionary<CultureInfo, int>();
                maxColumn = numberOfKeyParts;
            }
            else
            {
                numberOfKeyParts = ExcelCellToDictionaryUtils.GetNumberOfKeyParts(values);
                languageColumnLookup =
                    ExcelCellToDictionaryUtils.GetLanguageColumnsLookupTable(values, numberOfKeyParts);
                maxColumn = values.GetUpperBound(1);
            }

            foreach (var translation in texts)
            {
                //if current entry is part of glossary, skip writing.
                if (_glossaryTag != null && glossaryKey.IsMatch(translation.Key))
                {
                    continue;
                }

                var updatedRow = false;
                var lastFindForDialogIndex = -1;
                var keyParts = translation.Key.Split(Properties.Settings.Default.Seperator_for_partial_Literalkeys);

                keyParts = DictionaryToExcelCellUtils.SqueezeArrayIntoShapeIfNeeded(keyParts, numberOfKeyParts);

                //find first row, matching beginning of key.
                var currentDialogFind = usedRange.Find(keyParts[0], Type.Missing,
                    ExcelInterop.XlFindLookIn.xlValues, ExcelInterop.XlLookAt.xlPart,
                    ExcelInterop.XlSearchOrder.xlByRows, ExcelInterop.XlSearchDirection.xlNext, false);
                var firstDialogFind = currentDialogFind;

                //search for match with key.
                while (currentDialogFind != null)
                {
                    //values array, usedRange, maxColumns and languagesColumnLookup may change if
                    //Excel sheet needs to be altered.
                    updatedRow = DictionaryToExcelCellUtils.TryUpdateRow(worksheet, ref usedRange, ref values, ref maxColumn, languageColumnLookup,
                        currentDialogFind, translation.Value, keyParts, numberOfKeyParts);

                    lastFindForDialogIndex = currentDialogFind.Row;
                    currentDialogFind = usedRange.FindNext(currentDialogFind);

                    //stop, if row was updated or no entries in sheet matched fully.
                    if (updatedRow || currentDialogFind.Address == firstDialogFind.Address)
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

                //values array, usedRange, maxColumns and languagesColumnLookup may change if Excel sheet
                //needs to be altered.
                DictionaryToExcelCellUtils.WriteNewRow(worksheet, ref usedRange, ref values, ref maxColumn,
                    languageColumnLookup, lastFindForDialogIndex, translation.Value, keyParts, numberOfKeyParts);
            }
        }
    }
}
