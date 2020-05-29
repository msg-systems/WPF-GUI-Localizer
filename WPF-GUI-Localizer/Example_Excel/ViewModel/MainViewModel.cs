using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Windows.Controls;
using Example_Excel.View;
using GalaSoft.MvvmLight;
using Internationalization.GUITranslator;
using Internationalization.LiteralProvider.Abstract;

namespace Example_Excel.ViewModel
{
    public class MainViewModel : ViewModelBase
    {
        private UserControl _currentView;

        public MainViewModel()
        {
            CurrentView = new ExampleView();
        }

        public UserControl CurrentView
        {
            get => _currentView;
            set => Set(ref _currentView, value);
        }

        public CultureInfo SelectedLanguage
        {
            get => Thread.CurrentThread.CurrentUICulture;
            set
            {
                Thread.CurrentThread.CurrentCulture = value;
                Thread.CurrentThread.CurrentUICulture = value;

                GuiTranslator.TranslateGui(CurrentView);
            }
        }

        public IEnumerable<CultureInfo> TranslatableLanguages => AbstractLiteralProvider.Instance.GetKnownLanguages();
    }
}