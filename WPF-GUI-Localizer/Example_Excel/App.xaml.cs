using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Internationalization.FileProvider.Excel;
using Internationalization.FileProvider.Interface;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.LiteralProvider.File;

namespace Example_Excel
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            this.Startup += OnStartup;
            this.Exit += OnExit;
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            IFileProvider efp = new ExcelFileProvider(@"Resource/Language_File.xlsx", "gloss");
            FileLiteralProvider.Initialize(efp, new CultureInfo("en"));
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            AbstractLiteralProvider.Instance.Save();
        }
    }
}
