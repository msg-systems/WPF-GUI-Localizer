using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Internationalization.Exception;

namespace Internationalization.Utilities
{
    public static class CultureInfoUtil
    {
        /// <param name="cultureName">string representation of CultureInfo as language tag</param>
        /// <param name="onlyBracketsAtEndOfString">will default back to false if no matching brackets are found</param>
        /// <exception cref="CultureNotFoundException">exception is thrown if language tag cannot be found within list of languages supported by .NET</exception>
        public static CultureInfo GetCultureInfo(string cultureName, bool onlyBracketsAtEndOfString)
        {
            int begin = cultureName.LastIndexOf(@"(", StringComparison.Ordinal) + 1;
            int length = cultureName.LastIndexOf(@")", StringComparison.Ordinal) - begin;
            if (onlyBracketsAtEndOfString && begin > 0 && length > 0)
            {
                cultureName = cultureName.Substring(begin, length);
            }

            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            if (!allCultures.Select(c => c.Name).Contains(cultureName) || string.IsNullOrEmpty(cultureName))
            {
                throw new CultureNotFoundException();
            }

            return new CultureInfo(cultureName);
        }

        /// <summary>
        /// Finds the dictionary for <see cref="targetLanguage"/>, its parent (usually same as two letter name), its two
        /// letter name (e.g. en for en-US), its patents two letter version or returns null if no compatible dictionary
        /// can be found in <see cref="baseDictionary"/>.
        /// </summary>
        /// <param name="baseDictionary">
        /// Dictionary collection to be searched; Will return null if this parameter is null;
        /// </param>
        /// <param name="targetLanguage">
        /// CultureInfo with which the return dictionary has to be compatible; Will return null if this parameter is null;
        /// </param>
        /// <returns>
        /// Dictionary for <see cref="targetLanguage"/>, its parent (usually same as two letter name), its two
        /// letter name (e.g. en for en-US), its patents two letter version or null if no compatible dictionary
        /// can be found in <see cref="baseDictionary"/> (Dictionaries will be sreached in this order).
        /// </returns>
        public static Dictionary<string, string> TryGetLanguageDict(
            Dictionary<CultureInfo, Dictionary<string, string>> baseDictionary, CultureInfo targetLanguage)
        {
            if (baseDictionary == null || targetLanguage == null)
            {
                return null;
            }

            if (baseDictionary.ContainsKey(targetLanguage))
            {
                return baseDictionary[targetLanguage];
            }

            CultureInfo parentCultureInfo = targetLanguage.Parent;
            if (baseDictionary.ContainsKey(parentCultureInfo))
            {
                return baseDictionary[parentCultureInfo];
            }

            CultureInfo twoLetterTarget = new CultureInfo(targetLanguage.TwoLetterISOLanguageName);
            if (baseDictionary.ContainsKey(twoLetterTarget))
            {
                return baseDictionary[twoLetterTarget];
            }

            CultureInfo twoLetterParent = new CultureInfo(parentCultureInfo.TwoLetterISOLanguageName);
            if (baseDictionary.ContainsKey(twoLetterParent))
            {
                return baseDictionary[twoLetterParent];
            }

            return null;
        }
    }
}
