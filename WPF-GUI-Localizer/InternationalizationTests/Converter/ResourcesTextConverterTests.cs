﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using Internationalization.Enum;
using Internationalization.LiteralProvider.Abstract;
using Internationalization.LiteralProvider.Interface;
using Internationalization.LiteralProvider.Resource;
using Internationalization.Model;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;

namespace Internationalization.Converter.Tests
{
    [TestClass()]
    public class ResourcesTextConverterTests
    {
        private abstract class MoqLiteralProviderSetupHelper : AbstractLiteralProvider
        {
            public static void Init(ILiteralProvider literalProvider)
            {
                Instance = literalProvider;
            }
        }

        [TestMethod()]
        public void Convert_Null_ReturnsEmptyString()
        {
            //Arrange
            var mockLP = new Mock<ILiteralProvider>();
            MoqLiteralProviderSetupHelper.Init(mockLP.Object);
            var converter = new ResourcesTextConverter();
            string resourceKey = null;

            //Act
            object converted = converter.Convert(
                resourceKey, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(string.Empty, convertedAsTargetType);
        }

        [TestMethod()]
        public void Convert_InappropriateType_ReturnsEmptyString()
        {
            //Arrange
            var resourceKey = new List<string> {"someKey"};
            var mockLP = new Mock<ResourceLiteralProvider> { CallBase = true };
            MoqLiteralProviderSetupHelper.Init(mockLP.Object);
            var converter = new ResourcesTextConverter();

            //Act
            object converted = converter.Convert(
                resourceKey, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(string.Empty, convertedAsTargetType);
        }

        [TestMethod()]
        public void Convert_EmptyString_ReturnsCorresponding()
        {
            //Arrange
            string resourceKey = "";
            string expectedTranslation = "Hello World!";
            var mockLP = new Mock<ResourceLiteralProvider> { CallBase = true };
            mockLP.Setup(resLP => resLP.GetGuiTranslationOfCurrentCulture(resourceKey))
                .Returns(expectedTranslation);
            MoqLiteralProviderSetupHelper.Init(mockLP.Object);
            var converter = new ResourcesTextConverter();

            //Act
            object converted = converter.Convert(
                resourceKey, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(expectedTranslation, convertedAsTargetType);
        }

        [TestMethod()]
        public void Convert_ExistingEntry_ReturnsCorresponding()
        {
            //Arrange
            string resourceKey = "someKey";
            string expectedTranslation = "Hello World!";
            var mockLP = new Mock<ResourceLiteralProvider>{ CallBase = true };
            mockLP.Setup(resLP => resLP.GetGuiTranslationOfCurrentCulture(resourceKey))
                .Returns(expectedTranslation);
            MoqLiteralProviderSetupHelper.Init(mockLP.Object);
            var converter = new ResourcesTextConverter();

            //Act
            object converted = converter.Convert(
                resourceKey, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(expectedTranslation, convertedAsTargetType);
        }

        [TestMethod()]
        public void Convert_NonExistingEntry_ReturnsEmptyString()
        {
            //Arrange
            string resourceKey = "someKey";
            var mockLP = new Mock<ResourceLiteralProvider> { CallBase = true };
            mockLP.Setup(resLP => resLP.GetGuiTranslationOfCurrentCulture(resourceKey))
                .Returns((string) null);
            MoqLiteralProviderSetupHelper.Init(mockLP.Object);
            var converter = new ResourcesTextConverter();

            //Act
            object converted = converter.Convert(
                resourceKey, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(string.Empty, convertedAsTargetType);
        }

        /// <summary>
        /// This test assumes not setting <see cref="AbstractLiteralProvider.Instance"/> does not cause endless loops!
        /// </summary>
        [TestMethod()]
        public void Convert_NoLiteralProvider_ReturnsEmptyString()
        {
            //Arrange
            string resourceKey = "someKey";
            var converter = new ResourcesTextConverter();

            //Act
            object converted = converter.Convert(
                resourceKey, typeof(string), null, CultureInfo.InvariantCulture);
            var convertedAsTargetType = converted as string;

            //Assert
            Assert.IsInstanceOfType(converted, typeof(string));
            Assert.AreEqual(string.Empty, convertedAsTargetType);
        }
    }
}