using System;
using System.Globalization;
using System.Linq;
using Internationalization.Exception;

namespace Internationalization.Utilities
{
    public static class CultureInfoUtil
    {
        /// <param name="cultureName">string representation of CultureInfo as language tag</param>
        /// <param name="onlyBracketsAtEndOfString">will default back to false if no matching brackets are found</param>
        /// <exception cref="InvalidCultureNameException">exception is thrown if language tag cannot be found within list of languages supported by .NET</exception>
        public static CultureInfo GetCultureInfo(string cultureName, bool onlyBracketsAtEndOfString)
        {
            int begin = cultureName.LastIndexOf(@"(", StringComparison.Ordinal) + 1;
            int length = cultureName.LastIndexOf(@")", StringComparison.Ordinal) - begin;
            if (onlyBracketsAtEndOfString && begin > 0 && length > 0)
            {
                cultureName = cultureName.Substring(begin, length);
            }

            var allCultures = CultureInfo.GetCultures(CultureTypes.AllCultures);
            if (!allCultures.Select(c => c.Name).Contains(cultureName) || string.IsNullOrEmpty(cultureName))
            {
                throw new InvalidCultureNameException();
            }

            return new CultureInfo(cultureName);
        }
    }
}
