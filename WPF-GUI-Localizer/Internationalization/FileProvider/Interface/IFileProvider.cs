using System.Collections.Generic;
using System.Globalization;
using Internationalization.Model;

namespace Internationalization.FileProvider.Interface
{
    /// <summary>
    /// stores a dictionary in some form
    /// </summary>
    public interface IFileProvider
    {
        /// <summary>
        /// Represents progress on reading the data or cancellation of the reading action
        /// </summary>
        ProviderStatus Status { get; }

        void CancelInitialization();

        /// <summary>
        /// Returns saved dictionary
        /// </summary>
        Dictionary<CultureInfo, Dictionary<string, string>> GetDictionary();

        /// <summary>
        /// Updates a key-value-pair in the dictionary
        /// </summary>
        void Update(string key, IEnumerable<TextLocalization> texts);

        /// <summary>
        /// Makes current dictionary persistent in some way
        /// </summary>
        void SaveDictionary();
    }
}