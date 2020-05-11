using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Internationalization.Utilities
{
    public static class CultureInfoUtil
    {
        private static readonly ILogger Logger;

        static CultureInfoUtil()
        {
            Logger = GlobalSettings.LibraryLoggerFactory.CreateLogger(typeof(CultureInfoUtil));
        }

        /// <summary>Returns the CultureInfo object corresponding to language code given</summary>
        /// <param name="cultureName">String representation of CultureInfo as language tag.</param>
        /// <param name="onlyBracketsAtEndOfString">
        /// Will default back to false if no matching brackets are found.
        /// </param>
        /// <exception cref="CultureNotFoundException">
        /// Thrown if language tag cannot be found within list of languages supported by .NET.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if culture string given is null.</exception>
        public static CultureInfo GetCultureInfo(string cultureName, bool onlyBracketsAtEndOfString)
        {
            if (cultureName == null)
            {
                var e = new ArgumentNullException(nameof(cultureName),
                    "Unable to generate CultureInfo object from null sting.");
                Logger.Log(LogLevel.Error, e, "GetCultureInfo received null parameter.");
                throw e;
            }

            var culture = GetCultureInfoOrDefault(cultureName, onlyBracketsAtEndOfString);

            if (culture == null)
            {
                var e = new CultureNotFoundException(
                    $"Unable to generate CultureInfo object form string ({cultureName}).");
                Logger.Log(LogLevel.Error, e, "GetCultureInfo received null parameter.");
                throw e;
            }

            return culture;
        }

        /// <summary>
        /// Returns the CultureInfo object corresponding to language code given.
        /// Defaults to null, if the culture was not found</summary>
        /// <param name="cultureName">String representation of CultureInfo as language tag.</param>
        /// <param name="onlyBracketsAtEndOfString">
        /// Will default back to false if no matching brackets are found.
        /// </param>
        public static CultureInfo GetCultureInfoOrDefault(string cultureName, bool onlyBracketsAtEndOfString)
        {
            if (cultureName == null)
            {
                return null;
            }

            var begin = cultureName.LastIndexOf(@"(", StringComparison.Ordinal) + 1;
            var length = cultureName.LastIndexOf(@")", StringComparison.Ordinal) - begin;
            string newCultureName;
            if (onlyBracketsAtEndOfString && begin > 0 && length > 0)
            {
                newCultureName = cultureName.Substring(begin, length);
            }
            else
            {
                newCultureName = cultureName;
            }

            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            if (!allCultures.Select(c => c.Name).Contains(newCultureName) || string.IsNullOrEmpty(newCultureName))
            {
                return null;
            }

            return new CultureInfo(newCultureName);
        }

        /// <summary>
        /// Finds the translation entry out of <paramref name="baseDictionary"/> for <paramref name="targetLanguage"/>
        /// and <paramref name="key"/>, if no entry is found for <paramref name="targetLanguage"/>, its parent
        /// (usually same as two letter name), its two letter name (e.g. en for en-US) and its patents two letter
        /// version are searched for <paramref name="key"/>. Returns default value (null) if no entry for
        /// <paramref name="key"/> can be found in dictionarys campatible with <paramref name="targetLanguage"/>.
        /// </summary>
        /// <param name="baseDictionary">
        /// Dictionary collection to be searched.
        /// </param>
        /// <param name="targetLanguage">
        /// CultureInfo with which the used dictionary has to be compatible.
        /// </param>
        /// <param name="key">
        /// Key to search for in available dictionarys.
        /// </param>
        /// <returns>
        /// Translation for <paramref name="key"/> out of dictionary for <paramref name="targetLanguage"/>, its
        /// parent (usually same as two letter name), its two letter name (e.g. en for en-US), its patents two
        /// letter version or null if no compatible dictionary can be found in <paramref name="baseDictionary"/>
        /// (Dictionaries will be searched in this order).
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either <paramref name="baseDictionary"/>, <paramref name="targetLanguage"/> or
        /// <paramref name="key"/> is null.
        /// </exception>
        public static string GetLanguageDictValueOrDefault(
            Dictionary<CultureInfo, Dictionary<string, string>> baseDictionary, CultureInfo targetLanguage, string key)
        {
            if (baseDictionary == null)
            {
                var e = new ArgumentNullException(nameof(baseDictionary),
                    "Unable to pick dictionary out of null dictionary.");
                Logger.Log(LogLevel.Error, e, "TryGetLanguageDict received null parameter.");
                throw e;
            }

            if (targetLanguage == null)
            {
                var e = new ArgumentNullException(nameof(baseDictionary),
                    "Unable to pick dictionary for null culture.");
                Logger.Log(LogLevel.Error, e, "TryGetLanguageDict received null parameter.");
                throw e;
            }

            if (key == null)
            {
                var e = new ArgumentNullException(nameof(key),
                    "Unable to pick translation for null key.");
                Logger.Log(LogLevel.Error, e, "TryGetLanguageDict received null parameter.");
                throw e;
            }

            if (baseDictionary.ContainsKey(targetLanguage) && baseDictionary[targetLanguage].ContainsKey(key))
            {
                return baseDictionary[targetLanguage][key];
            }

            var parentCultureInfo = targetLanguage.Parent;
            if (baseDictionary.ContainsKey(parentCultureInfo) && baseDictionary[targetLanguage].ContainsKey(key))
            {
                return baseDictionary[parentCultureInfo][key];
            }

            var twoLetterTarget = new CultureInfo(targetLanguage.TwoLetterISOLanguageName);
            if (baseDictionary.ContainsKey(twoLetterTarget) && baseDictionary[targetLanguage].ContainsKey(key))
            {
                return baseDictionary[twoLetterTarget][key];
            }

            var twoLetterParent = new CultureInfo(parentCultureInfo.TwoLetterISOLanguageName);
            if (baseDictionary.ContainsKey(twoLetterParent) && baseDictionary[targetLanguage].ContainsKey(key))
            {
                return baseDictionary[twoLetterParent][key];
            }

            return null;
        }
    }
}