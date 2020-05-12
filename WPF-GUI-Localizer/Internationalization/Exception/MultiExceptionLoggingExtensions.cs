using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Internationalization.Exception
{
    public static class MultiExceptionLoggingExtensions
    {
        public static MultiExceptionLoggingObject ThrowIfNull(this MultiExceptionLoggingObject loggingObject, ILogger logger,
            string nameOfMethod, string message)
        {
            ArgumentNullException e = null;

            foreach (var parameter in loggingObject.GetParameters())
            {
                if (parameter.Value == null)
                {
                    if (e == null)
                    {
                        e = new ArgumentNullException(parameter.Key, message);
                        logger.Log(LogLevel.Error, e, $"{nameOfMethod} received null parameter ({parameter.Key}).");
                    }
                    else
                    {
                        logger.Log(LogLevel.Error, $"{nameOfMethod} received null parameter ({parameter.Key}).");
                    }
                }
            }

            if (e != null)
            {
                throw e;
            }

            return loggingObject;
        }
    }
}
