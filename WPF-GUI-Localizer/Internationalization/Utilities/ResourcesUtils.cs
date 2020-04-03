using System.Reflection;
using System.Resources;

namespace Internationalization.Utilities
{
    public static class ResourcesUtils
    {
        public static Assembly ResourcesAssembly { get; set; }

        /// <summary>
        /// Returns ResourcesManager of <see cref="ResourcesAssembly"/> or entry assembly if not defined or null if neither can be used
        /// </summary>
        public static ResourceManager GetResourcesManager()
        {
            if (ResourcesAssembly == null)
            {
                ResourcesAssembly = Assembly.GetEntryAssembly();
                if (ResourcesAssembly == null) return null;
            }

            string nameOfAppToBeTranslated = ResourcesAssembly.FullName.Split(',')[0];
            return new ResourceManager(nameOfAppToBeTranslated + ".Properties.Resources", ResourcesAssembly);
        }
    }
}
