using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Internationalization.Utilities
{
    public static class ExcelCellToDictionaryUtils
    {
        public static void FillSubDictionaries(Dictionary<CultureInfo, Dictionary<string, string>> dictionary,
            object[,] excelCells, int maxColumn, int numberOfKeyParts)
        {
            for (var langIndex = numberOfKeyParts + 1; langIndex <= maxColumn; langIndex++)
            {
                var lang = CultureInfoUtil.GetCultureInfo(ExcelCellToString(excelCells[1, langIndex]),
                    true);

                if (!dictionary.ContainsKey(lang))
                {
                    dictionary.Add(lang, new Dictionary<string, string>());
                }
            }
        }

        //TODO doc
        public static Dictionary<CultureInfo, int> GetLanguageColumnsLookupTable(object[,] excelCells, int numberOfKeyParts)
        {
            var lookupTable = new Dictionary<CultureInfo, int>();
            var maxColumn = excelCells.GetUpperBound(1);

            for (var column = GetNumberOfKeyParts(excelCells) + 1; column <= maxColumn; column++)
            {
                var culture = CultureInfoUtil.GetCultureInfoOrDefault(
                    ExcelCellToString(excelCells[1, column]), true);
                lookupTable.Add(culture, column);
            }

            return lookupTable;
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

            for (var column = 1; column <= maxColumn; column++)
            {
                var culture = CultureInfoUtil.GetCultureInfoOrDefault(
                    ExcelCellToString(excelCells[1, column]), true);

                if (culture != null)
                {
                    return column - 1;
                }
            }

            return maxColumn;
        }

        public static string ExcelCellToDictionaryKey(object[,] excelCells, int row, int numberOfKeyParts,
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
                var keyColumnCells = new string[numberOfKeyParts];
                for (var i = 0; i < numberOfKeyParts; i++)
                {
                    keyColumnCells[i] = ExcelCellToString(excelCells[row, i + 1]);
                }

                key = CreateGuiDictionaryKey(keyColumnCells);
            }

            return key;
        }

        public static string ExcelCellToString(object cellValue)
        {
            return cellValue == null ? string.Empty : cellValue.ToString();
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

        public static void AddExcelCellToDictionary(Dictionary<CultureInfo, Dictionary<string, string>> dictionary,
            object languageCell, object translationCell, string keyOfRow)
        {
            var lang = CultureInfoUtil.GetCultureInfo(
                ExcelCellToDictionaryUtils.ExcelCellToString(languageCell),
                true);

            var translationString = translationCell as string;
            if (!string.IsNullOrEmpty(translationString))
            {
                dictionary[lang].Add(keyOfRow, translationString);
            }
        }
    }
}