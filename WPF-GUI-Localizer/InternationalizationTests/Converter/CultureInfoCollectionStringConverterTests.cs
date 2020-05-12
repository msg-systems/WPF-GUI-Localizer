using Microsoft.VisualStudio.TestTools.UnitTesting;
using Internationalization.Converter;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Internationalization.Converter.Tests
{
    [TestClass()]
    public class CultureInfoCollectionStringConverterTests
    {
        public CultureInfoCollectionStringConverter converter;

        [TestInitialize]
        public void Initialize()
        {
            var a = GlobalSettings.LibraryLoggerFactory;
            converter = new CultureInfoCollectionStringConverter();
        }
        
        [TestMethod()]
        public void Convert_NullList_ReturnsEmpty()
        {
            IEnumerable<CultureInfo> languages = null;

            object converted = converter.Convert(
                languages, typeof(IEnumerable<string>), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as IEnumerable<string>;

            Assert.IsInstanceOfType(converted, typeof(IEnumerable<string>));
            Assert.AreEqual(convertedAsTargetType?.Count(), 0);
        }

        [TestMethod()]
        public void Convert_EmptyList_ReturnsEmpty()
        {
            IEnumerable<CultureInfo> languages = new List<CultureInfo>();

            object converted = converter.Convert(
                languages, typeof(IEnumerable<string>), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as IEnumerable<string>;

            Assert.IsInstanceOfType(converted, typeof(IEnumerable<string>));
            Assert.AreEqual(convertedAsTargetType?.Count(), 0);
        }

        [TestMethod()]
        public void Convert_OneElement_ReturnsCorrectList()
        {
            var language = new CultureInfo("en-US");
            string expectedConverted = language.DisplayName + " (" + language.Name + ")";
            IEnumerable<CultureInfo> languages = new List<CultureInfo>{language};

            object converted = converter.Convert(
                languages, typeof(IEnumerable<string>), null, CultureInfo.InvariantCulture);
            IList<string> convertedAsTargetType = (converted as IEnumerable<string>)?.ToList();

            Assert.IsInstanceOfType(converted, typeof(IEnumerable<string>));
            Assert.AreEqual(convertedAsTargetType?.Count, 1);
            Assert.AreEqual(convertedAsTargetType?.First(), expectedConverted);
        }

        [TestMethod()]
        public void Convert_TwoElements_ReturnsCorrectList()
        {
            var language1 = new CultureInfo("it");
            string expectedConverted1 = language1.DisplayName + " (" + language1.Name + ")";
            var language2 = new CultureInfo("zh-Hans");
            string expectedConverted2 = language2.DisplayName + " (" + language2.Name + ")";
            IEnumerable<CultureInfo> languages = new List<CultureInfo> {language1, language2};

            object converted = converter.Convert(
                languages, typeof(IEnumerable<string>), null, CultureInfo.InvariantCulture);
            IList<string> convertedAsTargetType = (converted as IEnumerable<string>)?.ToList();

            Assert.IsInstanceOfType(converted, typeof(IEnumerable<string>));
            Assert.AreEqual(convertedAsTargetType?.Count, 2);
            Assert.AreEqual(convertedAsTargetType?[0], expectedConverted1);
            Assert.AreEqual(convertedAsTargetType?[1], expectedConverted2);
        }

        [TestMethod()]
        public void Convert_ListIncludesInvariant_ReturnsCorrectList()
        {
            var language1 = new CultureInfo("eo-001");
            string expectedConverted1 = language1.DisplayName + " (" + language1.Name + ")";
            var language2 = CultureInfo.InvariantCulture;
            string expectedConverted2 = language2.DisplayName + " (" + language2.Name + ")";
            IEnumerable<CultureInfo> languages = new List<CultureInfo> { language1, language2 };

            object converted = converter.Convert(
                languages, typeof(IEnumerable<string>), null, CultureInfo.InvariantCulture);
            IList<string> convertedAsTargetType = (converted as IEnumerable<string>)?.ToList();

            Assert.IsInstanceOfType(converted, typeof(IEnumerable<string>));
            Assert.AreEqual(convertedAsTargetType?.Count, 2);
            Assert.AreEqual(convertedAsTargetType?[0], expectedConverted1);
            Assert.AreEqual(convertedAsTargetType?[1], expectedConverted2);
        }

        [TestMethod()]
        public void ConvertBack_NullList_ReturnsEmpty()
        {
            IEnumerable<string> languages = null;

            object converted = converter.ConvertBack(
                languages, typeof(IEnumerable<CultureInfo>), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as IEnumerable<CultureInfo>;

            Assert.IsInstanceOfType(converted, typeof(IEnumerable<CultureInfo>));
            Assert.AreEqual(convertedAsTargetType?.Count(), 0);
        }

        [TestMethod()]
        public void ConvertBack_EmptyList_ReturnsEmpty()
        {
            IEnumerable<string> languages = new List<string>();

            object converted = converter.ConvertBack(
                languages, typeof(IEnumerable<CultureInfo>), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as IEnumerable<CultureInfo>;

            Assert.IsInstanceOfType(converted, typeof(IEnumerable<CultureInfo>));
            Assert.AreEqual(convertedAsTargetType?.Count(), 0);
        }

        [TestMethod()]
        public void ConvertBack_OneElement_ReturnsCorrectList()
        {
            var languageOriginal = new CultureInfo("en-US");
            string language = languageOriginal.DisplayName + " (" + languageOriginal.Name + ")";
            IEnumerable<string> languages = new List<string> { language };

            object converted = converter.ConvertBack(
                languages, typeof(IEnumerable<CultureInfo>), null, CultureInfo.InvariantCulture);
            IList<CultureInfo> convertedAsTargetType = (converted as IEnumerable<CultureInfo>)?.ToList();

            Assert.IsInstanceOfType(converted, typeof(IEnumerable<CultureInfo>));
            Assert.AreEqual(convertedAsTargetType?.Count, 1);
            Assert.AreEqual(convertedAsTargetType?.First(), languageOriginal);
        }

        [TestMethod()]
        public void ConvertBack_TwoElements_ReturnsCorrectList()
        {
            var languageOriginal1 = new CultureInfo("it");
            string language1 = languageOriginal1.DisplayName + " (" + languageOriginal1.Name + ")";
            var languageOriginal2 = new CultureInfo("zh-Hans");
            string language2 = languageOriginal2.DisplayName + " (" + languageOriginal2.Name + ")";
            IEnumerable<string> languages = new List<string> { language1, language2 };

            object converted = converter.ConvertBack(
                languages, typeof(IEnumerable<CultureInfo>), null, CultureInfo.InvariantCulture);
            IList<CultureInfo> convertedAsTargetType = (converted as IEnumerable<CultureInfo>)?.ToList();

            Assert.IsInstanceOfType(converted, typeof(IEnumerable<CultureInfo>));
            Assert.AreEqual(convertedAsTargetType?.Count, 2);
            Assert.AreEqual(convertedAsTargetType?[0], languageOriginal1);
            Assert.AreEqual(convertedAsTargetType?[1], languageOriginal2);
        }

        [TestMethod()]
        public void ConvertBack_ListIncludesInvariant_ReturnsCorrectList()
        {
            var languageOriginal1 = new CultureInfo("eo-001");
            string language1 = languageOriginal1.DisplayName + " (" + languageOriginal1.Name + ")";
            var languageOriginal2 = CultureInfo.InvariantCulture;
            string language2 = languageOriginal2.DisplayName + " (" + languageOriginal2.Name + ")";
            IEnumerable<string> languages = new List<string> { language1, language2 };

            object converted = converter.ConvertBack(
                languages, typeof(IEnumerable<CultureInfo>), null, CultureInfo.InvariantCulture);
            IList<CultureInfo> convertedAsTargetType = (converted as IEnumerable<CultureInfo>)?.ToList();

            Assert.IsInstanceOfType(converted, typeof(IEnumerable<CultureInfo>));
            Assert.AreEqual(convertedAsTargetType?.Count, 2);
            Assert.AreEqual(convertedAsTargetType?[0], languageOriginal1);
            Assert.AreEqual(convertedAsTargetType?[1], languageOriginal2);
        }

        [TestMethod()]
        public void ConvertBack_ListIncludesInvalid_LeavesOutInvalid()
        {
            var languageOriginal1 = new CultureInfo("chr-Cher-US");
            string language1 = languageOriginal1.DisplayName + " (" + languageOriginal1.Name + ")";
            string language2 = "Some name (hel-lo)";
            IEnumerable<string> languages = new List<string> { language1, language2 };
            
            object converted = converter.ConvertBack(
                languages, typeof(IEnumerable<CultureInfo>), null, CultureInfo.InvariantCulture);
            IList<CultureInfo> convertedAsTargetType = (converted as IEnumerable<CultureInfo>)?.ToList();

            Assert.IsInstanceOfType(converted, typeof(IEnumerable<CultureInfo>));
            Assert.AreEqual(convertedAsTargetType?.Count, 1);
            Assert.AreEqual(convertedAsTargetType?[0], languageOriginal1);
        }
    }
}