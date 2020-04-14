using System.Globalization;
using System.Windows;
using Internationalization;
using Internationalization.FileProvider.Excel;
using Internationalization.FileProvider.Interface;
using Internationalization.FileProvider.JSON;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.LiteralProvider.File;
using Microsoft.Extensions.Logging;

namespace Example_Excel
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
            var consoleLoggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Trace)
                    .AddConsole();
            });

            GlobalSettings.LibraryLoggerFactory = consoleLoggerFactory;

            IFileProvider efp = new ExcelFileProvider("Resource/Language_File", "gloss");
            //IFileProvider jfp = new JsonFileProvider("Resource/lang_file");

            FileLiteralProvider.Initialize(efp, new CultureInfo("en"));
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            AbstractLiteralProvider.Instance.Save();
        }
    }
}