using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Globalization;

namespace Internationalization.Converter.Tests
{
    [TestClass()]
    public class CultureInfoStringConverterTests
    {
        [TestMethod()]
        public void Convert_NullCultureInfo_ReturnsEmptyString()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            CultureInfo language = null;

            //Act
            object converted = converter.Convert(
                language, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(string.Empty, convertedAsTargetType);
        }

        [TestMethod()]
        public void Convert_InappropriateType_ReturnsEmptyString()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            string language = "I am a CultureInfo object";

            //Act
            object converted = converter.Convert(
                language, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(string.Empty, convertedAsTargetType);
        }

        [TestMethod()]
        public void Convert_NormalCultureInfo_ReturnsCorrespondingString()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            var language = new CultureInfo("en-US");
            string expectedConverted = language.DisplayName + " (" + language.Name + ")";

            //Act
            object converted = converter.Convert(
                language, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(expectedConverted, convertedAsTargetType);
        }

        [TestMethod()]
        public void Convert_InvariantCultureInfo_ReturnsCorrespondingString()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            var language = CultureInfo.InvariantCulture;
            string expectedConverted = language.DisplayName + " (" + language.Name + ")";

            //Act
            object converted = converter.Convert(
                language, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(expectedConverted, convertedAsTargetType);
        }

        [TestMethod()]
        public void ConvertBack_NullString_ReturnsNullCultureInfo()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            string language = null;

            //Act
            object converted = converter.ConvertBack(
                language, typeof(CultureInfo), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as CultureInfo;

            //Assert
            Assert.AreEqual(null, convertedAsTargetType);
        }

        [TestMethod()]
        public void ConvertBack_ValidString_ReturnsCorrespondingCultureInfo()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            var languageOriginal = new CultureInfo("en-US");
            string language = languageOriginal.DisplayName + " (" + languageOriginal.Name + ")";

            //Act
            object converted = converter.ConvertBack(
                language, typeof(CultureInfo), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as CultureInfo;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(CultureInfo));
            Assert.AreEqual(languageOriginal, convertedAsTargetType);
        }

        [TestMethod()]
        public void ConvertBack_EmptyString_ReturnsNullCultureInfo()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            string language = string.Empty;

            //Act
            object converted = converter.ConvertBack(
                language, typeof(CultureInfo), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as CultureInfo;

            //Assert
            Assert.AreEqual(null, convertedAsTargetType);
        }

        [TestMethod()]
        public void ConvertBack_InappropriateType_ReturnsNullCultureInfo()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            var language = new List<string>{ "I am a string object" };

            //Act
            object converted = converter.ConvertBack(
                language, typeof(CultureInfo), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as CultureInfo;

            //Assert
            Assert.AreEqual(null, convertedAsTargetType);
        }

        [TestMethod()]
        public void ConvertBack_InvariantString_ReturnsInvariantCultureInfo()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            var languageOriginal = CultureInfo.InvariantCulture;
            string language = languageOriginal.DisplayName + " (" + languageOriginal.Name + ")";

            //Act
            object converted = converter.ConvertBack(
                language, typeof(CultureInfo), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as CultureInfo;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(CultureInfo));
            Assert.AreEqual(languageOriginal, convertedAsTargetType);
        }

        [TestMethod()]
        public void ConvertBack_InvalidString_ReturnsNull()
        {
            //Arrange
            var converter = new CultureInfoStringConverter();
            string language = "Some name (hel-lo)";

            //Act
            object converted = converter.ConvertBack(
                language, typeof(CultureInfo), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as CultureInfo;

            //Assert
            Assert.AreEqual(null, convertedAsTargetType);
        }
    }
}