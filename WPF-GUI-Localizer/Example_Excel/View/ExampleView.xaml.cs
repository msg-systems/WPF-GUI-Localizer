using System;
using System.Collections.Generic;
using System.Windows.Controls;
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
