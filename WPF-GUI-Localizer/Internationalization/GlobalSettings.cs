using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Internationalization
{
    /// <summary>
    /// Conteins Settings used by multiple Classes of the library. Should be set before any objects
    /// are created / used.
    /// </summary>
    public static class GlobalSettings
    {
        private static ILoggerFactory _libraryLoggerFactory;

        /// <summary>
        /// This LoggerFactory will be used for all logging inside the Library. If objects of this
        /// library are created / used before this Property is set, they will use the Console logger
        /// with a minimum LogLevel of Information by default and not update their logger after
        /// this property is set. This property cannot be set to null.
        /// </summary>
        public static ILoggerFactory LibraryLoggerFactory
        {
            get => _libraryLoggerFactory;

            set
            {
                if (value != null) _libraryLoggerFactory = value;
            }
        }

        /// <summary>
        /// The assembly of the project, whose Ressources files are supposed to be used; Default: null
        /// </summary>
        public static Assembly ResourcesAssembly { get; set; }

        /// <summary>
        /// Determines if LocalizerEventHandler updates the changed translation of an element in the GUI
        /// directly using GuiTranslator or not; Default: true.
        /// </summary>
        public static bool UseGuiTranslatorForLocalizationUtils { get; set; }

        /// <summary>
        /// Initializes all Settings with their default value.
        /// </summary>
        static GlobalSettings()
        {
            _libraryLoggerFactory = LoggerFactory.Create(builder =>
            {
                builder
                    .SetMinimumLevel(LogLevel.Information)
                    .AddConsole();
            });

            UseGuiTranslatorForLocalizationUtils = true;
        }
    }
}