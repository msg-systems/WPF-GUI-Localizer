using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Internationalization.Model;
using Microsoft.Extensions.Logging;
using ExcelInterop = Microsoft.Office.Interop.Excel;

namespace Internationalization.Utilities
{
    public static class DictionaryToExcelCellUtils
    {
        private static readonly ILogger Logger;

        static DictionaryToExcelCellUtils()
        {
            Logger = GlobalSettings.LibraryLoggerFactory.CreateLogger(typeof(DictionaryToExcelCellUtils));
        }

        /// <summary>
        /// If input is longer that desired, all additional values will be combined into the last element
        /// of the array. Smaller arrays will not be altered.
        /// </summary>
        /// <param name="origianlKeyParts">The original array that may not be of fitting lenght.</param>
        /// <param name="numberOfTargetKeyParts">The disired maximum langth of the array</param>
        /// <returns>
        /// <paramref name="origianlKeyParts"/> itself, if it is not too long or a new array based on
        /// <paramref name="origianlKeyParts"/> with a length of <paramref name="numberOfTargetKeyParts"/>
        /// and the overflowing elements compacted into the last element of the returned array, seperated
        /// by Properties.Settings.Default.Seperator_for_partial_Literalkeys, otherwise.
        /// </returns>
        public static string[] SqueezeArrayIntoShapeIfNeeded(string[] origianlKeyParts, int numberOfTargetKeyParts)
        {
            if (origianlKeyParts.Length > numberOfTargetKeyParts)
            {
                var newKeyParts = new string[numberOfTargetKeyParts];
                Array.Copy(origianlKeyParts, newKeyParts, numberOfTargetKeyParts);

                var diff = origianlKeyParts.Length - numberOfTargetKeyParts;
                for (var overflowKeyPart = 0; overflowKeyPart < diff; overflowKeyPart++)
                {
                    newKeyParts[numberOfTargetKeyParts - 1] += Properties.Settings.Default.Seperator_for_partial_Literalkeys;
                    newKeyParts[numberOfTargetKeyParts - 1] += origianlKeyParts[numberOfTargetKeyParts + overflowKeyPart];
                }

                return newKeyParts;
            }

            return origianlKeyParts;
        }

        //TODO doc
        //languageLookup can be altered.
        public static bool TryUpdateRow(ExcelInterop.Worksheet worksheet, ref ExcelInterop.Range usedRange,
            ref object[,] excelCells, ref int maxColumn, Dictionary<CultureInfo, int> languageColumnLookup,
            ExcelInterop.Range currentDialogFind, IList<TextLocalization> localizedTexts, string[] keyParts,
            int numberOfKeyParts)
        {
            //get rest of key from sheet.
            ExcelInterop.Range currentRow = worksheet.Rows[currentDialogFind.Row];
            var keyColumnsCells = new string[numberOfKeyParts];
            for (var i = 0; i < numberOfKeyParts; i++)
            {
                keyColumnsCells[i] = currentRow.Cells[i + 1].Value;
            }

            //check if whole key matches.
            if (keyColumnsCells.SequenceEqual(keyParts))
            {
                WriteToCell(worksheet, localizedTexts, ref usedRange, ref excelCells, ref maxColumn,
                    languageColumnLookup, currentRow.Row);
                return true;
            }

            return false;
        }

        public static void WriteNewRow(ExcelInterop.Worksheet worksheet, ref ExcelInterop.Range usedRange,
            ref object[,] excelcells, ref int maxColumn, Dictionary<CultureInfo, int> languageColumnLookup,
            int lastFindForDialogIndex, IList<TextLocalization> localizedTexts, string[] keyParts,
            int numberOfKeyParts)
        {
            ExcelInterop.Range newRow;

            //try writing new line next to others with same key beginning.
            if (lastFindForDialogIndex >= 0)
            {
                newRow = worksheet.Rows[lastFindForDialogIndex + 1];
                newRow.Insert();
                //get inserted row.
                newRow = worksheet.Rows[lastFindForDialogIndex + 1];
                Logger.Log(LogLevel.Trace, "Entry will be inserted after similar keys.");
            }
            //if first part (or whole key for single key fragment setups like ResourceLiteralProvider)
            //can't be found write new line at end of sheet.
            else
            {
                var lastRow =
                    worksheet.Cells.SpecialCells(ExcelInterop.XlCellType.xlCellTypeLastCell);
                var indexlastRow = lastRow.Row;
                newRow = worksheet.Rows[indexlastRow + 1];
                Logger.Log(LogLevel.Trace, "Entry will be added to end of excel sheet.");
            }

            //write new key parts.
            for (var i = 0; i < numberOfKeyParts; i++)
            {
                newRow.Cells[i + 1] = keyParts[i];
            }

            //write new texts, values array and maxColumns may change if Excel sheet needs to be altered.
            WriteToCell(worksheet, localizedTexts, ref usedRange, ref excelcells, ref maxColumn, languageColumnLookup, newRow.Row);
        }

        private static void WriteToCell(ExcelInterop.Worksheet worksheet, IEnumerable<TextLocalization> texts,
            ref ExcelInterop.Range usedRange, ref object[,] excelCells, ref int maxColumn,
            Dictionary<CultureInfo, int> languageColumnLookup, int currentRow)
        {
            foreach (var text in texts)
            {
                languageColumnLookup.TryGetValue(text.Language, out int langIndex);

                //value 0 for langIndex is impossible, if the language is in the sheet,
                //because Excel cells start at 1.
                if (langIndex == 0)
                {
                    //if language doesn't exist in sheet, write new language string in header row of new column
                    //and update all variables.
                    maxColumn++;
                    langIndex = maxColumn;
                    languageColumnLookup.Add(text.Language, langIndex);
                    worksheet.Cells[1, langIndex].Value = $"({text.Language.Name})";
                    usedRange = worksheet.UsedRange;
                    excelCells = usedRange.Value;

                    Logger.Log(LogLevel.Information,
                        $"New language ({text.Language.EnglishName}) previously not part of excel sheet " +
                        "was added to excel sheet.");
                }

                ExcelInterop.Range targetCellToUpdate = worksheet.Cells[currentRow, langIndex];
                targetCellToUpdate.Value = text.Text;
            }
        }
    }
}
