﻿using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Internationalization.Converter.Tests
{
    [TestClass()]
    public class CultureInfoCollectionStringConverterTests
    {
        [TestMethod()]
        public void Convert_Null_ReturnsEmpty()
        {
            //Arrange
            var converter = new CultureInfoCollectionStringConverter();
            IEnumerable<CultureInfo> languages = null;

            //Act
            object converted = converter.Convert(
                languages, typeof(IEnumerable<string>), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as IEnumerable<string>;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(IEnumerable<string>));
            Assert.AreEqual(0,convertedAsTargetType?.Count());
        }

        [TestMethod()]
        public void Convert_InappropriateType_ReturnsEmpty()
        {
            //Arrange
            var converter = new CultureInfoCollectionStringConverter();
            IEnumerable<string> languages = new List<string> {"I am a CultureInfo object"};

            //Act
            object converted = converter.Convert(
                languages, typeof(IEnumerable<string>), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as IEnumerable<string>;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(IEnumerable<string>));
            Assert.AreEqual(0, convertedAsTargetType?.Count());
        }

        [TestMethod()]
        public void Convert_EmptyList_ReturnsEmpty()
        {
            //Arrange
            var converter = new CultureInfoCollectionStringConverter();
            IEnumerable<CultureInfo> languages = new List<CultureInfo>();

            //Act
            object converted = converter.Convert(
                languages, typeof(IEnumerable<string>), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as IEnumerable<string>;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(IEnumerable<string>));
            Assert.AreEqual(0, convertedAsTargetType?.Count());
        }

        [TestMethod()]
        public void Convert_OneElement_ReturnsCorrespondingList()
        {
            //Arrange
            var converter = new CultureInfoCollectionStringConverter();
            var language = new CultureInfo("en-US");
            string expectedConverted = language.DisplayName + " (" + language.Name + ")";
            IEnumerable<CultureInfo> languages = new List<CultureInfo>{language};

            //Act
            object converted = converter.Convert(
                languages, typeof(IEnumerable<string>), null, CultureInfo.InvariantCulture);
            IList<string> convertedAsTargetType = (converted as IEnumerable<string>)?.ToList();

            //Assert
            Assert.IsInstanceOfType(converted, typeof(IEnumerable<string>));
            Assert.AreEqual(1, convertedAsTargetType?.Count);
            Assert.AreEqual(expectedConverted, convertedAsTargetType?.First());
        }

        [TestMethod()]
        public void Convert_TwoElements_ReturnsCorrespondingList()
        {
            //Arrange
            var converter = new CultureInfoCollectionStringConverter();
            var language1 = new CultureInfo("it");
            string expectedConverted1 = language1.DisplayName + " (" + language1.Name + ")";
            var language2 = new CultureInfo("zh-Hans");
            string expectedConverted2 = language2.DisplayName + " (" + language2.Name + ")";
            IEnumerable<CultureInfo> languages = new List<CultureInfo> {language1, language2};

            //Act
            object converted = converter.Convert(
                languages, typeof(IEnumerable<string>), null, CultureInfo.InvariantCulture);
            IList<string> convertedAsTargetType = (converted as IEnumerable<string>)?.ToList();

            //Assert
            Assert.IsInstanceOfType(converted, typeof(IEnumerable<string>));
            Assert.AreEqual(2, convertedAsTargetType?.Count);
            Assert.AreEqual(expectedConverted1, convertedAsTargetType?[0]);
            Assert.AreEqual(expectedConverted2, convertedAsTargetType?[1]);
        }

        [TestMethod()]
        public void Convert_ListIncludesInvariant_ReturnsCorrespondingList()
        {
            //Arrange
            var converter = new CultureInfoCollectionStringConverter();
            var language1 = new CultureInfo("eo-001");
            string expectedConverted1 = language1.DisplayName + " (" + language1.Name + ")";
            var language2 = CultureInfo.InvariantCulture;
            string expectedConverted2 = language2.DisplayName + " (" + language2.Name + ")";
            IEnumerable<CultureInfo> languages = new List<CultureInfo> { language1, language2 };

            //Act
            object converted = converter.Convert(
                languages, typeof(IEnumerable<string>), null, CultureInfo.InvariantCulture);
            IList<string> convertedAsTargetType = (converted as IEnumerable<string>)?.ToList();

            //Assert
            Assert.IsInstanceOfType(converted, typeof(IEnumerable<string>));
            Assert.AreEqual(2, convertedAsTargetType?.Count);
            Assert.AreEqual(expectedConverted1, convertedAsTargetType?[0]);
            Assert.AreEqual(expectedConverted2, convertedAsTargetType?[1]);
        }

        [TestMethod()]
        public void ConvertBack_NullList_ReturnsEmpty()
        {
            //Arrange
            var converter = new CultureInfoCollectionStringConverter();
            IEnumerable<string> languages = null;

            //Act
            object converted = converter.ConvertBack(
                languages, typeof(IEnumerable<CultureInfo>), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as IEnumerable<CultureInfo>;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(IEnumerable<CultureInfo>));
            Assert.AreEqual(0, convertedAsTargetType?.Count());
        }

        [TestMethod()]
        public void ConvertBack_InappropriateType_ReturnsEmpty()
        {
            //Arrange
            var converter = new CultureInfoCollectionStringConverter();
            IEnumerable<List<string>> languages = new List<List<string>> { new List<string>{"I am a string object"} };

            //Act
            object converted = converter.ConvertBack(
                languages, typeof(IEnumerable<CultureInfo>), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as IEnumerable<CultureInfo>;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(IEnumerable<CultureInfo>));
            Assert.AreEqual(0, convertedAsTargetType?.Count());
        }

        [TestMethod()]
        public void ConvertBack_EmptyList_ReturnsEmpty()
        {
            //Arrange
            var converter = new CultureInfoCollectionStringConverter();
            IEnumerable<string> languages = new List<string>();

            //Act
            object converted = converter.ConvertBack(
                languages, typeof(IEnumerable<CultureInfo>), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as IEnumerable<CultureInfo>;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(IEnumerable<CultureInfo>));
            Assert.AreEqual(0, convertedAsTargetType?.Count());
        }

        [TestMethod()]
        public void ConvertBack_OneElement_ReturnsCorrespondingList()
        {
            //Arrange
            var converter = new CultureInfoCollectionStringConverter();
            var languageOriginal = new CultureInfo("en-US");
            string language = languageOriginal.DisplayName + " (" + languageOriginal.Name + ")";
            IEnumerable<string> languages = new List<string> { language };

            //Act
            object converted = converter.ConvertBack(
                languages, typeof(IEnumerable<CultureInfo>), null, CultureInfo.InvariantCulture);
            IList<CultureInfo> convertedAsTargetType = (converted as IEnumerable<CultureInfo>)?.ToList();

            //Assert
            Assert.IsInstanceOfType(converted, typeof(IEnumerable<CultureInfo>));
            Assert.AreEqual(1, convertedAsTargetType?.Count);
            Assert.AreEqual(languageOriginal, convertedAsTargetType?.First());
        }

        [TestMethod()]
        public void ConvertBack_TwoElements_ReturnsCorrespondingList()
        {
            //Arrange
            var converter = new CultureInfoCollectionStringConverter();
            var languageOriginal1 = new CultureInfo("it");
            string language1 = languageOriginal1.DisplayName + " (" + languageOriginal1.Name + ")";
            var languageOriginal2 = new CultureInfo("zh-Hans");
            string language2 = languageOriginal2.DisplayName + " (" + languageOriginal2.Name + ")";
            IEnumerable<string> languages = new List<string> { language1, language2 };

            //Act
            object converted = converter.ConvertBack(
                languages, typeof(IEnumerable<CultureInfo>), null, CultureInfo.InvariantCulture);
            IList<CultureInfo> convertedAsTargetType = (converted as IEnumerable<CultureInfo>)?.ToList();

            //Assert
            Assert.IsInstanceOfType(converted, typeof(IEnumerable<CultureInfo>));
            Assert.AreEqual(2, convertedAsTargetType?.Count);
            Assert.AreEqual(languageOriginal1, convertedAsTargetType?[0]);
            Assert.AreEqual(languageOriginal2, convertedAsTargetType?[1]);
        }

        [TestMethod()]
        public void ConvertBack_ListIncludesInvariant_ReturnsCorrespondingList()
        {
            //Arrange
            var converter = new CultureInfoCollectionStringConverter();
            var languageOriginal1 = new CultureInfo("eo-001");
            string language1 = languageOriginal1.DisplayName + " (" + languageOriginal1.Name + ")";
            var languageOriginal2 = CultureInfo.InvariantCulture;
            string language2 = languageOriginal2.DisplayName + " (" + languageOriginal2.Name + ")";
            IEnumerable<string> languages = new List<string> { language1, language2 };

            //Act
            object converted = converter.ConvertBack(
                languages, typeof(IEnumerable<CultureInfo>), null, CultureInfo.InvariantCulture);
            IList<CultureInfo> convertedAsTargetType = (converted as IEnumerable<CultureInfo>)?.ToList();

            //Assert
            Assert.IsInstanceOfType(converted, typeof(IEnumerable<CultureInfo>));
            Assert.AreEqual(2, convertedAsTargetType?.Count);
            Assert.AreEqual(languageOriginal1, convertedAsTargetType?[0]);
            Assert.AreEqual(languageOriginal2, convertedAsTargetType?[1]);
        }

        [TestMethod()]
        public void ConvertBack_ListIncludesInvalid_LeavesOutInvalid()
        {
            //Arrange
            var converter = new CultureInfoCollectionStringConverter();
            var languageOriginal1 = new CultureInfo("chr-Cher-US");
            string language1 = languageOriginal1.DisplayName + " (" + languageOriginal1.Name + ")";
            string language2 = "Some name (hel-lo)";
            IEnumerable<string> languages = new List<string> { language1, language2 };
            
            //Act
            object converted = converter.ConvertBack(
                languages, typeof(IEnumerable<CultureInfo>), null, CultureInfo.InvariantCulture);
            IList<CultureInfo> convertedAsTargetType = (converted as IEnumerable<CultureInfo>)?.ToList();

            //Assert
            Assert.IsInstanceOfType(converted, typeof(IEnumerable<CultureInfo>));
            Assert.AreEqual(1, convertedAsTargetType?.Count);
            Assert.AreEqual(languageOriginal1, convertedAsTargetType?[0]);
        }
    }
}