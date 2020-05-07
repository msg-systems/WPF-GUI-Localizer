using System.Collections.Generic;
using System.Globalization;
using Internationalization.Enum;
using Internationalization.Model;

namespace Internationalization.FileProvider.Interface
{
    /// <summary>
    /// stores a dictionary in some form
    /// </summary>
    public interface IFileProvider
    {
        /// <summary>
        /// Represents progress on reading the data or cancellation of the reading action.
        /// </summary>
        ProviderStatus Status { get; }

        /// <summary>
        /// The objects initialization can be stoped by calling this method.
        /// </summary>
        void CancelInitialization();

        /// <summary>
        /// Returns the internal dictionary of translations.
        /// </summary>
        Dictionary<CultureInfo, Dictionary<string, string>> GetDictionary();

        /// <summary>
        /// Updates a key-value-pair in the dictionary of translations.
        /// </summary>
        void Update(string key, IEnumerable<TextLocalization> texts);

        /// <summary>
        /// Makes current dictionary persistent in some way.
        /// </summary>
        void SaveDictionary();
    }
}