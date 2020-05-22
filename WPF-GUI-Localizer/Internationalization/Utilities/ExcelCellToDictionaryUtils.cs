using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Internationalization.Utilities
{
    public static class ExcelCellToDictionaryUtils
    {
        /// <summary>
        /// Adds entries to <paramref name="dictionary"/>, based on the languages in <paramref name="excelCells"/>
        /// </summary>
        /// <exception cref="CultureNotFoundException">
        /// Thrown, if one of the cells of the header rows in <paramref name="excelCells"/> is not recognized as
        /// a language.
        /// </exception>
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

        /// <summary>
        /// Returns a dictionary that maps <see cref="CultureInfo"/> objects to their column of
        /// <paramref name="excelCells"/>, in which they appear.
        /// </summary>
        /// <exception cref="CultureNotFoundException">
        /// Thrown, if one of the cells of the header rows in <paramref name="excelCells"/> is not recognized as
        /// a language.
        /// </exception>
        public static Dictionary<CultureInfo, int> GetLanguageColumnsLookupTable(object[,] excelCells,
            int numberOfKeyParts)
        {
            var lookupTable = new Dictionary<CultureInfo, int>();
            var maxColumn = excelCells.GetUpperBound(1);

            for (var column = GetNumberOfKeyParts(excelCells) + 1; column <= maxColumn; column++)
            {
                var culture = CultureInfoUtil.GetCultureInfo(
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

        /// <summary>
        /// Converts the key cells of <paramref name="row"/> into the format in which they are used as dictionary keys.
        /// </summary>
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

        /// <summary>
        /// Adds an entry consisting of <paramref name="keyOfRow"/> and the translation in
        /// <paramref name="translationCell"/> to <paramref name="dictionary"/> for the
        /// language in <paramref name="languageCell"/>.
        /// If the language in <paramref name="languageCell"/> cannot be found or the
        /// <paramref name="translationCell"/> is empty, <paramref name="dictionary"/> will not be updated.
        /// </summary>
        public static void TryAddExcelCellToDictionary(Dictionary<CultureInfo, Dictionary<string, string>> dictionary,
            object languageCell, object translationCell, string keyOfRow)
        {
            var lang = CultureInfoUtil.GetCultureInfoOrDefault(
                ExcelCellToString(languageCell), true);

            if (lang == null)
            {
                return;
            }

            var translationString = translationCell as string;
            if (!string.IsNullOrEmpty(translationString))
            {
                dictionary[lang].Add(keyOfRow, translationString);
            }
        }

        private static string ExcelCellToString(object cellValue)
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
    }
}