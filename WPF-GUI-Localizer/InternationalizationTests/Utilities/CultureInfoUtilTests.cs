using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Internationalization.Utilities.Tests
{
    [TestClass]
    public class CultureInfoUtilTests
    {
        private static readonly Dictionary<CultureInfo, Dictionary<string, string>> DictionaryExample =
            new Dictionary<CultureInfo, Dictionary<string, string>>
            {
                {
                    new CultureInfo("en"), new Dictionary<string, string>
                    {
                        {"greeting", "Hello"},
                        {"accept", "I accept"}
                    }
                },
                {
                    new CultureInfo("sv"), new Dictionary<string, string>
                    {
                        {"greeting", "Hej"},
                        {"accept", "jag accepterar"}
                    }
                },
                {
                    new CultureInfo("de"), new Dictionary<string, string>
                    {
                        {"greeting", "Hallo"},
                        {"accept", "Ich akzeptiere"}
                    }
                }
            };

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetCultureInfo_NullString_ThrowsArgumentNullException(bool withBrackets)
        {
            //Arrange
            string culture = null;

            //Act //Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                CultureInfoUtil.GetCultureInfo(culture, false));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetCultureInfo_EmptyString_ThrowsCultureNotFoundException(bool withBrackets)
        {
            //Arrange
            var culture = string.Empty;

            //Act //Assert
            Assert.ThrowsException<CultureNotFoundException>(() =>
                CultureInfoUtil.GetCultureInfo(culture, false));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetCultureInfo_StringIsNotValidCulture_ThrowsCultureNotFoundException(bool withBrackets)
        {
            //Arrange
            var culture = withBrackets ? "(llllll)" : "llllll";

            //Act //Assert
            Assert.ThrowsException<CultureNotFoundException>(() =>
                CultureInfoUtil.GetCultureInfo(culture, false));
        }

        [TestMethod]
        public void GetCultureInfo_ValidCultureWithBracketsNotMatchingCall_ThrowsCultureNotFoundException()
        {
            //Arrange
            var culture = new CultureInfo("de-DE");
            var cultureString = $"({culture.Name})";

            //Act //Assert
            Assert.ThrowsException<CultureNotFoundException>(() =>
                CultureInfoUtil.GetCultureInfo(cultureString, false));
        }

        [TestMethod]
        public void GetCultureInfo_ValidCultureWithoutBracketsNotMatchingCall_ReturnsCorrespondingCultureInfo()
        {
            //Arrange
            var culture = new CultureInfo("de-DE");
            var cultureString = culture.Name;

            //Act
            var returnCulture = CultureInfoUtil.GetCultureInfo(cultureString, true);

            //Assert
            Assert.AreEqual(culture, returnCulture);
        }

        [TestMethod]
        public void GetCultureInfo_ValidCultureWithBracketsMatchingCall_ReturnsCorrespondingCultureInfo()
        {
            //Arrange
            var culture = new CultureInfo("de-DE");
            var cultureString = $"({culture.Name})";

            //Act
            var returnCulture = CultureInfoUtil.GetCultureInfo(cultureString, true);

            //Assert
            Assert.AreEqual(culture, returnCulture);
        }

        [TestMethod]
        public void GetCultureInfo_ValidCultureWithoutBracketsMatchingCall_ReturnsCorrespondingCultureInfo()
        {
            //Arrange
            var culture = new CultureInfo("de-DE");
            var cultureString = culture.Name;

            //Act
            var returnCulture = CultureInfoUtil.GetCultureInfo(cultureString, true);

            //Assert
            Assert.AreEqual(culture, returnCulture);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetCultureInfoOrDefault_NullString_ReturnsNull(bool withBrackets)
        {
            //Arrange
            string culture = null;

            //Act
            var returnCulture = CultureInfoUtil.GetCultureInfoOrDefault(culture, true);

            //Assert
            Assert.AreEqual(null, returnCulture);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetCultureInfoOrDefault_EmptyString_ReturnsNull(bool withBrackets)
        {
            //Arrange
            var culture = string.Empty;

            //Act
            var returnCulture = CultureInfoUtil.GetCultureInfoOrDefault(culture, true);

            //Assert
            Assert.AreEqual(null, returnCulture);
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void GetCultureInfoOrDefault_StringIsNotValidCulture_ReturnsNull(bool withBrackets)
        {
            //Arrange
            var culture = withBrackets ? "(llllll)" : "llllll";

            //Act
            var returnCulture = CultureInfoUtil.GetCultureInfoOrDefault(culture, true);

            //Assert
            Assert.AreEqual(null, returnCulture);
        }

        [TestMethod]
        public void GetCultureInfoOrDefault_ValidCultureWithBracketsNotMatchingCall_ReturnsNull()
        {
            //Arrange
            var culture = new CultureInfo("de-DE");
            var cultureString = $"({culture.Name})";

            //Act
            var returnCulture = CultureInfoUtil.GetCultureInfoOrDefault(cultureString, false);

            //Assert
            Assert.AreEqual(null, returnCulture);
        }

        [TestMethod]
        public void GetCultureInfoOrDefault_ValidCultureWithoutBracketsNotMatchingCall_ReturnsCorrespondingCultureInfo()
        {
            //Arrange
            var culture = new CultureInfo("de-DE");
            var cultureString = culture.Name;

            //Act
            var returnCulture = CultureInfoUtil.GetCultureInfoOrDefault(cultureString, true);

            //Assert
            Assert.AreEqual(culture, returnCulture);
        }

        [TestMethod]
        public void GetCultureInfoOrDefault_ValidCultureWithBracketsMatchingCall_ReturnsCorrespondingCultureInfo()
        {
            //Arrange
            var culture = new CultureInfo("de-DE");
            var cultureString = $"({culture.Name})";

            //Act
            var returnCulture = CultureInfoUtil.GetCultureInfoOrDefault(cultureString, true);

            //Assert
            Assert.AreEqual(culture, returnCulture);
        }

        [TestMethod]
        public void GetCultureInfoOrDefault_ValidCultureWithoutBracketsMatchingCall_ReturnsCorrespondingCultureInfo()
        {
            //Arrange
            var culture = new CultureInfo("de-DE");
            var cultureString = culture.Name;

            //Act
            var returnCulture = CultureInfoUtil.GetCultureInfoOrDefault(cultureString, true);

            //Assert
            Assert.AreEqual(culture, returnCulture);
        }

        [TestMethod]
        public void TryGetLanguageDict_AnyParameterNull_ThrowsArgumentNullException()
        {
            //Arrange
            var dictionary = DictionaryExample;
            var targetCulture = new CultureInfo("en-UK");
            var inputCulture = new CultureInfo("en-US");
            var key = "key";
            var useExact = false;

            //Act //Assert
            Assert.ThrowsException<ArgumentNullException>(() =>
                CultureInfoUtil.GetLanguageDictValueOrDefault(null, targetCulture, key, inputCulture, useExact));
            Assert.ThrowsException<ArgumentNullException>(() =>
                CultureInfoUtil.GetLanguageDictValueOrDefault(dictionary, null, key, inputCulture, useExact));
            Assert.ThrowsException<ArgumentNullException>(() =>
                CultureInfoUtil.GetLanguageDictValueOrDefault(dictionary, targetCulture, null, inputCulture, useExact));
            Assert.ThrowsException<ArgumentNullException>(() =>
                CultureInfoUtil.GetLanguageDictValueOrDefault(dictionary, targetCulture, key, null, useExact));
        }

        [TestMethod]
        [DataRow(true)]
        [DataRow(false)]
        public void TryGetLanguageDict_TargetIsInDictionary_ReturnsCorrespondingTranslation(bool useExact)
        {
            //Arrange
            var dictionary = DictionaryExample;
            var targetCulture = new CultureInfo("en");
            var inputCulture = new CultureInfo("id-ID");
            var key = "accept";
            var expectedTranslation = DictionaryExample[targetCulture][key];

            //Act
            var returnTranslation =
                CultureInfoUtil.GetLanguageDictValueOrDefault(dictionary, targetCulture, key, inputCulture, useExact);

            //Assert
            Assert.AreEqual(expectedTranslation, returnTranslation);
        }

        [TestMethod]
        public void TryGetLanguageDict_TargetParentIsInDictionaryWithoutExact_ReturnsParentTranslation()
        {
            //Arrange
            var dictionary = DictionaryExample;
            var targetCulture = new CultureInfo("en-US");
            var parentOfTarget = targetCulture.Parent;
            var inputCulture = new CultureInfo("id-ID");
            var key = "accept";
            var useExact = false;
            var expectedTranslation = DictionaryExample[parentOfTarget][key];

            //Act
            var returnTranslation =
                CultureInfoUtil.GetLanguageDictValueOrDefault(dictionary, targetCulture, key, inputCulture, useExact);

            //Assert
            Assert.AreEqual(expectedTranslation, returnTranslation);
        }

        [TestMethod]
        public void TryGetLanguageDict_TargetParentIsInDictionaryWithExact_ReturnsNull()
        {
            //Arrange
            var dictionary = DictionaryExample;
            var targetCulture = new CultureInfo("en-US");
            var inputCulture = new CultureInfo("id-ID");
            var key = "accept";
            var useExact = true;

            //Act
            var returnTranslation =
                CultureInfoUtil.GetLanguageDictValueOrDefault(dictionary, targetCulture, key, inputCulture, useExact);

            //Assert
            Assert.AreEqual(null, returnTranslation);
        }

        [TestMethod]
        public void TryGetLanguageDict_NoMatchesInDictionaryWithoutExact_ReturnsInputTranslation()
        {
            //Arrange
            var dictionary = DictionaryExample;
            var targetCulture = new CultureInfo("fr");
            var inputCulture = new CultureInfo("de");
            var key = "accept";
            var useExact = false;
            var expectedTranslation = DictionaryExample[inputCulture][key];

            //Act
            var returnTranslation =
                CultureInfoUtil.GetLanguageDictValueOrDefault(dictionary, targetCulture, key, inputCulture, useExact);

            //Assert
            Assert.AreEqual(expectedTranslation, returnTranslation);
        }

        [TestMethod]
        public void TryGetLanguageDict_NoMatchesInDictionaryAlAllWithoutExact_ReturnsNull()
        {
            //Arrange
            var dictionary = DictionaryExample;
            var targetCulture = new CultureInfo("fr");
            var inputCulture = new CultureInfo("id-ID");
            var key = "accept";
            var useExact = false;

            //Act
            var returnTranslation =
                CultureInfoUtil.GetLanguageDictValueOrDefault(dictionary, targetCulture, key, inputCulture, useExact);

            //Assert
            Assert.AreEqual(null, returnTranslation);
        }
    }
}