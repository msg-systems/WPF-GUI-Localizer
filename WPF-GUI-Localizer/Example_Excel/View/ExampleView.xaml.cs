using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Internationalization.GUITranslator;

namespace Example_Excel.View
{
    /// <summary>
    /// Interaction logic for ExampleView.xaml
    /// </summary>
    public partial class ExampleView : UserControl
    {
        public ExampleView()
        {
            InitializeComponent();

            this.Loaded += TranslateMe;
        }

        private void TranslateMe(object sender, EventArgs eventArgs)
        {
            GuiTranslator.TranslateGui(this);
        }
    }
}
