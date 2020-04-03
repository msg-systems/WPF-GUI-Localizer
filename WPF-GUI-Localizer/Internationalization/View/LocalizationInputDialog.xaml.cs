using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Internationalization.Model;
using Internationalization.Utilities;

namespace Internationalization.View {

    /// <summary>
    /// Interaction logic for LocalizationInputDialog.xaml
    /// </summary>
    public partial class LocalizationInputDialog {
        public LocalizationInputDialog() {
            InitializeComponent();
            DataContext = this;
            ContentRendered += OnContentRenderedAction;
        }

        public TextLocalization InputLocalization { get; set; }
        public ObservableCollection<TextLocalization> LocalizedTexts { get; set; }

        private void OnContentRenderedAction(object sender, EventArgs args) {
            
            //manually fill drop down menu
            foreach (var item in LocalizedTexts) {

                //get Combobox of current translation
                UIElement uiElement =
                    (UIElement)TranslationItems.ItemContainerGenerator.ContainerFromItem(item);
                ComboBox comboBox = VisualTreeUtils.GetVisualChildCollection<ComboBox>(uiElement).First();
                comboBox.Items.Clear();

                //fill known translations
                var textLocalization = (TextLocalization)comboBox.DataContext;
                var knownTranslations = textLocalization.KnownTranslations;
                if (knownTranslations != null) {
                    foreach (string knownTranslation in knownTranslations) {
                        comboBox.Items.Add(new ComboBoxItem {
                            Background = Brushes.DeepSkyBlue,
                            Content = knownTranslation
                        });
                    }
                }

                //if no known translations found, add No suggestions messge
                if (comboBox.Items.Count <= 0) {
                    ComboBoxItem dummyComboBoxItem = new ComboBoxItem {
                        FontStyle = FontStyles.Italic,
                        Content = "No suggestions",
                        IsHitTestVisible = false
                    };
                    comboBox.Items.Add(dummyComboBoxItem);
                }
            }
        }
        
        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void LocalizationInputDialog_OnKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                DialogResult = true;
            }
        }
    }
}
