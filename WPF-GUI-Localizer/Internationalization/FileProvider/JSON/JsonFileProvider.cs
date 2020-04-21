using System;
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
    /// The managed dictionary gets directly saved in its deserialized form.
    /// </summary>
    public class JsonFileProvider : IFileProvider
    {
        private static ILogger _logger;

        private readonly string _path;
        private Dictionary<CultureInfo, Dictionary<string, string>> _dictOfDicts;

        private bool _successfullyCreatedFile;
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

            //null check.
            if (translationFilePath == null)
            {
                var e = new ArgumentNullException(nameof(translationFilePath), "Unable to open null path.");
                _logger.Log(LogLevel.Error, e, "JsonFileProvider recived null parameter in constructor.");
                throw e;
            }

            //start proper initialization.
            _path = GetPathAndHandleProblems(translationFilePath);
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

            WriteAllTextWrapper(JsonConvert.SerializeObject(_dictOfDicts));

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
                _logger.Log(LogLevel.Trace, "Did not update dictionary.");
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
        /// Throws + logs exceptions and in a few cases handles problems regarding the given <paramref name="path"/>.
        /// </summary>
        /// <param name="path">The path that should </param>
        /// <returns>The given path with a ".json" ending, if it was not present.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown, if <paramref name="path"/> contains only white space, includes
        /// unsupported characters or if the system fails to get the fully qualified
        /// location for the given path.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// Thrown, if the permissions for accessing the full path are missing.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown, if <paramref name="path"/> contains a colon anywhere other than as part of a
        /// volume identifier ("C:\").
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// Thrown, if <paramref name="path"/> is too long.
        /// </exception>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown, if permissions to create the directory are missing.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown, if the directory was not found.
        /// For example because it is on an unmapped device.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown, if a file with the name of the dictionary that should be created already exists. 
        /// </exception>
        private string GetPathAndHandleProblems(string path)
        {
            if (!path.EndsWith(".json"))
            {
                _logger.Log(LogLevel.Debug, $"Added '.json' to path ({path}).");
                path += ".json";
            }

            string fullPath = GetFullPathWrapper(path);


            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            CreateDirectoryWrapper(fullPath);
            
            return path;
        }

        /// <summary>
        /// Handles Exception logging for the <see cref="Path.GetFullPath(string)"/> function.
        /// </summary>
        /// <param name="path">
        /// The path that should be used for the GetFullPath call.
        /// Is assumes to not be null, since this was checked in <see cref="JsonFileProvider"/>s constructor.
        /// </param>
        /// <returns>The result of the GetFullPath call.</returns>
        /// <exception cref="ArgumentException">
        /// Thrown, if <paramref name="path"/> contains only white space, includes
        /// unsupported characters or if the system fails to get the fully qualified
        /// location for the given path.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// Thrown, if the permissions for accessing the full path are missing.
        /// </exception>
        /// <exception cref="NotSupportedException">
        /// Thrown, if <paramref name="path"/> contains a colon anywhere other than as part of a
        /// volume identifier ("C:\").
        /// </exception>
        /// <exception cref="PathTooLongException">
        /// Thrown, if <paramref name="path"/> is too long.
        /// </exception>
        private string GetFullPathWrapper(string path)
        {
            string fullPath;

            try
            {
                fullPath = Path.GetFullPath(path);
                //ArgumentNullException is not caught, because path was already
                //checked for being null in constructor.
            }
            catch (ArgumentException e)
            {
                _logger.Log(LogLevel.Error, e, "There appear to be some problems with the given "
                                               + $"path ({path}).\n"
                                               + "... It may be empty, contain only white space, include "
                                               + "unsupported characters or the system may have "
                                               + "failed to get the fully qualified "
                                               + "location for given path.");
                throw;
            }
            catch (System.Security.SecurityException e)
            {
                _logger.Log(LogLevel.Error, e, $"Unable to access path ({path}), due to missing permissions.");
                throw;
            }
            catch (NotSupportedException e)
            {
                _logger.Log(LogLevel.Error, e, "There appear to be some problems with the given "
                                               + $"path ({path}). It contains a colon in an invalid "
                                               + "position.");
                throw;
            }
            catch (PathTooLongException e)
            {
                _logger.Log(LogLevel.Error, e, $"Unable to access path ({path}), because it is too long");
                throw;
            }

            return fullPath;
        }

        /// <summary>
        /// Handles Exception logging for the <see cref="Directory.CreateDirectory(string)"/> function.
        /// It is assumes that <see cref="GetFullPathWrapper"/> was called prior to this function
        /// and that no exceptions were thrown.
        /// </summary>
        /// <param name="fullPath">
        /// The path of the file, for which a directory should be created.
        /// It is assumed to not be null, since this was checked in <see cref="JsonFileProvider"/>s constructor.
        /// </param>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown, if permissions to create the directory are missing.
        /// </exception>
        /// <exception cref="DirectoryNotFoundException">
        /// Thrown, if the directory was not found.
        /// For example because it is on an unmapped device.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown, if a file with the name of the dictionary that should be created already exists. 
        /// </exception>
        private void CreateDirectoryWrapper(string fullPath)
        {

            //GetDirectoryName call should be save, as the same exceptions that can occur here
            //have been checked in GetFullPathWrapper.
            var directory = Path.GetDirectoryName(fullPath);

            //GetDirectoryName returns null if path denotes a root directory or is null. Returns empty string
            //if path does not contain directory information.
            if (directory == null)
            {
                //directory can only be null, if directry is a root directory.
                _logger.Log(LogLevel.Trace, "Given path is a root directory. No directory will be created.");
            }
            else if (string.IsNullOrWhiteSpace(directory))
            {
                _logger.Log(LogLevel.Trace, "Given path does not contain directory information. "
                                            + "No directory will be created.");
            }
            else
            {
                try
                {
                    Directory.CreateDirectory(directory);
                    //uncaught Exceptions:
                    //ArgumentException, PathTooLongException and NotSupportedException
                    //are not caught, because GetDirectoryName will not generate invalid paths.
                    //ArgumentNullException is not caught, because directory was already
                    //checked for being null.
                }
                catch (UnauthorizedAccessException e)
                {
                    _logger.Log(LogLevel.Error, e, $"Unable to create directory ({directory}) needed "
                                                   + $"for path ({fullPath}), due to missing permissions.");
                    throw;
                }
                catch (DirectoryNotFoundException e)
                {
                    _logger.Log(LogLevel.Error, e, $"Unable to create directory ({directory}) needed "
                                                   + $"for path ({fullPath}). The directory was not found. "
                                                   + "It may lie on an unmapped device.");
                    throw;
                }
                catch (IOException e)
                {
                    _logger.Log(LogLevel.Error, e, $"Unable to create directory ({directory}) needed "
                                                   + $"for path ({fullPath}). The directory has conflicts with "
                                                   + "names of existing files.");
                    throw;
                }

                //success.
                _logger.Log(LogLevel.Information, $"Created directory for Json file ({fullPath}).");
            }
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

                fileContent = ReadAllTextWrapper();
            }
            else
            {
                _logger.Log(LogLevel.Trace, $"No langauge file present ({Path.GetFullPath(_path)}).");

                //identical to "JsonConvert.SerializeObject(new Dictionary<CultureInfo, Dictionary<string, string>>())".
                fileContent = "{}";
                WriteAllTextWrapper(fileContent);
            }

            _dictOfDicts =
                JsonConvert.DeserializeObject<Dictionary<CultureInfo, Dictionary<string, string>>>(fileContent);

            //cancelation identical to Initialization.
            switch (Status)//TODO revisit after adding more states to Status
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
        /// Handles Exception logging for the <see cref="File.ReadAllText(string)"/>
        /// function based on <see cref="_path"/>.
        /// It is assumes that <see cref="GetPathAndHandleProblems"/> was called prior to
        /// this function and that no errors were thrown.
        /// </summary>
        /// <returns>The text returned by the <see cref="File.ReadAllText(string)"/> call.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown, if <see cref="_path"/> is write-only, a directory, the needed permissions
        /// are missing or the operation is not supported on the current platform.
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
        private string ReadAllTextWrapper()
        {
            string fileContent;
            try
            {
                fileContent = File.ReadAllText(_path);
                //uncaught Exceptions:
                //ArgumentException, ArgumentNullException, PathTooLongException,
                //DirectoryNotFoundException and NotSupportedException are not
                //caught, because GetPathAndHandleProblems ran without errors.
            }
            catch (UnauthorizedAccessException e)
            {
                //Documentation lists the following reasons for UnauthorizedAccessException:
                //Path specified a file that is read-only. (probably meant write-only or not applicaple at all)
                // -or-
                //This operation is not supported on the current platform.
                // -or-
                //Path specified a directory.
                // -or-
                //The caller does not have the required permission.

                _logger.Log(LogLevel.Error, e, $"Unable to open the language file ({_path}), due to "
                                               + "missing permissions.");
                throw;
            }
            catch (System.Security.SecurityException e)
            {
                _logger.Log(LogLevel.Error, e, $"Unable to open the language file ({_path}), due to "
                                               + "missing permissions.");
                throw;
            }
            catch (FileNotFoundException e)
            {
                _logger.Log(LogLevel.Error, e, $"Unable to find the language file ({_path}).");
                throw;
            }
            catch (IOException e)
            {
                _logger.Log(LogLevel.Error, e, "Unable to open language file, due to an unknown I/O error "
                                               + $"that occurred while opening the file ({_path}).");
                throw;
            }

            //success.
            if ("null".Equals(fileContent))
            {
                _logger.Log(LogLevel.Information, "File had content 'null', will be treated as empty dictionary.");
                fileContent = "{}";
            }

            return fileContent;
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
            bool readSuccess = false;
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
        /// Handles Exception logging for the <see cref="File.WriteAllText(string, string)"/>
        /// function based on <see cref="_path"/>.
        /// It is assumes that <see cref="GetPathAndHandleProblems"/> was called prior to
        /// this function and that no errors were thrown.
        /// </summary>
        /// <param name="fileContent">
        /// The string that should be written to the file at <see cref="_path"/>
        /// </param>
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
        private void WriteAllTextWrapper(string fileContent)
        {
            try
            {
                //ArgumentException, ArgumentNullException, PathTooLongException,
                //DirectoryNotFoundException, IOException, UnauthorizedAccessException,
                //NotSupportedException, SecurityException
                File.WriteAllText(_path, fileContent);

                _successfullyCreatedFile = true;
            }
            catch (UnauthorizedAccessException e)
            {
                //Documentation lists the following reasons for UnauthorizedAccessException:
                //Path specified a file that is read-only.
                // -or-
                //Path specified a file that is hidden.
                // -or-
                //This operation is not supported on the current platform.
                // -or-
                //Path specified a directory.
                // -or-
                //The caller does not have the required permission.

                _logger.Log(LogLevel.Error, e, $"Unable to write to language file ({_path}), due to "
                                               + "missing permissions.");
                throw;
            }
            catch (System.Security.SecurityException e)
            {
                _logger.Log(LogLevel.Error, e, $"Unable to write to language file ({_path}), due to "
                                               + "missing permissions.");
                throw;
            }
            catch (IOException e)
            {
                _logger.Log(LogLevel.Error, e, "Unable to write to language file, due to an unknown I/O error "
                                               + $"that occurred while opening the file ({_path}).");
                throw;
            }
        }
    }
}