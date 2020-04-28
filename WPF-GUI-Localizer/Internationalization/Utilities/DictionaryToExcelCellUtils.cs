using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
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
        public static bool TryUpdateRow(ExcelInterop.Worksheet worksheetGui, object[,] excelCells,
            ExcelInterop.Range currentDialogFind, IList<TextLocalization> localizedTexts, string[] keyParts,
            int numKeyParts, int maxColumn)
        {
            //get rest of key from sheet.
            ExcelInterop.Range currentRow = worksheetGui.Rows[currentDialogFind.Row];
            var keyColumnsCells = new string[numKeyParts];
            for (var i = 0; i < numKeyParts; i++)
            {
                keyColumnsCells[i] = currentRow.Cells[i + 1].Value;
            }

            //check if whole key matches.
            if (keyColumnsCells.SequenceEqual(keyParts))
            {
                //now write to cell, values array and maxColumns may change if Excel sheet needs to be altered.
                WriteToCell(localizedTexts, ref excelCells, ref maxColumn, currentRow.Row,
                    worksheetGui, numKeyParts);
                return true;
            }

            return false;
        }

        public static void WriteNewRow(ExcelInterop.Worksheet worksheetGui, object[,] excelCells, int lastFindForDialogIndex,
            IList<TextLocalization> localizedTexts, string[] keyParts, int numKeyParts, int maxColumn)
        {
            ExcelInterop.Range newRow;

            //try writing new line next to others with same key beginning.
            if (lastFindForDialogIndex >= 0)
            {
                newRow = worksheetGui.Rows[lastFindForDialogIndex + 1];
                newRow.Insert();
                //get inserted row.
                newRow = worksheetGui.Rows[lastFindForDialogIndex + 1];
                Logger.Log(LogLevel.Trace, "Entry was inserted after similar keys.");
            }
            //if first part (or whole key for single key fragment setups like ResourceLiteralProvider)
            //can't be found write new line at end of sheet.
            else
            {
                var lastRow =
                    worksheetGui.Cells.SpecialCells(ExcelInterop.XlCellType.xlCellTypeLastCell);
                var indexlastRow = lastRow.Row;
                newRow = worksheetGui.Rows[indexlastRow + 1];
                Logger.Log(LogLevel.Trace, "Entry was added to end of excel sheet.");
            }

            //write new key parts.
            for (var i = 0; i < numKeyParts; i++)
            {
                newRow.Cells[i + 1] = keyParts[i];
            }

            //write new texts, values array and maxColumns may change if Excel sheet needs to be altered.
            WriteToCell(localizedTexts, ref excelCells, ref maxColumn, newRow.Row, worksheetGui, numKeyParts);
        }

        private static void WriteToCell(IEnumerable<TextLocalization> texts, ref object[,] excelCells, ref int maxColumn,
            int currentRow, ExcelInterop.Worksheet worksheetGui, int numKeyParts)
        {
            foreach (var text in texts)
            {
                //identify index for current language.
                int langIndex;
                for (langIndex = numKeyParts + 1; langIndex <= maxColumn; langIndex++)
                {
                    if (Equals(CultureInfoUtil.GetCultureInfo(ExcelCellToDictionaryUtils.ExcelCellToString(excelCells[1, langIndex]),
                            true), text.Language))
                    {
                        break;
                    }
                }

                if (langIndex > maxColumn)
                {
                    //if language doesn't exist in sheet, write new language string in header row of new column.
                    worksheetGui.Cells[1, langIndex].Value = $"({text.Language.Name})";
                    maxColumn++;
                    excelCells = worksheetGui.UsedRange.get_Value();
                    Logger.Log(LogLevel.Information,
                        $"New language ({text.Language.EnglishName}) previously not part of excel sheet "
                        + "detected and added to excel sheet.");
                }

                ExcelInterop.Range targetCellToUpdate = worksheetGui.Cells[currentRow, langIndex];
                targetCellToUpdate.Value = text.Text;
            }
        }
    }
}
