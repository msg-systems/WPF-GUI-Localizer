using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Internationalization.Exception;
using Microsoft.Extensions.Logging;

namespace Internationalization.Utilities
{
    /// <summary>
    ///     Handles logging and throwing of common Exceptions.
    /// </summary>
    public static class ExceptionLoggingUtils
    {
        public static void ThrowIfNull(ILogger logger, string nameOfMethod, object reference, string nameOfReference,
            string message)
        {
            ThrowIfNull(logger, reference, nameOfReference, message,
                $"{nameOfMethod} received null parameter.");
        }

        public static void ThrowIfNull(ILogger logger, object reference, string nameOfReference,
            string exceptionMessage, string logMessage)
        {
            ThrowIf(reference == null, logger, new ArgumentNullException(nameOfReference, exceptionMessage),
                logMessage);
        }

        public static MultiExceptionLoggingObject VerifyMultiple(object parameter, string nameOfParameter)
        {
            return new MultiExceptionLoggingObject(parameter, nameOfParameter);
        }

        public static void ThrowIfInputLanguageMissing(ILogger logger, IEnumerable<CultureInfo> listOfLanguages,
            CultureInfo inputLangue, string exceptionMessage, string logMessage)
        {
            ThrowIf(!listOfLanguages.Contains(inputLangue), logger,
                new InputLanguageNotFoundException(exceptionMessage), logMessage);
        }

        public static void ThrowIf<TException>(bool condition, ILogger logger, string message)
            where TException : System.Exception
        {
            ThrowIf(condition, logger, CreateException<TException>(message), message);
        }

        public static void ThrowIf<TException>(bool condition, ILogger logger, TException exception, string logMessage)
            where TException : System.Exception
        {
            if (condition)
            {
                Throw(logger, exception, logMessage);
            }
        }

        public static void Throw<TException>(ILogger logger, string message)
            where TException : System.Exception
        {
            Throw(logger, CreateException<TException>(message), message);
        }

        public static void Throw<TException>(ILogger logger, TException exception, string logMessage)
            where TException : System.Exception
        {
            logger.Log(LogLevel.Error, exception, logMessage);
            throw exception;
        }

        private static TException CreateException<TException>(string message) where TException : System.Exception
        {
            return Activator.CreateInstance(typeof(TException), message) as TException;
        }
    }
}