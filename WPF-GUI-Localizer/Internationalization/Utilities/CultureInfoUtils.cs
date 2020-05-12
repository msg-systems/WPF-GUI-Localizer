using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Internationalization.Exception;
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
            ExceptionLoggingUtils.ThrowIfNull(Logger, nameof(GetCultureInfo), (object) cultureName,
                nameof(cultureName), "Unable to generate CultureInfo object from null sting.");
            
            var culture = GetCultureInfoOrDefault(cultureName, onlyBracketsAtEndOfString);

            ExceptionLoggingUtils.ThrowIf(culture == null, Logger,
                new CultureNotFoundException($"Unable to generate CultureInfo object from string ({cultureName})."),
                "GetCultureInfo received invalid culture name.");

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
        /// and <paramref name="key"/>, if no entry is found in <paramref name="targetLanguage"/>, dictionaries of
        /// compatible languages are searched. Returns default value (null) if no entry for
        /// <paramref name="key"/> can be found in dictionaries campatible with <paramref name="targetLanguage"/>.
        /// </summary>
        /// <param name="baseDictionary">Dictionary collection to be searched.</param>
        /// <param name="targetLanguage">CultureInfo with which the used dictionary has to be compatible.</param>
        /// <param name="key">Key to search for in available dictionarys.</param>
        /// <param name="inputlanguage">
        /// The language the application was originally designed in. Used as a fallback.
        /// </param>
        /// <returns>
        /// Value for <paramref name="key"/> out of dictionary for <paramref name="targetLanguage"/>, its
        /// parent (usually same as two letter name), its two letter name (e.g. en for en-US), its patents two
        /// letter name, <see cref="CultureInfo.InvariantCulture"/>, the <paramref name="inputlanguage"/> or
        /// null if no compatible dictionary can be found in <paramref name="baseDictionary"/>
        /// (Dictionaries will be searched in this order).
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either <paramref name="baseDictionary"/>, <paramref name="targetLanguage"/> or
        /// <paramref name="key"/> is null.
        /// </exception>
        public static string GetLanguageDictValueOrDefault(
            Dictionary<CultureInfo, Dictionary<string, string>> baseDictionary, CultureInfo targetLanguage, string key,
            CultureInfo inputlanguage)
        {
            //null checks.
            ExceptionLoggingUtils.VerifyMultiple(baseDictionary, nameof(baseDictionary))
                .AlsoVerify(targetLanguage, nameof(targetLanguage))
                .AlsoVerify(key, nameof(key))
                .ThrowIfNull(Logger, nameof(GetLanguageDictValueOrDefault),
                    "Unable to pick dictionary with null parameter.");

            //searching baseDictionary.
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

            if (baseDictionary.ContainsKey(CultureInfo.InvariantCulture) &&
                baseDictionary[CultureInfo.InvariantCulture].ContainsKey(key))
            {
                return baseDictionary[CultureInfo.InvariantCulture][key];
            }

            if (baseDictionary.ContainsKey(inputlanguage) && baseDictionary[inputlanguage].ContainsKey(key))
            {
                return baseDictionary[inputlanguage][key];
            }

            //default.
            return null;
        }
    }
}