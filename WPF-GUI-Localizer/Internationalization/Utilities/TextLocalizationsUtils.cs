using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Internationalization.Model;

namespace Internationalization.Utilities
{
    public static class TextLocalizationsUtils
    {
        public static IEnumerable<string> ExtractKnownTranslations(string text, CultureInfo targetLanguage,
            Dictionary<CultureInfo, Dictionary<string, string>> allTranslations, CultureInfo inputLanguage)
        {
            ICollection<string> knownTranslations = new Collection<string>();

            allTranslations.TryGetValue(inputLanguage, out var sourceDictionary);

            if (string.IsNullOrWhiteSpace(text) || Equals(targetLanguage, inputLanguage) || sourceDictionary == null)
            {
                return knownTranslations;
            }

            //get all keys out of sourceDictionary, where value matches given text
            var fittingDictionaryEntries =
                sourceDictionary.Where(x => text.Equals(x.Value));

            //collect possible translations
            foreach (var entry in fittingDictionaryEntries)
            {
                var value = "";
                allTranslations.TryGetValue(targetLanguage, out var langDict);
                langDict?.TryGetValue(entry.Key, out value);

                //don't recommend the same translation twice
                if (!string.IsNullOrWhiteSpace(value) && !knownTranslations.Contains(value))
                {
                    knownTranslations.Add(value);
                }
            }

            return knownTranslations;
        }

        public static string GetRecommendedText(TextLocalization selectedText,
            ICollection<TextLocalization> localizedTexts,
            bool preferPreferedOverInputLangauge, CultureInfo inputLanguage, CultureInfo preferedLanguage)
        {
            var usePreferedInsted = preferPreferedOverInputLangauge;
            if (usePreferedInsted && Equals(selectedText.Language, preferedLanguage))
            {
                usePreferedInsted = false;
            }

            if (!usePreferedInsted && Equals(selectedText.Language, inputLanguage))
            {
                usePreferedInsted = true;
            }

            var recommendedTranslation = localizedTexts.FirstOrDefault(loc => Equals(loc.Language,
                usePreferedInsted ? preferedLanguage : inputLanguage))?.Text;

            if (recommendedTranslation == null ||
                recommendedTranslation.StartsWith(preferedLanguage.Name + "--", StringComparison.Ordinal))
            {
                recommendedTranslation =
                    localizedTexts.FirstOrDefault(loc => Equals(loc.Language, inputLanguage))?.Text;
            }

            return selectedText.Language.Name + "--" + recommendedTranslation;
        }

        /// <summary>
        /// turns multiple Dictionarys, each assigning translation to element / key into one
        /// Dictionary that assigns all possible translations to an element / key
        /// </summary>
        /// <param name="dictionary">
        /// flipped dictionary will be based on this dictionary, dictionary not required to
        /// have same number of translation for all languages
        /// </param>
        public static Dictionary<string, List<TextLocalization>> FlipLocalizationsDictionary(
            Dictionary<CultureInfo, Dictionary<string, string>> dictionary)
        {
            var returnDict = new Dictionary<string, List<TextLocalization>>();
            foreach (var langDict in dictionary)
            {
                foreach (var elementTranslation in langDict.Value)
                {
                    TryGetOrCreate(ref returnDict, elementTranslation.Key, out var texts);
                    texts.Add(new TextLocalization {Language = langDict.Key, Text = elementTranslation.Value});
                }
            }

            return returnDict;
        }

        private static void TryGetOrCreate(ref Dictionary<string, List<TextLocalization>> dictionary,
            string key, out List<TextLocalization> value)
        {
            dictionary.TryGetValue(key, out value);
            if (value == null)
            {
                value = new List<TextLocalization>();
                dictionary.Add(key, value);
            }
        }
    }
}