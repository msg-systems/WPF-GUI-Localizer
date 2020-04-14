using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Internationalization;
using Internationalization.FileProvider.Excel;
using Internationalization.FileProvider.Interface;
using Internationalization.FileProvider.JSON;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.LiteralProvider.Resource;
using Microsoft.Extensions.Logging;

namespace Example_Resources
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            Startup += OnStartup;
            Exit += OnExit;
        }

        private void OnStartup(object sender, StartupEventArgs e)
        {
            Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");

            var consoleLoggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole();
            });

            GlobalSettings.LibraryLoggerFactory = consoleLoggerFactory;

            IFileProvider jfp = new JsonFileProvider("Resource/Resource_Corrections.json");
            //IFileProvider efp = new ExcelFileProvider("Resource/Corrections_as_excel");

            ResourceLiteralProvider.Initialize(jfp, new CultureInfo("en"));
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            AbstractLiteralProvider.Instance.Save();
        }
    }
}