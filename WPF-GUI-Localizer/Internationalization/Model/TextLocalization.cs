using System.Collections.ObjectModel;
using System.Globalization;

namespace Internationalization.Model {
    public class TextLocalization {
        public CultureInfo Language { get; set; }
        public string Text { get; set; }
        public ObservableCollection<string> KnownTranslations { get; set; }
    }
}
