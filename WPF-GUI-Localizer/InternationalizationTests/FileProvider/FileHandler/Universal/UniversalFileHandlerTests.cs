using System;
using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Internationalization.FileProvider.FileHandler.Universal.Tests
{
    [TestClass()]
    public class UniversalFileHandlerTests
    {
        private const string TextFilePath = @"UniversalFHTestResources\Text.txt";
        private readonly string _textFileContent = "Hello, World!" + Environment.NewLine + "This is a test.";

        [TestCleanup]
        public void Cleanup()
        {
            SetFileReadAccess(TextFilePath, false);
        }

        [TestMethod()]
        public void VerifyPath_NullPath_ThrowsArgumentNullException()
        {
            //Arrange
            string path = null;
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));
            
            //Act //Assert
            Assert.ThrowsException<ArgumentNullException>(() => ufh.VerifyPath(path));
        }

        [TestMethod()]
        public void VerifyPath_EmptyPath_ThrowsArgumentException()
        {
            //Arrange
            string path = string.Empty;
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<ArgumentException>(() => ufh.VerifyPath(path));
        }

        [TestMethod()]
        public void VerifyPath_WhiteSpacePath_ThrowsArgumentException()
        {
            //Arrange
            string path = "   ";
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<ArgumentException>(() => ufh.VerifyPath(path));
        }

        [TestMethod()]
        public void VerifyPath_PathWithUnsupportedChars_ThrowsArgumentException()
        {
            //Arrange
            //Testing OS specific, because behaviour of ufh is also OS specific.
            var notSupported = Path.GetInvalidPathChars();
            var magicNumber = Math.Min(3, notSupported.Length);
            string path = $"Evil_D{notSupported[magicNumber]}rectory/Lang_File";
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<ArgumentException>(() => ufh.VerifyPath(path));
        }

        [TestMethod()]
        public void VerifyPath_PathWithInvalidColon_ThrowsNotSupportedException()
        {
            //Arrange
            string path = "/Evil_D:rectory/Lang_File";
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<NotSupportedException>(() => ufh.VerifyPath(path));
        }

        [TestMethod()]
        public void VerifyPath_TooLongPath_ThrowsPathTooLangException()
        {
            //Arrange
            string path = FindShortestStringThatTriggersPathTooLongException();
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<PathTooLongException>(() => ufh.VerifyPath(path));
        }

        [TestMethod()]
        public void VerifyPath_PathIsDirectory_ThrowsFileNotFoundException()
        {
            //Arrange
            string path = "UniversalFHTestResources";
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<FileNotFoundException>(() => ufh.VerifyPath(path));
        }

        [TestMethod()]
        public void ReadAllTextWrapper_FileExists_ContentIsRead()
        {
            //Arrange
            string path = TextFilePath;
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act
            var readText = ufh.ReadAllTextWrapper(path);

            //Assert
            Assert.AreEqual(readText, _textFileContent);
        }

        [TestMethod()]
        public void ReadAllTextWrapper_FileDoesNotExists_ThrowsFileNotFoundException()
        {
            //Arrange
            string nonexistentPath = "UniversalFHTestResources/Not_Text.txt";
            if (File.Exists(nonexistentPath) || Directory.Exists(nonexistentPath))
            {
                Assert.Fail("Path that should not exist exists.");
            }
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<FileNotFoundException>(() => ufh.ReadAllTextWrapper(nonexistentPath));
        }

        [TestMethod()]
        public void ReadAllTextWrapper_PathIsDirectory_ThrowsUnauthorizedAccessException()
        {
            //Arrange
            string path = "UniversalFHTestResources";
            var ufh = new UniversalFileHandler(typeof(UniversalFileHandlerTests));

            //Act //Assert
            Assert.ThrowsException<UnauthorizedAccessException>(() => ufh.ReadAllTextWrapper(path));
        }

        [TestMethod()]
        public void WriteAllTextWrapperTest()
        {
            Assert.Fail();//TODO
        }

        [TestMethod()]
        public void CopyBackupWrapperTest()
        {
            Assert.Fail();//TODO
        }

        /// <summary>
        /// As there is not way of reliably getting the conditions for <see cref="PathTooLongException"/>
        /// excluding a string longer than <see cref="Int16.MaxValue"/>, this function is used.
        /// </summary>
        /// <returns></returns>
        private string FindShortestStringThatTriggersPathTooLongException()
        {
            string path = "a";

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

        public static void SetFileReadAccess(string fileName, bool setReadOnly)
        {
            FileInfo fInfo = new FileInfo(fileName)
            {
                IsReadOnly = setReadOnly
            };
        }
    }
}