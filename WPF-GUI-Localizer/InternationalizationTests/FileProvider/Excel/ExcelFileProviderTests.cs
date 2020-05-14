using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Internationalization.Enum;
using Internationalization.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Internationalization.FileProvider.Excel.Tests
{
    [TestClass()]
    public class ExcelFileProviderTests
    {
        //properties of language file
        private static readonly IList<CultureInfo> ContainingLanguages = new List<CultureInfo>
            {new CultureInfo("en"), new CultureInfo("sv"), new CultureInfo("de"), new CultureInfo("fr")};
        private const int NumberOfRowsInAlmostFull = 14;
        //properties of path language file
        private const string PathBeginningPart = @"ExcelTestResources\Language_File";
        private const string PathEndingPart = ".xlsx";
        private const string PathEmptyPart = "_empty";
        private const string PathAlmostFullPart = "_almost_full";
        private const string PathNoLanguages = "_no_languages";
        //parameters used in constructor
        private const string Path = PathBeginningPart + PathEndingPart;
        private const string GlossaryTag = "gloss";
        private const string BackupPath = PathBeginningPart + "_backup" + PathEndingPart;
        //often used objects with large constructors
        private static readonly IList<TextLocalization> GreetingsExample = new List<TextLocalization>
        {
            new TextLocalization {Language = new CultureInfo("en"), Text = "Hello"},
            new TextLocalization {Language = new CultureInfo("sv"), Text = "Hej"},
            new TextLocalization {Language = new CultureInfo("de"), Text = "Hallo"},
            new TextLocalization {Language = new CultureInfo("fr"), Text = "Bonjour"}
        };
        private static readonly IList<TextLocalization> GreetingsExampleMinusGerman = new List<TextLocalization>
        {
            new TextLocalization {Language = new CultureInfo("en"), Text = "Hello"},
            new TextLocalization {Language = new CultureInfo("sv"), Text = "Hej"},
            new TextLocalization {Language = new CultureInfo("fr"), Text = "Bonjour"}
        };
        private static readonly IList<TextLocalization> GreetingsExamplePlusIndonesian = new List<TextLocalization>
        {
            new TextLocalization {Language = new CultureInfo("en"), Text = "Hello"},
            new TextLocalization {Language = new CultureInfo("sv"), Text = "Hej"},
            new TextLocalization {Language = new CultureInfo("de"), Text = "Hallo"},
            new TextLocalization {Language = new CultureInfo("fr"), Text = "Bonjour"},
            new TextLocalization {Language = new CultureInfo("id"), Text = "Halo"}
        };
        private static readonly IList<TextLocalization> AcceptExample = new List<TextLocalization>
        {
            new TextLocalization {Language = new CultureInfo("en"), Text = "I accept"},
            new TextLocalization {Language = new CultureInfo("sv"), Text = "jag accepterar"},
            new TextLocalization {Language = new CultureInfo("de"), Text = "Ich akzeptiere"},
            new TextLocalization {Language = new CultureInfo("fr"), Text = "J'accepte"}
        };

        [TestMethod()]
        public void Constructor_GivenBackupPath_GeneratesBackup()
        {
            //Arrange
            SelectLanguageFile("empty");
            File.Delete(BackupPath);

            //Act
            var efp = CreateNewExcelFileProvider(true);

            //Assert
            Assert.IsTrue(File.Exists(BackupPath));
        }

        [TestMethod()]
        public void Constructor_NotGivenBackupPath_DoesNotGenerateBackup()
        {
            //Arrange
            SelectLanguageFile("empty");
            File.Delete(BackupPath);

            //Act
            var efp = CreateNewExcelFileProvider(false);

            //Assert
            Assert.IsFalse(File.Exists(BackupPath));
        }

        [TestMethod()]
        public void Update_NullKey_ThrowsArgumentNullException()
        {
            //Arrange
            SelectLanguageFile("empty");
            var efp = CreateNewExcelFileProvider(false);
            IEnumerable<TextLocalization> localizations = new List<TextLocalization>
            {
                new TextLocalization {Language = new CultureInfo("en"), Text = "Hello"},
                new TextLocalization {Language = new CultureInfo("sv"), Text = "Hej"},
                new TextLocalization {Language = new CultureInfo("de"), Text = "Hallo"},
                new TextLocalization {Language = new CultureInfo("fr"), Text = "Bonjour"}
            };

            //Act //Assert
            Assert.ThrowsException<ArgumentNullException>(() => efp.Update(null, localizations));
        }

        [TestMethod()]
        public void GetDictionary_NormalUpdate_ReturnsUpdatedDict()
        {
            //Arrange
            SelectLanguageFile("empty");
            var efp = CreateNewExcelFileProvider(false);

            var key = "greeting";
            IEnumerable<TextLocalization> localizations = GreetingsExample;

            //Act
            efp.Update(key, localizations);
            var dict = efp.GetDictionary();

            //Assert
            Assert.IsTrue(dict.Keys.ToList().All(ContainingLanguages.Contains));
            Assert.AreEqual(ContainingLanguages.Count, dict.Count);
            VerifyDictionary(dict, key, localizations);
            Assert.AreEqual(1, dict[ContainingLanguages[0]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[1]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[2]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[3]].Count);
        }

        [TestMethod()]
        public void GetDictionary_NormalUpdateThenUpdateWithNullLocalizations_DictUpdatedOnce()
        {
            //Arrange
            SelectLanguageFile("empty");
            var efp = CreateNewExcelFileProvider(false);

            var key1 = "greeting";
            var key2 = "approving";
            IEnumerable<TextLocalization> localizations = GreetingsExample;

            //Act
            efp.Update(key1, localizations);
            efp.Update(key2, null);
            var dict = efp.GetDictionary();

            //Assert
            Assert.IsTrue(dict.Keys.ToList().All(ContainingLanguages.Contains));
            Assert.AreEqual(ContainingLanguages.Count, dict.Count);
            VerifyDictionary(dict, key1, localizations);
            Assert.AreEqual(1, dict[ContainingLanguages[0]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[1]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[2]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[3]].Count);
        }

        [TestMethod()]
        public void GetDictionary_NormalUpdateThenUpdateWithEmptyLocalizations_DictUpdatedOnce()
        {
            //Arrange
            SelectLanguageFile("empty");
            var efp = CreateNewExcelFileProvider(false);

            var key1 = "greeting";
            var key2 = "approving";
            IEnumerable<TextLocalization> localizations1 = GreetingsExample;
            IEnumerable<TextLocalization> localizations2 = new List<TextLocalization>();

            //Act
            efp.Update(key1, localizations1);
            efp.Update(key2, localizations2);
            var dict = efp.GetDictionary();

            //Assert
            Assert.IsTrue(dict.Keys.ToList().All(ContainingLanguages.Contains));
            Assert.AreEqual(ContainingLanguages.Count, dict.Count);
            VerifyDictionary(dict, key1, localizations1);
            Assert.AreEqual(1, dict[ContainingLanguages[0]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[1]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[2]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[3]].Count);
        }

        [TestMethod()]
        public void GetDictionary_TwoUpdatesOneSmaller_ReturnsAppropriatelyShapedDict()
        {
            //Arrange
            SelectLanguageFile("empty");
            var efp = CreateNewExcelFileProvider(false);

            var key1 = "greeting";
            var key2 = "approving";
            IEnumerable<TextLocalization> localizations1 = GreetingsExampleMinusGerman;
            IEnumerable<TextLocalization> localizations2 = AcceptExample;

            //Act
            efp.Update(key1, localizations1);
            efp.Update(key2, localizations2);
            var dict = efp.GetDictionary();

            //Assert
            Assert.IsTrue(dict.Keys.ToList().All(ContainingLanguages.Contains));
            Assert.AreEqual(ContainingLanguages.Count, dict.Count);
            VerifyDictionary(dict, key1, localizations1);
            VerifyDictionary(dict, key2, localizations2);
            Assert.AreEqual(2, dict[ContainingLanguages[0]].Count);
            Assert.AreEqual(2, dict[ContainingLanguages[1]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[2]].Count);
            Assert.AreEqual(2, dict[ContainingLanguages[3]].Count);
        }

        [TestMethod()]
        public void GetDictionary_TwoUpdatesOneBigger_ReturnsAppropriatelyShapedDict()
        {
            //Arrange
            SelectLanguageFile("empty");
            var efp = CreateNewExcelFileProvider(false);

            var key1 = "greeting";
            var key2 = "approving";
            IEnumerable<TextLocalization> localizations1 = GreetingsExamplePlusIndonesian;
            IEnumerable<TextLocalization> localizations2 = AcceptExample;
            IList<CultureInfo> containingLanguagesPlusID =
                (new List<CultureInfo>{new CultureInfo("id")}).Concat(ContainingLanguages).ToList();

            //Act
            efp.Update(key1, localizations1);
            efp.Update(key2, localizations2);
            var dict = efp.GetDictionary();

            //Assert
            Assert.IsTrue(dict.Keys.ToList().All(containingLanguagesPlusID.Contains));
            Assert.AreEqual(containingLanguagesPlusID.Count, dict.Count);
            VerifyDictionary(dict, key1, localizations1);
            VerifyDictionary(dict, key2, localizations2);
            Assert.AreEqual(1, dict[containingLanguagesPlusID[0]].Count);
            Assert.AreEqual(2, dict[containingLanguagesPlusID[1]].Count);
            Assert.AreEqual(2, dict[containingLanguagesPlusID[2]].Count);
            Assert.AreEqual(2, dict[containingLanguagesPlusID[3]].Count);
            Assert.AreEqual(2, dict[containingLanguagesPlusID[4]].Count);
        }

        [TestMethod()]
        public void GetDictionary_NoUpdate_ReturnDictHasEmptyInnerDicts()
        {
            //Arrange
            SelectLanguageFile("empty");
            var efp = CreateNewExcelFileProvider(false);

            //Act
            var dict = efp.GetDictionary();

            //Assert
            Assert.IsTrue(dict.Keys.ToList().All(ContainingLanguages.Contains));
            Assert.AreEqual(ContainingLanguages.Count, dict.Count);

            Assert.AreEqual(0, dict[ContainingLanguages[0]].Count);
            Assert.AreEqual(0, dict[ContainingLanguages[1]].Count);
            Assert.AreEqual(0, dict[ContainingLanguages[2]].Count);
            Assert.AreEqual(0, dict[ContainingLanguages[3]].Count);
        }

        [TestMethod()]
        public void LoadingFiles_NoLanguages_FileProviderIsInStateEmpty()
        {
            //Arrange
            SelectLanguageFile("no languages");

            //Act
            var efp = CreateNewExcelFileProvider(false);

            //Assert
            Assert.AreEqual(ProviderStatus.Empty, efp.Status);
        }

        [TestMethod()]
        public void LoadingFiles_AlmostFull_GetDictionaryReturnsAppropriateNumberOfEntries()
        {
            //Arrange
            SelectLanguageFile("almost full");
            var efp = CreateNewExcelFileProvider(false);

            //Act
            var dict = efp.GetDictionary();

            //Assert
            Assert.AreEqual(ContainingLanguages.Count, dict.Count);
            Assert.IsTrue(AllDictionariesAreSize(dict, NumberOfRowsInAlmostFull));
        }

        [TestMethod()]
        public void CancelInitialization_CallingMethod_PutsFileProviderInStateCancelled()
        {
            //Arrange
            ExcelFileProvider efp = new ExcelFileProvider(Path);

            //Act
            efp.CancelInitialization();
            while(efp.Status == ProviderStatus.CancellationInProgress)
            {
                Thread.Sleep(200);
            }

            //Assert
            Assert.AreEqual(ProviderStatus.CancellationComplete, efp.Status);
        }

        private void VerifyDictionary(Dictionary<CultureInfo, Dictionary<string, string>> dict, string expectedKey,
            IEnumerable<TextLocalization> textLocalizationsInput)
        {
            foreach (var textLocalization in textLocalizationsInput)
            {
                Assert.IsTrue(dict[textLocalization.Language].ContainsKey(expectedKey));
                Assert.IsTrue(dict[textLocalization.Language].ContainsValue(textLocalization.Text));
            }
        }

        private bool AllDictionariesAreSize(Dictionary<CultureInfo, Dictionary<string, string>> dict, int expectedSize)
        {
            foreach (var subDict in dict)
            {
                if (subDict.Value.Count != expectedSize)
                {
                    return false;
                }
            }

            return true;
        }

        private ExcelFileProvider CreateNewExcelFileProvider(bool withBackup)
        {
            ExcelFileProvider efp;
            if (withBackup)
            {
                efp = new ExcelFileProvider(Path, GlossaryTag, BackupPath);
            }
            else
            {
                efp = new ExcelFileProvider(Path, GlossaryTag);
            }

            //unlike AbstractLiteralProvider.Instance efp cannot be accessed in tests until it is initialized.
            while (efp.Status == ProviderStatus.InitializationInProgress)
            {
                Thread.Sleep(200);
            }

            return efp;
        }

        private void SelectLanguageFile(string versionOfLangueFile)
        {
            var fullNameOfLanguageFile = PathBeginningPart;

            switch (versionOfLangueFile)
            {
                case "empty":
                    fullNameOfLanguageFile += PathEmptyPart;
                    break;
                case "almost full":
                    fullNameOfLanguageFile += PathAlmostFullPart;
                    break;
                case "no languages":
                    fullNameOfLanguageFile += PathNoLanguages;
                    break;
                default:
                    Assert.Fail("SelectLanguageFile function was used incorrectly.");
                    return;
            }

            fullNameOfLanguageFile += PathEndingPart;

            File.Copy(fullNameOfLanguageFile, Path, true);
        }
    }
}