using System.Collections.Generic;
using System.Globalization;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Internationalization.Converter.Tests
{
    [TestClass]
    public class CultureInfoStringConverterTests
    {
        [TestMethod]
        public void Convert_NullCultureInfo_ReturnsEmptyString()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            CultureInfo language = null;

            //Act
            var converted = converter.Convert(
                language, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(string.Empty, convertedAsTargetType);
        }

        [TestMethod]
        public void Convert_InappropriateType_ReturnsEmptyString()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            var language = "I am a CultureInfo object";

            //Act
            var converted = converter.Convert(
                language, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(string.Empty, convertedAsTargetType);
        }

        [TestMethod]
        public void Convert_NormalCultureInfo_ReturnsCorrespondingString()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            var language = new CultureInfo("en-US");
            var expectedConverted = language.DisplayName + " (" + language.Name + ")";

            //Act
            var converted = converter.Convert(
                language, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(expectedConverted, convertedAsTargetType);
        }

        [TestMethod]
        public void Convert_InvariantCultureInfo_ReturnsCorrespondingString()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            var language = CultureInfo.InvariantCulture;
            var expectedConverted = language.DisplayName + " (" + language.Name + ")";

            //Act
            var converted = converter.Convert(
                language, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(expectedConverted, convertedAsTargetType);
        }

        [TestMethod]
        public void ConvertBack_NullString_ReturnsNullCultureInfo()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            string language = null;

            //Act
            var converted = converter.ConvertBack(
                language, typeof(CultureInfo), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as CultureInfo;

            //Assert
            Assert.AreEqual(null, convertedAsTargetType);
        }

        [TestMethod]
        public void ConvertBack_ValidString_ReturnsCorrespondingCultureInfo()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            var languageOriginal = new CultureInfo("en-US");
            var language = languageOriginal.DisplayName + " (" + languageOriginal.Name + ")";

            //Act
            var converted = converter.ConvertBack(
                language, typeof(CultureInfo), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as CultureInfo;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(CultureInfo));
            Assert.AreEqual(languageOriginal, convertedAsTargetType);
        }

        [TestMethod]
        public void ConvertBack_EmptyString_ReturnsNullCultureInfo()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            var language = string.Empty;

            //Act
            var converted = converter.ConvertBack(
                language, typeof(CultureInfo), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as CultureInfo;

            //Assert
            Assert.AreEqual(null, convertedAsTargetType);
        }

        [TestMethod]
        public void ConvertBack_InappropriateType_ReturnsNullCultureInfo()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            var language = new List<string> {"I am a string object"};

            //Act
            var converted = converter.ConvertBack(
                language, typeof(CultureInfo), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as CultureInfo;

            //Assert
            Assert.AreEqual(null, convertedAsTargetType);
        }

        [TestMethod]
        public void ConvertBack_InvariantString_ReturnsInvariantCultureInfo()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            var languageOriginal = CultureInfo.InvariantCulture;
            var language = languageOriginal.DisplayName + " (" + languageOriginal.Name + ")";

            //Act
            var converted = converter.ConvertBack(
                language, typeof(CultureInfo), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as CultureInfo;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(CultureInfo));
            Assert.AreEqual(languageOriginal, convertedAsTargetType);
        }

        [TestMethod]
        public void ConvertBack_InvalidString_ReturnsNull()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            var language = "Some name (hel-lo)";

            //Act
            var converted = converter.ConvertBack(
                language, typeof(CultureInfo), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as CultureInfo;

            //Assert
            Assert.AreEqual(null, convertedAsTargetType);
        }
    }
}