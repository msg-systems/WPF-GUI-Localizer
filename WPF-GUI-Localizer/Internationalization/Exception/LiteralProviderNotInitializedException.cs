using System;

namespace Internationalization.Exception
{
    [Serializable]
    public class LiteralProviderNotInitializedException : ProviderNotInitializedException
    {
        public LiteralProviderNotInitializedException()
        {
        }

        public LiteralProviderNotInitializedException(string message)
            : base(message)
        {
        }

        public LiteralProviderNotInitializedException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }
    }
}