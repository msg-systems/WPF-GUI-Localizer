using System;

namespace Internationalization.Exception
{
    [Serializable]
    public class ProviderNotInitializedException : System.Exception
    {
        public ProviderNotInitializedException()
        {
        }

        public ProviderNotInitializedException(string message)
            : base(message)
        {
        }

        public ProviderNotInitializedException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }
    }
}