using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Internationalization.Exception;
using Internationalization.FileProvider.Interface;
using Internationalization.Model;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Internationalization.FileProvider.JSON
{
    /// <summary>
    /// Saves its files using Json.NET.
    /// The managed dictionary gets directly saved in its deserialized form
    /// </summary>
    public class JsonFileProvider : IFileProvider
    {
        private static ILogger _logger;

        private readonly string _path;
        private Dictionary<CultureInfo, Dictionary<string, string>> _dictOfdicts;

        private bool _successfullyCreatedFile;
        public ProviderStatus Status { get; private set; }

        /// <param name="translationFilePath">the path under which the dictionary will be saved</param>
        public JsonFileProvider(string translationFilePath)
        {
            Status = ProviderStatus.InitializationInProgress;

            _logger = GlobalSettings.LibraryLoggerFactory.CreateLogger<JsonFileProvider>();
            _logger.Log(LogLevel.Trace, "Initializing JsonFileProvider.");

            if (PathLooksGood(ref translationFilePath))
            {
                _path = translationFilePath;

                Initialize();
            }
            else
            {
                _logger.Log(LogLevel.Information, "Reading of excel files aborted.");
            }
        }

        public void SaveDictionary()
        {
            _logger.Log(LogLevel.Trace, "SaveDictionary was called.");
            try
            {
                File.WriteAllText(_path, JsonConvert.SerializeObject(_dictOfdicts));
            }
            catch (System.Exception e)
            {
                _logger.Log(LogLevel.Debug,
                    $@"Unable to write langage file ({Path.GetFullPath(_path)}). {e.GetType()} ({e.Message}).");
            }
        }

        public void Update(string key, IEnumerable<TextLocalization> texts)
        {
            IList<TextLocalization> textsEnumerated = texts.ToList();

            var textsString = string.Join(", ", textsEnumerated.Select(l => l.ToString()));
            _logger.Log(LogLevel.Trace, $"Update was called with {{{textsString}}} as translations for key ({key}).");

            foreach (var textLocalization in textsEnumerated)
            {
                _dictOfdicts.TryGetValue(textLocalization.Language, out var langDict);
                if (langDict == null)
                {
                    langDict = new Dictionary<string, string>();
                    _dictOfdicts.Add(textLocalization.Language, langDict);
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
            }

            //if file was created by JsonFileProvider itself
            if (Status == ProviderStatus.InitializationInProgress && _successfullyCreatedFile)
            {
                _logger.Log(LogLevel.Debug, "First update after empty sheet was created.");
                SaveDictionary();

                Status = ProviderStatus.Initialized;
            }
        }

        public void CancelInitialization()
        {
            Status = ProviderStatus.CancellationInProgress;
            //Cancelation identical to Initialization
        }

        /// <exception cref="FileProviderNotInitializedException">
        /// Will be thrown if the object has not found a language file to pull translations from.
        /// </exception>
        public Dictionary<CultureInfo, Dictionary<string, string>> GetDictionary()
        {
            if (Status != ProviderStatus.Initialized)
            {
                //logged as warning not error, since this behaviour could be normal / intended (ResourceLiteralProvider).
                _logger.Log(LogLevel.Warning, "Dictionary was accessed without JsonFileProvider being initialized.");
                throw new FileProviderNotInitializedException();
            }

            return _dictOfdicts;
        }

        private bool PathLooksGood(ref string path)
        {
            if (path == null)
            {
                _logger.Log(LogLevel.Warning, "Cannot access language file, bacause path is null.");
                return false;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch
            {
                //Could get triggered, if path is not written correctly. Also if permissions for location are missing.
                _logger.Log(LogLevel.Warning, $"There appear to be some problems with the given path ({path}).\n"
                                              + "Failed to get fully qualified location for given path.");
                return false;
            }

            if (!path.EndsWith(".json"))
            {
                _logger.Log(LogLevel.Debug, $"Added '.json' to path ({path}).");
                path += ".json";
            }

            if (File.Exists(fullPath))
            {
                return true;
            }

            _logger.Log(LogLevel.Information, $"Directory for Json file will be created ({path}).");

            var directory = Path.GetDirectoryName(path);

            if (!string.IsNullOrEmpty(directory))
            {
                try
                {
                    Directory.CreateDirectory(directory);
                }
                catch
                {
                    _logger.Log(LogLevel.Warning, $"Failed to create directory ({directory}) for path ({fullPath}).");
                    return false;
                }
            }

            return true;
        }

        private void Initialize()
        {
            _logger.Log(LogLevel.Trace, "Entering Initialize function.");
            var fileContent = string.Empty;

            try
            {
                fileContent = File.ReadAllText(_path);
                if ("null".Equals(fileContent))
                {
                    _logger.Log(LogLevel.Trace, "File had content 'null', will be treated as empty dictionary.");
                    fileContent = "{}";
                }
            }
            catch
            {
                _logger.Log(LogLevel.Information, $"Unable to open langauge file ({Path.GetFullPath(_path)}).");

                try
                {
                    fileContent = "{}";
                    //identical to JsonConvert.SerializeObject(new Dictionary<CultureInfo, Dictionary<string, string>>())
                    File.WriteAllText(_path, fileContent);

                    _successfullyCreatedFile = true;
                    if (Status == ProviderStatus.CancellationInProgress)
                    {
                        Status = ProviderStatus.CancellationComplete;
                        _logger.Log(LogLevel.Trace, "Cancellation finished after creating new file.");
                    }
                    else
                    {
                        _logger.Log(LogLevel.Trace, "Initialization finished after creating new file.");
                    }

                    return;
                }
                catch
                {
                    _logger.Log(LogLevel.Warning, $"Unable to open or create new language file ({_path})."
                                                  + "JsonFileProvider will not be Initialized.");
                    return;
                }
            }

            _dictOfdicts =
                JsonConvert.DeserializeObject<Dictionary<CultureInfo, Dictionary<string, string>>>(fileContent);

            //Cancelation identical to Initialization
            switch (Status)
            {
                case ProviderStatus.CancellationInProgress:
                    Status = ProviderStatus.CancellationComplete;
                    break;
                case ProviderStatus.InitializationInProgress:
                    Status = ProviderStatus.Initialized;
                    break;
            }
        }
    }
}