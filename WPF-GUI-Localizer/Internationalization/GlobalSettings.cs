using Microsoft.Extensions.Logging;

namespace Internationalization
{
    /// <summary>
    /// Conteins Settings used by multiple Classes of the library. Should be set before any objects are created / used.
    /// </summary>
    public static class GlobalSettings
    {
        /// <summary>
        /// This LoggerFactory will be used for all logging inside the Library. If objects of this library are created / used before this Property is set,
        /// they will use the Console logger without a LogLevel filter by default and not update their logger after this property is set.
        /// </summary>
        public static ILoggerFactory LibraryLoggerFactory { internal get; set; } = LoggerFactory.Create(builder => { builder.AddConsole(); });
    }
}
