using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Internationalization.Utilities
{
    public static class ExcelCellToDictionaryUtils
    {
        public static void FillSubDictionaries(Dictionary<CultureInfo, Dictionary<string, string>> dictionary,
            object[,] excelCells, int maxColumn, int numKeyParts)
        {
            for (var langIndex = numKeyParts + 1; langIndex <= maxColumn; langIndex++)
            {
                var lang = CultureInfoUtil.GetCultureInfo(ExcelCellToString(excelCells[1, langIndex]),
                    true);

                if (!dictionary.ContainsKey(lang))
                {
                    dictionary.Add(lang, new Dictionary<string, string>());
                }
            }
        }

        /// <summary>
        /// Counts how many columns are reserved for the translation key, based on the
        /// first column that can be identified as a language column.
        /// </summary>
        /// <param name="excelCells">The array conatining all cell values out of the sheet.</param>
        /// <returns>The number of columns reserved for the translation key.</returns>
        public static int GetNumberOfKeyParts(object[,] excelCells)
        {
            var maxColumn = excelCells.GetUpperBound(1);

            for (var column = 1; column < maxColumn; column++)
            {
                var culture = CultureInfoUtil.GetCultureInfoOrDefault(ExcelCellToString(excelCells[1, column]),
                    true);

                if (culture != null)
                {
                    return column - 1;
                }
            }

            return maxColumn;
        }

        public static string ExcelCellToDictionaryKey(object[,] excelCells, int row, int numKeyParts,
            bool isGlossaryEntry, string glossaryTag, ref int numberOfGlossaryEntries)
        {
            string key;

            if (isGlossaryEntry)
            {
                key = glossaryTag + numberOfGlossaryEntries;
                numberOfGlossaryEntries++;
            }
            else
            {
                var keyColumnCells = new string[numKeyParts];
                for (var i = 0; i < numKeyParts; i++)
                {
                    keyColumnCells[i] = ExcelCellToString(excelCells[row, i + 1]);
                }

                key = CreateGuiDictionaryKey(keyColumnCells);
            }

            return key;
        }

        public static string ExcelCellToString(object cellValue)
        {
            return cellValue == null ? String.Empty : cellValue.ToString();
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
    }
}