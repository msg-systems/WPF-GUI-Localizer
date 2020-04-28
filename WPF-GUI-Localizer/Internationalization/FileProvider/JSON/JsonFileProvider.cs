using Internationalization.Exception;
using Internationalization.FileProvider.Interface;
using Internationalization.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Internationalization.Enum;
using Internationalization.FileProvider.FileHandler.Universal;

namespace Internationalization.FileProvider.JSON
{
    /// <summary>
    /// Saves its files using Json.NET.
    /// The managed dictionary gets directly saved in its deserialized form.
    /// </summary>
    public class JsonFileProvider : IFileProvider
    {
        private static ILogger _logger;

        private readonly string _path;
        private readonly UniversalFileHandler _fileHandler;

        private bool _successfullyCreatedFile;
        private Dictionary<CultureInfo, Dictionary<string, string>> _dictOfDicts =
            new Dictionary<CultureInfo, Dictionary<string, string>>();

        public ProviderStatus Status { get; private set; }

        /// <summary>
        /// Saves its files using the Json.NET library.
        /// </summary>
        /// <param name="translationFilePath">The path under which the dictionary will be saved.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown, if <paramref name="translationFilePath"/> is null.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown, if the permissions are missing that are needed to create the directory for
        /// <paramref name="translationFilePath"/> or <paramref name="translationFilePath"/> is write-only,
        /// read-only, a directory, hidden, the needed permissions for opening or writing are missing or
        /// the operation is not supported on the current platform.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// Thrown, if certain permissions are missing. (CLR level)
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown, if <paramref name="translationFilePath"/> does not exist or cannot be found.
        /// For example because it is a direcory.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown, if an unknown I/O-Error occurres.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown, if <paramref name="translationFilePath"/> contains a colon anywhere other than as part of a
        /// volume identifier ("C:\").
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// Thrown, if <paramref name="translationFilePath"/> is too long.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown, if the directory was not found.
        /// For example because it is on an unmapped device.
        /// </exception>
        public JsonFileProvider(string translationFilePath)
        {
            //set Status.
            Status = ProviderStatus.InitializationInProgress;

            //easy initializations.
            _logger = GlobalSettings.LibraryLoggerFactory.CreateLogger<JsonFileProvider>();
            _logger.Log(LogLevel.Trace, "Initializing JsonFileProvider.");
            _fileHandler = new UniversalFileHandler(typeof(JsonFileProvider), ".json");

            //null check.
            if (translationFilePath == null)
            {
                var e = new ArgumentNullException(nameof(translationFilePath), "Unable to open null path.");
                _logger.Log(LogLevel.Error, e, "JsonFileProvider recived null parameter in constructor.");
                throw e;
            }

            //start proper initialization.
            _path = _fileHandler.GetPathAndHandleProblems(translationFilePath);
            Initialize();
        }

        /// <summary>
        /// Makes the current dictionary persistent, by writing its serialized form to the
        /// specified file.
        /// </summary>
        /// <exception cref="FileProviderNotInitializedException">
        /// Thrown, if the object has not found a language file to pull translations from.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown, if <see cref="_path"/> is read-only, a directory, hidden, the needed permissions
        /// are missing or the operation is not supported on the current platform.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// Thrown, if certain permissions are missing. (CLR level)
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown, if an unknown I/O-Error occurres.
        /// </exception>
        public void SaveDictionary()
        {
            _logger.Log(LogLevel.Trace, "SaveDictionary was called.");

            if (Status != ProviderStatus.Initialized)
            {
                var e = new FileProviderNotInitializedException(
                    "SaveDictionary was called, without JsonFileProvider being initialized.");
                _logger.Log(LogLevel.Error, e,
                    "SaveDictionary was called, without JsonFileProvider being initialized.");
                throw e;
            }

            _fileHandler.WriteAllTextWrapper(JsonConvert.SerializeObject(_dictOfDicts), _path);

            _logger.Log(LogLevel.Trace, "Dictionary was saved without errors.");
        }

        /// <summary>
        /// Updates internal dictionary at <paramref name="key"/> with the given dictionary.
        /// Only languages contained in <paramref name="texts"/> will be updated.
        /// Will automatically write to file, if this is the first Update call
        /// and no file existed upon creation of this object.
        /// </summary>
        /// <exception cref="ArgumentNullException">Thrown, if <paramref name="key"/> is null.</exception>
        /// <param name="key">The entry for which translations should be updated.</param>
        /// <param name="texts">
        /// The new translations. If list is null or empty, no changes will be made to the dictionary.
        /// </param>
        public void Update(string key, IEnumerable<TextLocalization> texts)
        {
            //null checks.
            if (key == null)
            {
                var e = new ArgumentNullException(nameof(key), "Key recived in Update call is null.");
                _logger.Log(LogLevel.Warning, e, "Unable to update dictionary for null key.");
                throw e;
            }
            if (texts == null)
            {
                //no exception has to be thrown here, because null is treated like empty list and
                //no translations will be updated.
                _logger.Log(LogLevel.Debug, "Unable to update dictionary for null translations. "
                                            + "No translations were updated.");
                return;
            }

            //logging.
            IList<TextLocalization> textsEnumerated = texts.ToList();
            var textsString = string.Join(", ", textsEnumerated.Select(l => l.ToString()));
            _logger.Log(LogLevel.Trace, $"Update was called with {{{textsString}}} as translations for key ({key}).");

            //dictionary updates.
            if (!UpdateDictionary(key, textsEnumerated))
            {
                _logger.Log(LogLevel.Debug, "Did not update dictionary.");
                return;
            }

            //create file based on first entry,
            //if dictionary was updated and the file was created by JsonFileProvider itself.
            if (Status == ProviderStatus.InitializationInProgress && _successfullyCreatedFile)
            {
                _logger.Log(LogLevel.Debug, "First update after empty sheet was created.");
                SaveDictionary();

                Status = ProviderStatus.Initialized;
            }
        }

        public void CancelInitialization()
        {
            if (Status == ProviderStatus.InitializationInProgress)
            {
                //cancelation identical to Initialization.
                Status = ProviderStatus.CancellationInProgress;
                _logger.Log(LogLevel.Trace,
                    "JsonFileProvider is now in the process of cancelling its initialization");
            }
        }

        /// <summary>
        /// Returns the internal dictionary.
        /// </summary>
        /// <exception cref="FileProviderNotInitializedException">
        /// Thrown, if the object has not found a language file to pull translations from.
        /// </exception>
        public Dictionary<CultureInfo, Dictionary<string, string>> GetDictionary()
        {
            if (Status != ProviderStatus.Initialized)
            {
                var e = new FileProviderNotInitializedException(
                    "Dictionary was accessed, without JsonFileProvider being initialized.");
                _logger.Log(LogLevel.Error, e,
                    "Dictionary was accessed, without JsonFileProvider being initialized.");
                throw e;
            }

            return _dictOfDicts;
        }

        /// <summary>
        /// Coordinates the initialization of <see cref="_dictOfDicts"/>.
        /// </summary>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown, if <see cref="_path"/> is write-only, read-only, a directory, hidden, the needed
        /// permissions for opening or writing are missing or the operation is not supported
        /// on the current platform.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// Thrown, if certain permissions are missing. (CLR level)
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown, if <see cref="_path"/> does not exist or cannot be found.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown, if an unknown I/O-Error occurres.
        /// </exception>
        private void Initialize()
        {
            _logger.Log(LogLevel.Trace, "Entering Initialize function.");

            string fileContent;

            if (File.Exists(_path))
            {
                _logger.Log(LogLevel.Trace, $"Langauge file is present ({Path.GetFullPath(_path)}).");

                fileContent = _fileHandler.ReadAllTextWrapper(_path);

                if ("null".Equals(fileContent))
                {
                    _logger.Log(LogLevel.Information,
                        "File had content 'null', will be treated as empty dictionary.");
                    fileContent = "{}";
                }
            }
            else
            {
                _logger.Log(LogLevel.Trace, $"No langauge file present ({Path.GetFullPath(_path)}).");

                //identical to "JsonConvert.SerializeObject(new Dictionary<CultureInfo, Dictionary<string, string>>())".
                fileContent = "{}";
                _fileHandler.WriteAllTextWrapper(fileContent, _path);

                _successfullyCreatedFile = true;
            }

            _dictOfDicts =
                JsonConvert.DeserializeObject<Dictionary<CultureInfo, Dictionary<string, string>>>(fileContent);

            //cancelation identical to Initialization.
            switch (Status) //TODO revisit after adding more states to Status
            {
                case ProviderStatus.CancellationInProgress:
                    Status = ProviderStatus.CancellationComplete;
                    break;
                case ProviderStatus.InitializationInProgress:
                    Status = ProviderStatus.Initialized;
                    break;
            }
        }

        /// <summary>
        /// Updates <see cref="_dictOfDicts"/> using the given values and returns true, if any updates were made.
        /// </summary>
        /// <param name="key">
        /// The entry for which translations should be updated.
        /// Assumed to be not null, because this function is only used once.
        /// </param>
        /// <param name="textLocalizations">
        /// The new translations. If list is null or empty, no changes will be made to the dictionary.
        /// Assumed to be not null, because this function is only used once.
        /// </param>
        /// <returns>
        /// True, if at least one languages translation was updated.
        /// False, if <paramref name="textLocalizations"/> cantained no entries.
        /// </returns>
        private bool UpdateDictionary(string key, IEnumerable<TextLocalization> textLocalizations)
        {
            var readSuccess = false;

            foreach (var textLocalization in textLocalizations)
            {
                _dictOfDicts.TryGetValue(textLocalization.Language, out var langDict);
                if (langDict == null)
                {
                    langDict = new Dictionary<string, string>();
                    _dictOfDicts.Add(textLocalization.Language, langDict);
                    _logger.Log(LogLevel.Trace,
                        $"New language dictionary was created for {textLocalization.Language.EnglishName}.");
                }

                if (langDict.ContainsKey(key))
                {
                    langDict.Remove(key);
                    _logger.Log(LogLevel.Trace, "Updated existing entry for given value.");
                }
                else
                {
                    _logger.Log(LogLevel.Trace, "Created new entry for given value.");
                }

                langDict.Add(key, textLocalization.Text);

                readSuccess = true;
            }

            return readSuccess;
        }
    }
}