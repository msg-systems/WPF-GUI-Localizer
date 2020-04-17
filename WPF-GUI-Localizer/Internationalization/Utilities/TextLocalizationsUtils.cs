using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Internationalization.Exception;
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
            if (localizedTexts.FirstOrDefault(loc => Equals(loc.Language, inputLanguage)) == null)
            {
                //the list of TextLocalizations has to contain at least the InputLanguage
                throw new InputLanguageNotFoundException(
                    "Unable to generate translation recommendations for Localizaions without InputLanguage.");
            }

            var usePreferedInsted = EvaluateLanguagePreference(preferPreferedOverInputLangauge, localizedTexts,
                selectedText.Language, preferedLanguage, inputLanguage);

            //Sould not throw InvalidOperationException, because EvaluateLanguagePreference returns false
            //to usePreferedInsted if list does not contain the preferedLanguage and this function
            //throws InputLanguageNotFoundException is inputLanguage is not in the list.
            var recommendedTranslation = localizedTexts.First(loc => Equals(loc.Language,
                usePreferedInsted ? preferedLanguage : inputLanguage)).Text;

            return selectedText.Language.Name + "--" + recommendedTranslation;
        }

        /// <summary>
        /// It converts the association CultureInfo - translation into a TextLocalization object,
        /// reducing multiple dictionaries with CultureInfo objects as keys to one dictionary
        /// that uses elements / ressources keys as dictionary keys.
        /// </summary>
        /// <param name="dictionary">
        /// Flipped dictionary will be based on this dictionary. The dictionary is not required to
        /// have same number of translation for all languages.
        /// </param>
        public static Dictionary<string, List<TextLocalization>> FlipLocalizationsDictionary(
            Dictionary<CultureInfo, Dictionary<string, string>> dictionary)
        {
            var returnDict = new Dictionary<string, List<TextLocalization>>();

            foreach (var langDict in dictionary)
            {
                foreach (var elementTranslation in langDict.Value)
                {
                    var texts = GetOrCreate(ref returnDict, elementTranslation.Key);
                    texts.Add(new TextLocalization {Language = langDict.Key, Text = elementTranslation.Value});
                }
            }

            return returnDict;
        }

        private static bool EvaluateLanguagePreference(bool usePreferedInsted,
            ICollection<TextLocalization> localizedTexts, CultureInfo languageOfText, CultureInfo preferedLanguage,
            CultureInfo inputLanguage)
        {
            var testingprefered = localizedTexts.FirstOrDefault(loc => Equals(loc.Language, preferedLanguage))?.Text;

            //if prefered does not exist or is itself a recommendation fallback to inputlang.
            if (testingprefered == null ||
                testingprefered.StartsWith(preferedLanguage.Name + "--", StringComparison.Ordinal))
            {
                return false;
            }

            //the following code can *probably* not be simplified.
            if (usePreferedInsted && Equals(languageOfText, preferedLanguage))
            {
                return false;
            }

            if (!usePreferedInsted && Equals(languageOfText, inputLanguage))
            {
                return true;
            }

            return usePreferedInsted;
        }

        private static List<TextLocalization> GetOrCreate(ref Dictionary<string,
                List<TextLocalization>> dictionary, string key)
        {
            dictionary.TryGetValue(key, out var value);

            if (value == null)
            {
                value = new List<TextLocalization>();
                dictionary.Add(key, value);
            }

            return value;
        }
    }
}