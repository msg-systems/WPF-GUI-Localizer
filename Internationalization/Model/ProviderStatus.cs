using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Internationalization.Model
{
    public enum ProviderStatus
    {
        InitializationInProgress,
        Initialized,
        CancellationInProgress,
        CancellationComplete
    }
}
