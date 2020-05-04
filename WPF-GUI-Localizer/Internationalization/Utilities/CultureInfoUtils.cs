using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Internationalization.Utilities
{
    public static class CultureInfoUtil
    {
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
                //TODO logging
                throw new ArgumentNullException(nameof(cultureName),
                    "Unable to generate CultureInfo object from null sting.");
            }

            var culture = GetCultureInfoOrDefault(cultureName, onlyBracketsAtEndOfString);

            if(culture == null)
            {
                throw new CultureNotFoundException(
                    $"Unable to generate CultureInfo object form string ({cultureName}).");
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
        /// Finds the dictionary for <paramref name="targetLanguage"/>, its parent (usually same as two letter name), its two
        /// letter name (e.g. en for en-US), its patents two letter version or returns null if no compatible dictionary
        /// can be found in <paramref name="baseDictionary"/>.
        /// </summary>
        /// <param name="baseDictionary">
        /// Dictionary collection to be searched.
        /// </param>
        /// <param name="targetLanguage">
        /// CultureInfo with which the return dictionary has to be compatible.
        /// </param>
        /// <returns>
        /// Dictionary for <paramref name="targetLanguage"/>, its parent (usually same as two letter name), its two
        /// letter name (e.g. en for en-US), its patents two letter version or null if no compatible dictionary
        /// can be found in <paramref name="baseDictionary"/> (Dictionaries will be searched in this order).
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if either <paramref name="baseDictionary"/> or <paramref name="targetLanguage"/> are null.
        /// </exception>
        public static Dictionary<string, string> TryGetLanguageDict(
            Dictionary<CultureInfo, Dictionary<string, string>> baseDictionary, CultureInfo targetLanguage)
        {
            if (baseDictionary == null)
            {
                throw new ArgumentNullException(nameof(baseDictionary), "Unable to pick dictionary out of null dictionary.");
            }

            if (targetLanguage == null)
            {
                throw new ArgumentNullException(nameof(baseDictionary), "Unable to pick dictionary for null culture.");
            }

            if (baseDictionary.ContainsKey(targetLanguage))
            {
                return baseDictionary[targetLanguage];
            }

            var parentCultureInfo = targetLanguage.Parent;
            if (baseDictionary.ContainsKey(parentCultureInfo))
            {
                return baseDictionary[parentCultureInfo];
            }

            var twoLetterTarget = new CultureInfo(targetLanguage.TwoLetterISOLanguageName);
            if (baseDictionary.ContainsKey(twoLetterTarget))
            {
                return baseDictionary[twoLetterTarget];
            }

            var twoLetterParent = new CultureInfo(parentCultureInfo.TwoLetterISOLanguageName);
            if (baseDictionary.ContainsKey(twoLetterParent))
            {
                return baseDictionary[twoLetterParent];
            }

            return null;
        }
    }
}