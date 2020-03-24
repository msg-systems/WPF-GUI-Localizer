using System;

namespace Internationalization.Exception
{
    [Serializable]
    public class InvalidCultureNameException : System.Exception
    {
        public InvalidCultureNameException()
        { }

        public InvalidCultureNameException(string message)
            : base(message)
        { }

        public InvalidCultureNameException(string message, System.Exception innerException)
            : base(message, innerException)
        { }
    }
}
