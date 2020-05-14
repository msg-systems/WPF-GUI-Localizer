using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
