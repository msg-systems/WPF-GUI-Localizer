using System.Reflection;
using System.Resources;
using Internationalization.Exception;
using Microsoft.Extensions.Logging;

namespace Internationalization.Utilities
{
    public static class ResourcesManagerProvider
    {
        private static readonly ILogger Logger;

        static ResourcesManagerProvider()
        {
            Logger = GlobalSettings.LibraryLoggerFactory.CreateLogger(typeof(ResourcesManagerProvider));
        }

        /// <summary>
        /// Returns ResourcesManager of <see cref="GlobalSettings.ResourcesAssembly"/> or the current entry assembly,
        /// if not set.
        /// </summary>
        /// <exception cref="ResourcesNotFoundException">
        /// Thrown, if both <see cref="GlobalSettings.ResourcesAssembly"/> is not set and the entry assembly
        /// cannot be accesed.
        /// </exception>
        public static ResourceManager GetResourcesManager()
        {
            if (GlobalSettings.ResourcesAssembly == null)
            {
                GlobalSettings.ResourcesAssembly = Assembly.GetEntryAssembly();
                ExceptionLoggingUtils.ThrowIf<ResourcesNotFoundException>(
                    GlobalSettings.ResourcesAssembly == null, Logger,
                    "GlobalSettings.ResourcesAssembly was not set and entry assembly cannot be accessed.");
            }

            var nameOfAppToBeTranslated = GlobalSettings.ResourcesAssembly.FullName.Split(',')[0];
            return new ResourceManager(nameOfAppToBeTranslated + ".Properties.Resources",
                GlobalSettings.ResourcesAssembly);
        }
    }
}