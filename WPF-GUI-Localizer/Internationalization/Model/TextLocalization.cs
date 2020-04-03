using System.Collections.Generic;
using System.Globalization;

namespace Internationalization.Model {
    public class TextLocalization {
        public CultureInfo Language { get; set; }
        public string Text { get; set; }
        public IEnumerable<string> KnownTranslations { get; set; }
    }
}
