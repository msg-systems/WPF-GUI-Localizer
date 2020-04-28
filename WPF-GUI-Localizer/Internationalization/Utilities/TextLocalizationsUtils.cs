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

        /// <summary>
        /// Returns a placeholder text (e.g. "fr--Total amount") and evaluates, what translation
        /// should be used as basis for the text (part after "fr--").
        /// </summary>
        /// <param name="targetLanguage">The language for which the placeholder needs to be generated.</param>
        /// <param name="localizedTexts">
        /// The collection of all known translations.
        /// If <paramref name="preferedLanguage"/> is not contained in this collection,
        /// <paramref name="preferPreferedOverInputLangauge"/> will be ignored and <paramref name="inputLanguage"/>
        /// will always be used.
        /// </param>
        /// <param name="preferPreferedOverInputLangauge">
        /// Determines which out of <paramref name="inputLanguage"/> and <paramref name="preferedLanguage"/>
        /// should be used by default.
        /// This value will be overridden, if <paramref name="targetLanguage"/> is identical to either
        /// <paramref name="inputLanguage"/> or <paramref name="preferedLanguage"/> or
        /// <paramref name="preferedLanguage"/> is not contained in <paramref name="localizedTexts"/>.
        /// </param>
        /// <param name="inputLanguage">The language in which the application was originally created in.</param>
        /// <param name="preferedLanguage">
        /// The language to fall back to, if <paramref name="inputLanguage"/> is the <paramref name="targetLanguage"/>
        /// or to aid as basis for further translation (e.g application was originally french, is then translated
        /// to english and from english to multiple others).
        /// </param>
        /// <returns>
        /// The full placeholder string consisting of language code, "--" and the translation of
        /// <paramref name="inputLanguage"/> or <paramref name="preferedLanguage"/>.
        /// </returns>
        /// <exception cref="InputLanguageNotFoundException">
        /// Thrown, if <paramref name="localizedTexts"/> does not contain <paramref name="inputLanguage"/>.
        /// </exception>
        public static string GetRecommendedText(CultureInfo targetLanguage,
            ICollection<TextLocalization> localizedTexts, bool preferPreferedOverInputLangauge,
            CultureInfo inputLanguage, CultureInfo preferedLanguage)
        {
            if (localizedTexts.FirstOrDefault(loc => Equals(loc.Language, inputLanguage)) == null)
            {
                //the list of TextLocalizations has to contain at least the InputLanguage
                throw new InputLanguageNotFoundException(
                    "Unable to generate translation recommendations for Localizaions without InputLanguage.");
            }

            var usePreferedInsted = EvaluateLanguagePreference(preferPreferedOverInputLangauge, localizedTexts,
                targetLanguage, preferedLanguage, inputLanguage);

            //Will find a fitting entry, because EvaluateLanguagePreference returns false
            //to usePreferedInsted if list does not contain the preferedLanguage and this function
            //throws InputLanguageNotFoundException is inputLanguage is not in the list.
            var recommendedTranslation = localizedTexts.First(loc => Equals(loc.Language,
                usePreferedInsted ? preferedLanguage : inputLanguage)).Text;

            return targetLanguage.Name + "--" + recommendedTranslation;
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