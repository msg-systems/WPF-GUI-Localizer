using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Internationalization.FileProvider.Excel;
using Internationalization.FileProvider.Interface;
using Internationalization.FileProvider.JSON;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.LiteralProvider.Resource;

namespace Example_Resources
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
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en");

            IFileProvider jfp = new JsonFileProvider(@"Resource/Resource_Corrections.json");
            IFileProvider efp = new ExcelFileProvider("Resource/Corrections_as_excel");
            ResourceLiteralProvider.Initialize(jfp, new CultureInfo("en"));
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            AbstractLiteralProvider.Instance.Save();
        }
    }
}
