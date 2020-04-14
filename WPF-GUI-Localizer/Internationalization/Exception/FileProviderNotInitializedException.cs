using System;

namespace Internationalization.Exception
{
    [Serializable]
    public class FileProviderNotInitializedException : System.Exception
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