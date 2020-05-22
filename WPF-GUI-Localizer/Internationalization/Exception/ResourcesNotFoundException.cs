using System;
using System.IO;

namespace Internationalization.Exception
{
    [Serializable]
    public class ResourcesNotFoundException : FileNotFoundException
    {
        public ResourcesNotFoundException()
        {
        }

        public ResourcesNotFoundException(string message)
            : base(message)
        {
        }

        public ResourcesNotFoundException(string message, System.Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
