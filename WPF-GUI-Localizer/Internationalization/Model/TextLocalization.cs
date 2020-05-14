using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Internationalization.Model
{
    public class TextLocalization
    {
        public CultureInfo Language { get; set; }
        public string Text { get; set; }
        public IEnumerable<string> KnownTranslations { get; set; }

        public override string ToString()
        {
            string languageName = "null";
            string knownTranslationsCount = "no";

            if (Language != null)
            {
                languageName = Language.Name;
            }

            if (KnownTranslations != null)
            {
                knownTranslationsCount = "" + KnownTranslations.Count();
            }

            return $"({languageName} : {Text} ({knownTranslationsCount} suggested translations))";
        }
    }
}