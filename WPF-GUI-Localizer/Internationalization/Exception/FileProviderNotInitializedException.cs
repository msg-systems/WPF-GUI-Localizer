using System;

namespace Internationalization.Exception
{
    [Serializable]
    public class FileProviderNotInitializedException : ProviderNotInitializedException
    {
        public FileProviderNotInitializedException()
        {
        }

        public FileProviderNotInitializedException(string message)
            : base(message)
        {
        }

        public FileProviderNotInitializedException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }
    }
}