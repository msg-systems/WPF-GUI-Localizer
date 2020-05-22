using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Internationalization.FileProvider.FileHandler.Universal.Tests
{
    [TestClass]
    public class UniversalFileHandlerTests
    {
        private const string TextFilePath = @"TestResources\UniversalFHTestResources\Text.txt";
        private const string NonExistentFilePath = @"TestResources\UniversalFHTestResources\Not_Text.txt";
        private static readonly string TextFileContent = "Hello, World!" + Environment.NewLine + "This is a test.";

        [TestCleanup]
        public void Cleanup()
        {
            SetFileReadAccess(TextFilePath, false);
        }

        [TestMethod]
        public void VerifyPath_NullPath_ThrowsArgumentNullException()
        {
            //Arrange
            string path = null;
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<ArgumentNullException>(() => ufh.VerifyPath(path));
        }

        [TestMethod]
        public void VerifyPath_EmptyPath_ThrowsArgumentException()
        {
            //Arrange
            var path = string.Empty;
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<ArgumentException>(() => ufh.VerifyPath(path));
        }

        [TestMethod]
        public void VerifyPath_WhiteSpacePath_ThrowsArgumentException()
        {
            //Arrange
            var path = "   ";
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<ArgumentException>(() => ufh.VerifyPath(path));
        }

        [TestMethod]
        public void VerifyPath_PathWithUnsupportedChars_ThrowsArgumentException()
        {
            //Arrange
            //Testing OS specific, because behaviour of ufh is also OS specific.
            var notSupported = Path.GetInvalidPathChars();
            var magicNumber = Math.Min(3, notSupported.Length);
            var path = $"Evil_D{notSupported[magicNumber]}rectory/Lang_File";
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<ArgumentException>(() => ufh.VerifyPath(path));
        }

        [TestMethod]
        public void VerifyPath_PathWithInvalidColon_ThrowsNotSupportedException()
        {
            //Arrange
            var path = "/Evil_D:rectory/Lang_File";
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<NotSupportedException>(() => ufh.VerifyPath(path));
        }

        [TestMethod]
        public void VerifyPath_TooLongPath_ThrowsPathTooLangException()
        {
            //Arrange
            var path = FindShortestStringThatTriggersPathTooLongException();
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<PathTooLongException>(() => ufh.VerifyPath(path));
        }

        [TestMethod]
        public void VerifyPath_PathIsDirectory_ThrowsFileNotFoundException()
        {
            //Arrange
            var path = @"TestResources\UniversalFHTestResources";
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<FileNotFoundException>(() => ufh.VerifyPath(path));
        }

        [TestMethod]
        public void ReadAllTextWrapper_FileExists_ContentIsRead()
        {
            //Arrange
            var path = TextFilePath;
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act
            var readText = ufh.ReadAllTextWrapper(path);

            //Assert
            Assert.AreEqual(readText, TextFileContent);
        }

        [TestMethod]
        public void ReadAllTextWrapper_FileDoesNotExists_ThrowsFileNotFoundException()
        {
            //Arrange
            var nonexistentPath = GetNonExistentPath();
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<FileNotFoundException>(() => ufh.ReadAllTextWrapper(nonexistentPath));
        }

        [TestMethod]
        public void ReadAllTextWrapper_PathIsDirectory_ThrowsUnauthorizedAccessException()
        {
            //Arrange
            var path = @"TestResources\UniversalFHTestResources";
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<UnauthorizedAccessException>(() => ufh.ReadAllTextWrapper(path));
        }

        [TestMethod]
        public void WriteAllTextWrapperTest()
        {
            //Arrange
            var expectedContent = TextFileContent;
            var nonexistentPath = GetNonExistentPath();
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act
            ufh.WriteAllTextWrapper(expectedContent, nonexistentPath);

            //Assert
            Assert.IsTrue(File.Exists(nonexistentPath));
            Assert.AreEqual(expectedContent, File.ReadAllText(nonexistentPath));

            //Cleanup
            File.Delete(nonexistentPath);
        }

        [TestMethod]
        public void CopyBackupWrapperTest()
        {
            //Arrange
            var path = TextFilePath;
            var nonexistentPath = GetNonExistentPath();
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act
            ufh.CopyBackupWrapper(path, nonexistentPath);

            //Assert
            Assert.IsTrue(File.Exists(nonexistentPath));
            Assert.AreEqual(TextFileContent, File.ReadAllText(nonexistentPath));

            //Cleanup
            File.Delete(nonexistentPath);
        }

        /// <summary>
        ///     As there is not way of reliably getting the conditions for <see cref="PathTooLongException" />
        ///     excluding a string longer than <see cref="Int16.MaxValue" />, this function is used.
        /// </summary>
        /// <returns></returns>
        private string FindShortestStringThatTriggersPathTooLongException()
        {
            var path = "a";

            while (path.Length < short.MaxValue)
            {
                path += "a";
                try
                {
                    Path.GetFullPath(path);
                }
                catch (PathTooLongException)
                {
                    break;
                }
            }

            return path;
        }

        private static void SetFileReadAccess(string fileName, bool setReadOnly)
        {
            var fInfo = new FileInfo(fileName)
            {
                IsReadOnly = setReadOnly
            };
        }

        private static string GetNonExistentPath()
        {
            if (File.Exists(NonExistentFilePath) || Directory.Exists(NonExistentFilePath))
            {
                Assert.Fail("Path that should not exist exists.");
            }

            return NonExistentFilePath;
        }
    }
}