using System.Reflection;
using System.Resources;

namespace Internationalization.Utilities
{
    public static class ResourcesManagerProvider
    {
        /// <summary>
        /// Returns ResourcesManager of <see cref="GlobalSettings.ResourcesAssembly"/> or the current entry assembly if not defined or null if neither can be used
        /// </summary>
        public static ResourceManager GetResourcesManager()
        {
            if (GlobalSettings.ResourcesAssembly == null)
            {
                GlobalSettings.ResourcesAssembly = Assembly.GetEntryAssembly();
                if (GlobalSettings.ResourcesAssembly == null) return null; //TODO error werfen?
            }

            var nameOfAppToBeTranslated = GlobalSettings.ResourcesAssembly.FullName.Split(',')[0];
            return new ResourceManager(nameOfAppToBeTranslated + ".Properties.Resources",
                GlobalSettings.ResourcesAssembly);
        }
    }
}