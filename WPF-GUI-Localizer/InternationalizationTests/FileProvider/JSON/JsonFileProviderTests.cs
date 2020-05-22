using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using Internationalization.Enum;
using Internationalization.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Internationalization.FileProvider.JSON.Tests
{
    [TestClass]
    public class JsonFileProviderTests
    {
        private const int NumberOfEntriesInSomeFile = 5;

        //properties of path language file
        private const string PathBeginningPart = @"TestResources\JsonTestResources\Language_File";
        private const string PathEndingPart = ".json";
        private const string PathEmptyPart = "_empty";
        private const string PathSomePart = "_some";

        private const string PathNoLanguagesPart = "_no_languages";

        //parameter used in constructor
        private const string Path = PathBeginningPart + PathEndingPart;

        //properties of language file
        private static readonly IList<CultureInfo> ContainingLanguages = new List<CultureInfo>
            {new CultureInfo("en"), new CultureInfo("sv"), new CultureInfo("de"), new CultureInfo("fr")};

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

        [TestMethod]
        public void Update_NullKey_ThrowsArgumentNullException()
        {
            //Arrange
            SelectLanguageFile("empty");
            var jfp = new JsonFileProvider(Path);
            IEnumerable<TextLocalization> localizations = GreetingsExample;

            //Act //Assert
            Assert.ThrowsException<ArgumentNullException>(() => jfp.Update(null, localizations));
        }

        [TestMethod]
        public void GetDictionary_NormalUpdate_ReturnsUpdatedDict()
        {
            //Arrange
            SelectLanguageFile("empty");
            var jfp = new JsonFileProvider(Path);

            var key = "greeting";
            IEnumerable<TextLocalization> localizations = GreetingsExample;

            //Act
            jfp.Update(key, localizations);
            var dict = jfp.GetDictionary();

            //Assert
            Assert.IsTrue(dict.Keys.ToList().All(ContainingLanguages.Contains));
            Assert.AreEqual(ContainingLanguages.Count, dict.Count);
            VerifyDictionary(dict, key, localizations);
            Assert.AreEqual(1, dict[ContainingLanguages[0]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[1]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[2]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[3]].Count);
        }

        [TestMethod]
        public void GetDictionary_NormalUpdateThenUpdateWithNullLocalizations_DictUpdatedOnce()
        {
            //Arrange
            SelectLanguageFile("empty");
            var jfp = new JsonFileProvider(Path);

            var key1 = "greeting";
            var key2 = "approving";
            IEnumerable<TextLocalization> localizations = GreetingsExample;

            //Act
            jfp.Update(key1, localizations);
            jfp.Update(key2, null);
            var dict = jfp.GetDictionary();

            //Assert
            Assert.IsTrue(dict.Keys.ToList().All(ContainingLanguages.Contains));
            Assert.AreEqual(ContainingLanguages.Count, dict.Count);
            VerifyDictionary(dict, key1, localizations);
            Assert.AreEqual(1, dict[ContainingLanguages[0]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[1]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[2]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[3]].Count);
        }

        [TestMethod]
        public void GetDictionary_NormalUpdateThenUpdateWithEmptyLocalizations_DictUpdatedOnce()
        {
            //Arrange
            SelectLanguageFile("empty");
            var Jfp = new JsonFileProvider(Path);

            var key1 = "greeting";
            var key2 = "approving";
            IEnumerable<TextLocalization> localizations1 = GreetingsExample;
            IEnumerable<TextLocalization> localizations2 = new List<TextLocalization>();

            //Act
            Jfp.Update(key1, localizations1);
            Jfp.Update(key2, localizations2);
            var dict = Jfp.GetDictionary();

            //Assert
            Assert.IsTrue(dict.Keys.ToList().All(ContainingLanguages.Contains));
            Assert.AreEqual(ContainingLanguages.Count, dict.Count);
            VerifyDictionary(dict, key1, localizations1);
            Assert.AreEqual(1, dict[ContainingLanguages[0]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[1]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[2]].Count);
            Assert.AreEqual(1, dict[ContainingLanguages[3]].Count);
        }

        [TestMethod]
        public void GetDictionary_TwoUpdatesOneSmaller_ReturnsAppropriatelyShapedDict()
        {
            //Arrange
            SelectLanguageFile("empty");
            var jfp = new JsonFileProvider(Path);

            var key1 = "greeting";
            var key2 = "approving";
            IEnumerable<TextLocalization> localizations1 = GreetingsExampleMinusGerman;
            IEnumerable<TextLocalization> localizations2 = AcceptExample;

            //Act
            jfp.Update(key1, localizations1);
            jfp.Update(key2, localizations2);
            var dict = jfp.GetDictionary();

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

        [TestMethod]
        public void GetDictionary_TwoUpdatesOneBigger_ReturnsAppropriatelyShapedDict()
        {
            //Arrange
            SelectLanguageFile("empty");
            var jfp = new JsonFileProvider(Path);

            var key1 = "greeting";
            var key2 = "approving";
            IEnumerable<TextLocalization> localizations1 = GreetingsExamplePlusIndonesian;
            IEnumerable<TextLocalization> localizations2 = AcceptExample;
            IList<CultureInfo> containingLanguagesPlusID =
                new List<CultureInfo> {new CultureInfo("id")}.Concat(ContainingLanguages).ToList();

            //Act
            jfp.Update(key1, localizations1);
            jfp.Update(key2, localizations2);
            var dict = jfp.GetDictionary();

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

        [TestMethod]
        public void GetDictionary_NoUpdate_ReturnDictHasEmptyInnerDicts()
        {
            //Arrange
            SelectLanguageFile("empty");
            var jfp = new JsonFileProvider(Path);

            //Act
            var dict = jfp.GetDictionary();

            //Assert
            Assert.IsTrue(dict.Keys.ToList().All(ContainingLanguages.Contains));
            Assert.AreEqual(ContainingLanguages.Count, dict.Count);

            Assert.AreEqual(0, dict[ContainingLanguages[0]].Count);
            Assert.AreEqual(0, dict[ContainingLanguages[1]].Count);
            Assert.AreEqual(0, dict[ContainingLanguages[2]].Count);
            Assert.AreEqual(0, dict[ContainingLanguages[3]].Count);
        }

        [TestMethod]
        public void LoadingFiles_NoLanguages_FileProviderIsInStateEmpty()
        {
            //Arrange
            SelectLanguageFile("no languages");

            //Act
            var efp = new JsonFileProvider(Path);

            //Assert
            Assert.AreEqual(ProviderStatus.Empty, efp.Status);
        }

        [TestMethod]
        public void LoadingFiles_Some_GetDictionaryReturnsAppropriateNumberOfEntries()
        {
            //Arrange
            SelectLanguageFile("some");
            var efp = new JsonFileProvider(Path);

            //Act
            var dict = efp.GetDictionary();

            //Assert
            Assert.AreEqual(ContainingLanguages.Count, dict.Count);
            Assert.IsTrue(AllDictionariesAreSize(dict, NumberOfEntriesInSomeFile));
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

        private void SelectLanguageFile(string versionOfLangueFile)
        {
            var fullNameOfLanguageFile = PathBeginningPart;

            switch (versionOfLangueFile)
            {
                case "empty":
                    fullNameOfLanguageFile += PathEmptyPart;
                    break;
                case "some":
                    fullNameOfLanguageFile += PathSomePart;
                    break;
                case "no languages":
                    fullNameOfLanguageFile += PathNoLanguagesPart;
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