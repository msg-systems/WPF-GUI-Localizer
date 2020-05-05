using System;
using System.IO;
using Microsoft.Extensions.Logging;

namespace Internationalization.FileProvider.FileHandler.Universal
{
    public class UniversalFileHandler
    {
        private static ILogger _logger;

        private readonly string _fileEnding;

        public UniversalFileHandler(Type typeOfUser, string fileEnding)
        {
            _logger = GlobalSettings.LibraryLoggerFactory.CreateLogger<UniversalFileHandler>();
            _fileEnding = fileEnding;

            _logger.Log(LogLevel.Trace, $"Created FileHandler for {typeOfUser.Name}.");
        }

        protected UniversalFileHandler(string typeOfUser, string fileEnding)
        {
            _logger = GlobalSettings.LibraryLoggerFactory.CreateLogger<UniversalFileHandler>();
            _fileEnding = fileEnding;

            _logger.Log(LogLevel.Trace, $"Created FileHandler for {typeOfUser}.");
        }

        /// <summary>
        /// Handles Exception logging for the given <paramref name="path"/>.
        /// Returns the given <paramref name="path"/> with the appropriate ending, if it was not present.
        /// </summary>
        /// <param name="path">The path that should </param>
        /// <returns>
        /// The given <paramref name="path"/> with the appropriate ending, if it was not present.
        /// </returns>
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
        /// <exception cref="FileNotFoundException">
        /// Thrown, if <paramref name="path"/> is a dictionary.
        /// </exception>
        public string GetPathAndHandleProblems(string path)
        {
            if (!path.EndsWith(_fileEnding))
            {
                _logger.Log(LogLevel.Debug, $"Added '{_fileEnding}' to path ({path}).");
                path += _fileEnding;
            }

            var fullPath = GetFullPathWrapper(path);

            if (File.Exists(fullPath))
            {
                return fullPath;
            }

            if (Directory.Exists(fullPath))
            {
                var fntException = new FileNotFoundException(
                    "Given path is directory instead of file.", path);
                _logger.Log(LogLevel.Error, fntException, "Unable to find file, because directory " +
                                                          "with same name exists.");
                throw fntException;
            }

            CreateDirectoryWrapper(fullPath);

            return path;
        }

        /// <summary>
        /// Handles Exception logging for the <see cref="Path.GetFullPath(string)"/> function.
        /// </summary>
        /// <param name="path">
        /// The path that should be used for the GetFullPath call.
        /// Is assumed to not be null, because it should have already been checked in the users constructor.
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
                _logger.Log(LogLevel.Error, e, "There appear to be some problems with the given " +
                                               $"path ({path}).\n" +
                                               "... It may be empty, contain only white space, include " +
                                               "unsupported characters or the system may have " +
                                               "failed to get the fully qualified " +
                                               "location for given path.");
                throw;
            }
            catch (System.Security.SecurityException e)
            {
                _logger.Log(LogLevel.Error, e, $"Unable to access path ({path}), due to missing permissions.");
                throw;
            }
            catch (NotSupportedException e)
            {
                _logger.Log(LogLevel.Error, e, "There appear to be some problems with the given "+
                                               $"path ({path}). It contains a colon in an invalid "+
                                               "position.");
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
        /// It is assumed that <see cref="GetFullPathWrapper"/> was called prior to this function
        /// and that no exceptions were thrown.
        /// </summary>
        /// <param name="fullPath">
        /// The path of the file, for which a directory should be created.
        /// Is assumed to not be null, because it should have already been checked in the users constructor.
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
                _logger.Log(LogLevel.Trace, "Given path does not contain directory information. " +
                                            "No directory will be created.");
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
                    _logger.Log(LogLevel.Error, e, $"Unable to create directory ({directory}) needed " +
                                                   $"for path ({fullPath}), due to missing permissions.");
                    throw;
                }
                catch (DirectoryNotFoundException e)
                {
                    _logger.Log(LogLevel.Error, e, $"Unable to create directory ({directory}) needed " +
                                                   $"for path ({fullPath}). The directory was not found. " +
                                                   "(It may lie on an unmapped device)");
                    throw;
                }
                catch (IOException e)
                {
                    _logger.Log(LogLevel.Error, e, $"Unable to create directory ({directory}) needed " +
                                                   $"for path ({fullPath}). The directory has conflicts with " +
                                                   "names of existing files.");
                    throw;
                }

                //success.
                _logger.Log(LogLevel.Information, $"Created directory for {_fileEnding} file ({fullPath}).");
            }
        }

        /// <summary>
        /// Handles Exception logging for the <see cref="File.ReadAllText(string)"/>
        /// function based on <paramref name="path"/>.
        /// It is assumed that <see cref="GetPathAndHandleProblems"/> was called prior to
        /// this function and that no errors were thrown.
        /// </summary>
        /// <param name="path">
        /// The path of the file that <see cref="File.ReadAllText(string)"/> should read.
        /// </param>
        /// <returns>The text returned by the <see cref="File.ReadAllText(string)"/> call.</returns>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown, if <paramref name="path"/> is write-only, a directory, the needed permissions
        /// are missing or the operation is not supported on the current platform.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// Thrown, if certain permissions are missing. (CLR level)
        /// </exception>
        /// <exception cref="FileNotFoundException">
        /// Thrown, if <paramref name="path"/> does not exist or cannot be found.
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown, if an unknown I/O-Error occurres.
        /// </exception>
        public string ReadAllTextWrapper(string path)
        {
            string fileContent;
            try
            {
                fileContent = File.ReadAllText(path);
                //uncaught Exceptions:
                //ArgumentException, ArgumentNullException, PathTooLongException
                //and NotSupportedException are not caught, because GetPathAndHandleProblems
                //ran without errors and they can therefore not occur here.
            }
            catch (UnauthorizedAccessException e)
            {
                //Documentation lists the following reasons for UnauthorizedAccessException:
                //      Path specified a file that is read-only. (probably meant write-only or not applicaple at all)
                //-or-  This operation is not supported on the current platform.
                //-or-  Path specified a directory.
                //-or-  The caller does not have the required permission.

                _logger.Log(LogLevel.Error, e, $"Unable to open the language file ({path}), due to " +
                                               "missing permissions.");
                throw;
            }
            catch (System.Security.SecurityException e)
            {
                _logger.Log(LogLevel.Error, e, $"Unable to open the language file ({path}), due to " +
                                               "missing permissions.");
                throw;
            }
            catch (FileNotFoundException e)
            {
                _logger.Log(LogLevel.Error, e, $"Unable to find the language file ({path}).");
                throw;
            }
            catch (DirectoryNotFoundException e)
            {
                _logger.Log(LogLevel.Error, e, $"Unable to open the language file ({path}), due to " +
                                               "the directory was not being found. " +
                                               "(It may lie on an unmapped device)");
                throw;
            }
            catch (IOException e)
            {
                _logger.Log(LogLevel.Error, e, "Unable to open language file, due to an unknown I/O error " +
                                               $"that occurred while opening the file ({path}).");
                throw;
            }

            return fileContent;
        }

        /// <summary>
        /// Handles Exception logging for the <see cref="File.WriteAllText(string, string)"/>
        /// function based on <paramref name="path"/>.
        /// It is assumed that <see cref="GetPathAndHandleProblems"/> was called prior to
        /// this function for the same value of <paramref name="path"/> and that no exceptions were thrown.
        /// </summary>
        /// <param name="fileContent">
        /// The string that should be written to the file at <paramref name="path"/>
        /// </param>
        /// <param name="path">
        /// The path at which <see cref="File.WriteAllText(string, string)"/> should
        /// write <paramref name="fileContent"/>.
        /// </param>
        /// <exception cref="UnauthorizedAccessException">
        /// Thrown, if <paramref name="path"/> is read-only, a directory, hidden, the needed permissions
        /// are missing or the operation is not supported on the current platform.
        /// </exception>
        /// <exception cref="System.Security.SecurityException">
        /// Thrown, if certain permissions are missing. (CLR level)
        /// </exception>
        /// <exception cref="IOException">
        /// Thrown, if an unknown I/O-Error occurres.
        /// </exception>
        public void WriteAllTextWrapper(string fileContent, string path)
        {
            try
            {
                File.WriteAllText(path, fileContent);
                //uncaught Exceptions:
                //ArgumentException, ArgumentNullException, PathTooLongException
                //and NotSupportedException are not caught, because GetPathAndHandleProblems
                //ran without errors and they can therefore not occur here.
            }
            catch (UnauthorizedAccessException e)
            {
                //Documentation lists the following reasons for UnauthorizedAccessException:
                //      Path specified a file that is read-only.
                //-or-  Path specified a file that is hidden.
                //-or-  This operation is not supported on the current platform.
                //-or-  Path specified a directory.
                //-or-  The caller does not have the required permission.

                _logger.Log(LogLevel.Error, e, $"Unable to write to language file ({path}), due to " +
                                               "missing permissions.");
                throw;
            }
            catch (System.Security.SecurityException e)
            {
                _logger.Log(LogLevel.Error, e, $"Unable to write to language file ({path}), due to " +
                                               "missing permissions.");
                throw;
            }
            catch (DirectoryNotFoundException e)
            {
                _logger.Log(LogLevel.Error, e, $"Unable to open the language file ({path}), due to " +
                                               "the directory was not being found. " +
                                               "(It may lie on an unmapped device)");
                throw;
            }
            catch (IOException e)
            {
                _logger.Log(LogLevel.Error, e, $"Unable to write to language file ({path}), due to an " +
                                               "unknown I/O error.");
                throw;
            }
        }

        /// <summary>
        /// Handles Exception logging for the <see cref="File.Copy(string, string, bool)"/>
        /// function based on the given parameters.
        /// <see cref="File.Copy(string, string, bool)"/> is only invoked, if no file is present
        /// at <paramref name="toPath"/>.
        /// It is assumed that <see cref="GetPathAndHandleProblems"/> was called prior to
        /// this function for the value of <paramref name="fromPath"/> and <paramref name="toPath"/>
        /// and that no exceptions were thrown.
        /// </summary>
        /// <param name="fromPath">The path of the original file.</param>
        /// <param name="toPath">The path of the destination.</param>
        public void CopyBackupWrapper(string fromPath, string toPath)
        {
            var fromFullPath = Path.GetFullPath(fromPath);
            var toFullPath = Path.GetFullPath(toPath);
            if (!File.Exists(toFullPath))
            {
                try
                {
                    File.Copy(fromFullPath, toFullPath);
                    //uncaught Exceptions:
                    //ArgumentException, ArgumentNullException, PathTooLongException
                    //and NotSupportedException because GetPathAndHandleProblems
                    //ran without errors and they can therefore not occur here.
                }
                catch (UnauthorizedAccessException e)
                {
                    _logger.Log(LogLevel.Error, e, $"Unable to backup language file ({fromPath}), due to " +
                                                   "missing permissions.");
                    throw;
                }
                catch (DirectoryNotFoundException e)
                {
                    _logger.Log(LogLevel.Error, e, $"Unable to open the language file ({fromPath}), due to " +
                                                   "the directory was not being found. " +
                                                   "(It may lie on an unmapped device)");
                    throw;
                }
                catch (FileNotFoundException e)
                {
                    _logger.Log(LogLevel.Error, e, $"Unable to backup language file ({fromPath}), due to " +
                                                   "the file not being found.");
                    throw;
                }
                catch (IOException e)
                {
                    _logger.Log(LogLevel.Error, e, $"Unable to backup language file ({fromPath}), due an " +
                                                   "unknown I/O error.");
                    throw;
                }
            }
            else
            {
                _logger.Log(LogLevel.Trace, "Backup file already created, No new backup was made.");
            }
        }
    }
}
