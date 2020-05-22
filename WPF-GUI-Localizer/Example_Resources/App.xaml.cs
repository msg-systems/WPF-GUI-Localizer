using Internationalization;
using Internationalization.FileProvider.Interface;
using Internationalization.FileProvider.JSON;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.LiteralProvider.Resource;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Threading;
using System.Windows;

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
            //IFileProvider efp = new ExcelFileProvider("Resource/Corrections_as_excel.xlsx");

            ResourceLiteralProvider.Initialize(jfp, new CultureInfo("en"));
        }

        private void OnExit(object sender, ExitEventArgs e)
        {
            AbstractLiteralProvider.Instance.Save();
        }
    }
}