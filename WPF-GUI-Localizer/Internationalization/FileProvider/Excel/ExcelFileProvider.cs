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
using Microsoft.Extensions.Logging;

namespace Internationalization.FileProvider.Excel
{
    /// <summary>
    /// Saves its files using an Excel Application.
    /// This Excel Application will under normal circumstances always be closed,
    /// if however the execution is aborted (because of an exception or the stop
    /// debugging button) while an Excel process is still running,
    /// it will stick around and will need to be terminated using the Task Manager.
    /// </summary>
    public class ExcelFileProvider : IFileProvider
    {
        private static ILogger _logger;

        private readonly string _path;
        private readonly string _backupPath;
        private readonly ExcelFileHandler _fileHandler;

        //0: initialization is not yet started or completed.
        //1: initialization is already started and running.
        private int _isInitializing;
        private BackgroundWorker _backgroundWorker;
        private int _numKeyParts;
        private Dictionary<CultureInfo, Dictionary<string, string>> _dictOfDicts =
            new Dictionary<CultureInfo, Dictionary<string, string>>();

        public ProviderStatus Status { get; private set; }

        /// <summary>Saves file as Excel, a backup will be created before the file is edited.</summary>
        /// <param name="translationFilePath">File that will be worked on being worked on.</param>
        /// <param name="glossaryTag">
        /// Entries in the Excel table that start with this tag will be interpreted as part of the glossary.
        /// </param>
        /// <param name="oldTranslationFilePath">
        /// A copy of the original sheet will be put here if no copy exists jet.
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
        /// Thrown, if an unknown I/O-Error occurres.
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
            if (translationFilePath == null)
            {
                var e = new ArgumentNullException(nameof(translationFilePath), "Unable to open null path.");
                _logger.Log(LogLevel.Error, e,
                    $"ExcelFileProvider recived null parameter in constructor for {nameof(translationFilePath)}.");
                throw e;
            }

            //start proper initialization.
            if(oldTranslationFilePath != null) {
                _backupPath = _fileHandler.GetPathAndHandleProblems(oldTranslationFilePath);
            }
            _path = _fileHandler.GetPathAndHandleProblems(translationFilePath);
            _fileHandler.Path = _path;
            Initialize();
        }

        /// <summary>
        /// Returns the internal dictionary.
        /// </summary>
        /// <exception cref="FileProviderNotInitializedException">
        /// Will be thrown if the object has not found a language file to pull translations from.
        /// </exception>
        public Dictionary<CultureInfo, Dictionary<string, string>> GetDictionary()
        {
            if (Status == ProviderStatus.Empty)
            {
                var minimalDict = new Dictionary<CultureInfo, Dictionary<string, string>>
                {
                    { Thread.CurrentThread.CurrentUICulture, new Dictionary<string, string>() }
                };

                return minimalDict;
            }
            else if (Status == ProviderStatus.Initialized)
            {
                return _dictOfDicts;
            }

            //ExcelFileProvider is still initializing, cancelling or cancelled.
            var e = new FileProviderNotInitializedException(
                "Dictionary was accessed, without ExcelFileProvider being initialized.");
            _logger.Log(LogLevel.Error, e,
                "Dictionary was accessed, without ExcelFileProvider being initialized.");
            throw e;
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
                _numKeyParts = key.Split(Properties.Settings.Default.Seperator_for_partial_Literalkeys).Length;
                _fileHandler.ExcelWriteActions(_dictOfDicts);

                Status = ProviderStatus.Initialized;
            }
            else
            {
                _logger.Log(LogLevel.Trace, "Finished updating dictionary.");
            }
        }

        /// <summary>
        /// Makes the current dictionary persistent, by writing it to an excel file (.xlsx).
        /// </summary>
        public void SaveDictionary()
        {
            _logger.Log(LogLevel.Trace, "SaveDictionary was called.");

            _fileHandler.ExcelWriteActions(_dictOfDicts);

            _logger.Log(LogLevel.Trace, "Dictionary was saved without errors.");
        }

        public void CancelInitialization()
        {
            if (_backgroundWorker == null) return;

            if (_backgroundWorker.IsBusy)
            {
                Status = ProviderStatus.CancellationInProgress;
                _logger.Log(LogLevel.Trace, "Cancellation started.");
                _backgroundWorker.CancelAsync();
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

        /// <summary>
        /// Class internal clean up after BackgroundWorker finished.
        /// </summary>
        private void LoadExcelLanguageFileAsyncCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Interlocked.Exchange(ref _isInitializing, 0);

            if (Status == ProviderStatus.CancellationInProgress)
            {
                Status = ProviderStatus.CancellationComplete;
                _logger.Log(LogLevel.Trace,
                    "Finished cancellation. ExcelFileProvider is now in State CancellationComplete.");

                return;
            }

            if (e.Error != null)
            {
                Status = ProviderStatus.Empty;
                _logger.Log(LogLevel.Trace,
                    $"Finished initialization with {e.Error.GetType()} ({e.Error.Message}). " +
                    "ExcelFileProvider is now in State Empty.");

                return;
            }

            //Result is read here, because it is only now guarenteed to exist.
            _dictOfDicts = e.Result as Dictionary<CultureInfo, Dictionary<string, string>>;

            if (_dictOfDicts == null || _dictOfDicts.Count == 0)
            {
                Status = ProviderStatus.Empty;
                _logger.Log(LogLevel.Trace, "Was unable to collect information from file. " +
                                            "ExcelFileProvider is now in State Empty.");
            }
            else
            {
                Status = ProviderStatus.Initialized;
                _logger.Log(LogLevel.Trace,
                    "Finished initialization. ExcelFileProvider is now in State Initialized.");
            }

            _logger.Log(LogLevel.Debug, $"Initialization finished in State #{Status}.");
        }

        private void Initialize()
        {
            //logging.
            _logger.Log(LogLevel.Trace, "Entering Initialize function.");

            //creating backup.
            if (_backupPath == null)
            {
                _logger.Log(LogLevel.Trace, "No backup file will be created.");
            }
            else
            {
                _fileHandler.CopyBackupWrapper(_path, _backupPath);
            }

            //setting up BackgroundWorker.
            if (Status == ProviderStatus.InitializationInProgress &&
                Interlocked.Exchange(ref _isInitializing, 1) == 0)
            {

                if (File.Exists(_path))
                {
                    _backgroundWorker = new BackgroundWorker();
                    _backgroundWorker.DoWork += _fileHandler.LoadExcelLanguageFileAsync;
                    _backgroundWorker.RunWorkerCompleted += _fileHandler.LoadExcelLanguageFileAsyncCompleted;
                    _backgroundWorker.RunWorkerCompleted += LoadExcelLanguageFileAsyncCompleted;
                    _backgroundWorker.WorkerSupportsCancellation = true;

                    _logger.Log(LogLevel.Trace, "Starting BackgroundWorker.");
                    _backgroundWorker.RunWorkerAsync();
                }
                else
                {
                    _logger.Log(LogLevel.Debug,
                        $"Unable to find language file ({Path.GetFullPath(_path)}).");

                    _fileHandler.ExcelWriteActions(null);

                    Status = ProviderStatus.Empty;
                    _logger.Log(LogLevel.Trace, "Ended new excel file creation.");
                }
            }
            else
            {
                _logger.Log(LogLevel.Information, "Initialize function was called again before finishing.");
            }
        }
    }
}