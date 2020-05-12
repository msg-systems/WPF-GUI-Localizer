using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Internationalization.Enum;
using Internationalization.Exception;
using Internationalization.FileProvider.FileHandler.ExcelApp;
using Internationalization.FileProvider.Interface;
using Internationalization.Model;
using Internationalization.Utilities;
using Microsoft.Extensions.Logging;

namespace Internationalization.FileProvider.Excel
{
    /// <summary>
    /// Saves its files using an Excel Application.
    /// This Excel Application will always be closed under normal circumstances,
    /// if however the execution is aborted (because of an exception or the stop
    /// debugging button) while an Excel process stays idle,
    /// it will stick around and may need to be terminated using the Task Manager.
    /// </summary>
    public class ExcelFileProvider : IFileProvider
    {
        private static ILogger _logger;

        private readonly string _path;
        private readonly string _backupPath;
        private readonly ExcelFileHandler _fileHandler;

        //0: initialization is not yet started or completed.
        //1: initialization is already started and running.
        private BackgroundWorker _backgroundWorker;
        private Dictionary<CultureInfo, Dictionary<string, string>> _dictOfDicts =
            new Dictionary<CultureInfo, Dictionary<string, string>>();

        public ProviderStatus Status { get; private set; }

        /// <summary>
        /// Creates the instance of the ExcelFileProvider, which reads and persists all translations as Excel-files.
        /// A backup Excel-file will be created for all edits.
        /// </summary>
        /// <param name="translationFilePath">Path to the file containing the translations.</param>
        /// <param name="glossaryTag">
        /// (Optional) Entries in the Excel table that start with this tag will be interpreted as part of the glossary.
        /// </param>
        /// <param name="oldTranslationFilePath">
        /// (Optional) The path to where the original translation file will be copied as a backup.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown, if <paramref name="translationFilePath"/> is null.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown, if the permissions are missing that are needed to create the directory for
        /// <paramref name="translationFilePath"/> / <paramref name="oldTranslationFilePath"/> or one of them
        /// is write-only, read-only, a directory, hidden, the needed permissions for opening or writing are
        /// missing or the operation is not supported on the current platform.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// Thrown, if certain permissions are missing. (CLR level)
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown, if <paramref name="translationFilePath"/> / <paramref name="oldTranslationFilePath"/>
        /// does not exist or cannot be found.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown, if an unknown I/O-Error occurs.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown, if <paramref name="translationFilePath"/> / <paramref name="oldTranslationFilePath"/>
        /// contains a colon anywhere other than as part of a volume identifier ("C:\").
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// Thrown, if <paramref name="translationFilePath"/> / <paramref name="oldTranslationFilePath"/>
        /// is too long.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown, if the directory was not found.
        /// For example because it is on an unmapped device.
        /// </exception>
        public ExcelFileProvider(string translationFilePath, string glossaryTag = null,
            string oldTranslationFilePath = null)
        {
            //set Status.
            Status = ProviderStatus.InitializationInProgress;

            //easy initializations.
            _logger = GlobalSettings.LibraryLoggerFactory.CreateLogger<ExcelFileProvider>();
            _logger.Log(LogLevel.Trace, "Initializing ExcelFileProvider.");
            _fileHandler = new ExcelFileHandler(typeof(ExcelFileProvider), glossaryTag);

            //null check.
            ExceptionLoggingUtils.ThrowIfNull(_logger, (object) translationFilePath, nameof(translationFilePath),
                "Unable to open null path.", "ExcelFileProvider received null parameter in constructor.");
            
            //start proper initialization.
            if(oldTranslationFilePath != null) {
                _fileHandler.VerifyPath(oldTranslationFilePath);
                _backupPath = oldTranslationFilePath;
            }
            _fileHandler.VerifyPath(translationFilePath);
            _path = translationFilePath;
            _fileHandler.Path = _path;

            Initialize();
        }

        /// <summary>
        /// Returns the internal dictionary of translations.
        /// </summary>
        /// <exception cref="FileProviderNotInitializedException">
        /// Will be thrown if the object has not found a language file to pull translations from.
        /// </exception>
        public Dictionary<CultureInfo, Dictionary<string, string>> GetDictionary()
        {
            switch (Status)
            {
                case ProviderStatus.Empty:
                {
                    var minimalDict = new Dictionary<CultureInfo, Dictionary<string, string>>
                    {
                        { Thread.CurrentThread.CurrentUICulture, new Dictionary<string, string>() }
                    };

                    return minimalDict;
                }
                case ProviderStatus.Initialized:
                {
                    return _dictOfDicts;
                }
                default:
                {
                    //ExcelFileProvider is still initializing, cancelling or cancelled.
                    ExceptionLoggingUtils.Throw<FileProviderNotInitializedException>(_logger,
                        "Dictionary was accessed, without ExcelFileProvider being initialized.");

                    //TODO er sollte nie hier hin kommen, aber meckert trotzdem
                    throw new NotSupportedException("unreachable code.");
                }
            }
        }

        /// <summary>
        /// Updates the internal dictionary of translations at <paramref name="key"/> with the given dictionary.
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
            ExceptionLoggingUtils.ThrowIfNull(_logger, nameof(Update), (object) key, nameof(key),
                "Unable to update dictionary for null key.");
            if (texts == null)
            {
                //no exception has to be thrown here, because null is treated like empty list and
                //no translations will be updated.
                _logger.Log(LogLevel.Debug, "Unable to update dictionary for null translations. " +
                                            "No translations were updated.");
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
            if (Status == ProviderStatus.Empty)
            {
                _logger.Log(LogLevel.Debug, "First update after empty sheet was created.");
                _fileHandler.ExcelWriteActions(_dictOfDicts);

                Status = ProviderStatus.Initialized;
                _logger.Log(LogLevel.Information, "Finished updating dictionary. " +
                                            "ExcelFileProvider is now in State Initialized.");
            }
            else
            {
                _logger.Log(LogLevel.Debug, "Finished updating dictionary.");
            }
        }

        /// <summary>
        /// Persists the current dictionary of translations, by writing it to the given excel file.
        /// </summary>
        public void SaveDictionary()
        {
            _logger.Log(LogLevel.Trace, "SaveDictionary was called.");

            _fileHandler.ExcelWriteActions(_dictOfDicts);

            _logger.Log(LogLevel.Debug, "Dictionary was saved without errors.");
        }


        /// <summary>
        /// Interrupts the Initialization (e.g. when shutting down the application during initialization)
        /// </summary>
        public void CancelInitialization()
        {
            if (_backgroundWorker == null) return;

            if (_backgroundWorker.IsBusy)
            {
                Status = ProviderStatus.CancellationInProgress;
                _logger.Log(LogLevel.Debug, "Cancellation started.");
                _backgroundWorker.CancelAsync();
            }
        }


        /// <summary>
        /// Updates the internal dictionary of translations using the given values and returns true, if any updates were made.
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
        /// True, if at least one language translation was updated.
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
                    _logger.Log(LogLevel.Information,
                        $"New language dictionary was created for {textLocalization.Language.EnglishName}.");
                }

                if (langDict.ContainsKey(key))
                {
                    langDict.Remove(key);
                    _logger.Log(LogLevel.Debug, "Updated existing entry for given value.");
                }
                else
                {
                    _logger.Log(LogLevel.Debug, "Created new entry for given value.");
                }

                langDict.Add(key, textLocalization.Text);

                readSuccess = true;
            }

            return readSuccess;
        }

        /// <summary>
        /// Class internal clean up after BackgroundWorker finished.
        /// </summary>
        private void LoadExcelLanguageFileAsyncCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (Status == ProviderStatus.CancellationInProgress)
            {
                Status = ProviderStatus.CancellationComplete;
                _logger.Log(LogLevel.Information,
                    "Finished cancellation. ExcelFileProvider is now in State CancellationComplete.");

                return;
            }

            if (e.Error != null)
            {
                Status = ProviderStatus.Empty;
                _logger.Log(LogLevel.Information,
                    $"Finished initialization with {e.Error.GetType()} ({e.Error.Message}). " +
                    "ExcelFileProvider is now in State Empty.");

                return;
            }

            //Result is read here, because it is only now guarenteed to exist.
            _dictOfDicts = e.Result as Dictionary<CultureInfo, Dictionary<string, string>>;

            if (_dictOfDicts == null || _dictOfDicts.Count == 0)
            {
                Status = ProviderStatus.Empty;
                _logger.Log(LogLevel.Information, "Was unable to collect information from file. " +
                                            "ExcelFileProvider is now in State Empty.");
            }
            else
            {
                Status = ProviderStatus.Initialized;
                _logger.Log(LogLevel.Information,
                    "Finished initialization. ExcelFileProvider is now in State Initialized.");
            }

            _logger.Log(LogLevel.Information, $"Initialization finished in State #{Status}.");
        }

        private void Initialize()
        {
            //logging.
            _logger.Log(LogLevel.Trace, "Entering Initialize function.");

            //creating backup.
            if (_backupPath == null)
            {
                _logger.Log(LogLevel.Debug, "No backup file will be created.");
            }
            else
            {
                _fileHandler.CopyBackupWrapper(_path, _backupPath);
            }

            //setting up BackgroundWorker.
            if (File.Exists(_path))
            {
                _backgroundWorker = new BackgroundWorker();
                _backgroundWorker.DoWork += _fileHandler.LoadExcelLanguageFileAsync;
                _backgroundWorker.RunWorkerCompleted += _fileHandler.LoadExcelLanguageFileAsyncCompleted;
                _backgroundWorker.RunWorkerCompleted += LoadExcelLanguageFileAsyncCompleted;
                _backgroundWorker.WorkerSupportsCancellation = true;

                _logger.Log(LogLevel.Debug, "Starting BackgroundWorker.");
                _backgroundWorker.RunWorkerAsync();
            }
            else
            {
                _logger.Log(LogLevel.Debug,
                    $"Unable to find language file ({Path.GetFullPath(_path)}).");

                _fileHandler.ExcelWriteActions(null);

                Status = ProviderStatus.Empty;
                _logger.Log(LogLevel.Debug, "Ended new excel file creation.");
            }
        }
    }
}