using System.Collections.Generic;

namespace Internationalization.Exception
{
    public class MultiExceptionLoggingObject
    {
        private readonly Dictionary<string, object> _parameterList;

        public MultiExceptionLoggingObject(object firstParameter, string nameOfFirstParameter)
        {
            _parameterList = new Dictionary<string, object> {{nameOfFirstParameter, firstParameter}};
        }

        public MultiExceptionLoggingObject AlsoVerify(object parameter, string nameOfParameter)
        {
            _parameterList.Add(nameOfParameter, parameter);
            return this;
        }

        public IEnumerable<KeyValuePair<string, object>> GetParameters()
        {
            return _parameterList;
        }
    }
}