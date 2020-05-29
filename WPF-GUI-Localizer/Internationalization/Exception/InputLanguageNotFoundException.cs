using System;

namespace Internationalization.Exception
{
    [Serializable]
    public class InputLanguageNotFoundException : System.Exception
    {
        public InputLanguageNotFoundException()
        {
        }

        public InputLanguageNotFoundException(string message)
            : base(message)
        {
        }

        public InputLanguageNotFoundException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }
    }
}