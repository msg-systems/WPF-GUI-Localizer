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
        ///     Will default back to false if no matching brackets are found.
        /// </param>
        /// <exception cref="CultureNotFoundException">
        ///     Thrown if language tag cannot be found within list of languages supported by .NET.
        /// </exception>
        /// <exception cref="ArgumentNullException">Thrown if culture string given is null.</exception>
        public static CultureInfo GetCultureInfo(string cultureName, bool onlyBracketsAtEndOfString)
        {
            ExceptionLoggingUtils.ThrowIfNull(Logger, nameof(GetCultureInfo), (object) cultureName,
                nameof(cultureName), "Unable to generate CultureInfo object from null sting.");

            var culture = GetCultureInfoOrDefaultInternal(cultureName, onlyBracketsAtEndOfString, out var searched);

            ExceptionLoggingUtils.ThrowIf(culture == null, Logger,
                new CultureNotFoundException(cultureName, searched,
                    "Unable to generate CultureInfo object from string."),
                "GetCultureInfo received invalid culture name.");

            return culture;
        }

        /// <summary>
        ///     Returns the CultureInfo object corresponding to language code given.
        ///     Defaults to null, if the culture was not found
        /// </summary>
        /// <param name="cultureName">String representation of CultureInfo as language tag.</param>
        /// <param name="onlyBracketsAtEndOfString">
        ///     Will default back to false if no matching brackets are found.
        /// </param>
        public static CultureInfo GetCultureInfoOrDefault(string cultureName, bool onlyBracketsAtEndOfString)
        {
            if (cultureName == null)
            {
                return null;
            }

            return GetCultureInfoOrDefaultInternal(cultureName, onlyBracketsAtEndOfString, out _);
        }

        /// <summary>
        ///     Finds the translation entry out of <paramref name="baseDictionary" /> for <paramref name="targetLanguage" />
        ///     and <paramref name="key" />, if no entry is found in <paramref name="targetLanguage" />, dictionaries of
        ///     compatible languages are searched. Returns default value (null) if no entry for
        ///     <paramref name="key" /> can be found in dictionaries campatible with <paramref name="targetLanguage" />.
        /// </summary>
        /// <param name="baseDictionary">Dictionary collection to be searched.</param>
        /// <param name="targetLanguage">CultureInfo with which the used dictionary has to be compatible.</param>
        /// <param name="key">Key to search for in available dictionarys.</param>
        /// <param name="inputLanguage">
        ///     The language the application was originally designed in. Used as a fallback.
        /// </param>
        /// <param name="useOnlyExactLanguage">If true, compatible languages and fallbacks are ignored.</param>
        /// <returns>
        ///     Value for <paramref name="key" /> out of dictionary for <paramref name="targetLanguage" />, its
        ///     parent (usually same as two letter name), its two letter name (e.g. en for en-US), its patents two
        ///     letter name, <see cref="CultureInfo.InvariantCulture" />, the <paramref name="inputLanguage" /> or
        ///     null if no compatible dictionary can be found in <paramref name="baseDictionary" />
        ///     (Dictionaries will be searched in this order).
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     Thrown if either <paramref name="baseDictionary" />, <paramref name="targetLanguage" /> or
        ///     <paramref name="key" /> is null.
        /// </exception>
        public static string GetLanguageDictValueOrDefault(
            Dictionary<CultureInfo, Dictionary<string, string>> baseDictionary, CultureInfo targetLanguage, string key,
            CultureInfo inputLanguage, bool useOnlyExactLanguage)
        {
            //null checks.
            ExceptionLoggingUtils.VerifyMultiple(baseDictionary, nameof(baseDictionary))
                .AlsoVerify(targetLanguage, nameof(targetLanguage))
                .AlsoVerify(key, nameof(key))
                .AlsoVerify(inputLanguage, nameof(inputLanguage))
                .ThrowIfNull(Logger, nameof(GetLanguageDictValueOrDefault),
                    "Unable to pick dictionary with null parameter.");

            //searching baseDictionary.
            if (baseDictionary.ContainsKey(targetLanguage) && baseDictionary[targetLanguage].ContainsKey(key))
            {
                return baseDictionary[targetLanguage][key];
            }

            if (useOnlyExactLanguage)
            {
                return null;
            }

            var parentCultureInfo = targetLanguage.Parent;
            if (baseDictionary.ContainsKey(parentCultureInfo) && baseDictionary[parentCultureInfo].ContainsKey(key))
            {
                return baseDictionary[parentCultureInfo][key];
            }

            var twoLetterTarget = new CultureInfo(targetLanguage.TwoLetterISOLanguageName);
            if (baseDictionary.ContainsKey(twoLetterTarget) && baseDictionary[twoLetterTarget].ContainsKey(key))
            {
                return baseDictionary[twoLetterTarget][key];
            }

            var twoLetterParent = new CultureInfo(parentCultureInfo.TwoLetterISOLanguageName);
            if (baseDictionary.ContainsKey(twoLetterParent) && baseDictionary[twoLetterParent].ContainsKey(key))
            {
                return baseDictionary[twoLetterParent][key];
            }

            if (baseDictionary.ContainsKey(CultureInfo.InvariantCulture) &&
                baseDictionary[CultureInfo.InvariantCulture].ContainsKey(key))
            {
                return baseDictionary[CultureInfo.InvariantCulture][key];
            }

            if (baseDictionary.ContainsKey(inputLanguage) && baseDictionary[inputLanguage].ContainsKey(key))
            {
                return baseDictionary[inputLanguage][key];
            }

            //default.
            return null;
        }

        private static CultureInfo GetCultureInfoOrDefaultInternal(string cultureName, bool onlyBracketsAtEndOfString,
            out string partOfNameSearched)
        {
            partOfNameSearched = cultureName;

            var begin = cultureName.LastIndexOf(@"(", StringComparison.Ordinal) + 1;
            var length = cultureName.LastIndexOf(@")", StringComparison.Ordinal) - begin;
            if (onlyBracketsAtEndOfString && begin > 0 && length > 0)
            {
                partOfNameSearched = cultureName.Substring(begin, length);
            }
            //invariant culutre has empty name (output by converter: "Invariant Language (Invariant Country) ()")
            else if (onlyBracketsAtEndOfString && begin > 0 && length == 0)
            {
                return CultureInfo.InvariantCulture;
            }

            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            if (!allCultures.Select(c => c.Name).Contains(partOfNameSearched) ||
                string.IsNullOrEmpty(partOfNameSearched))
            {
                return null;
            }

            return new CultureInfo(partOfNameSearched);
        }
    }
}