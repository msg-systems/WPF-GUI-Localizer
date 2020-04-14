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
            return $"({Language.Name} : {Text} ({KnownTranslations.Count()} suggested translations))";
        }
    }
}