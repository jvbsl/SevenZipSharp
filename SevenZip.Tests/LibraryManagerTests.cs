﻿namespace SevenZip.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class LibraryManagerTests : TestBase
    {
        [Test]
        public void SetNonExistant7zDllLocationTest()
        {
            Assert.Throws<SevenZipLibraryException>(() => SevenZipLibraryManager.Instance.SetLibraryPath("null"));
        }

        [Test]
        public void CurrentLibraryFeaturesTest()
        {
            var features = SevenZipBase.CurrentLibraryFeatures;

            // Exercising more code paths...
            features = SevenZipLibraryManager.Instance.CurrentLibraryFeatures;

            Assert.IsTrue(features.HasFlag(LibraryFeature.ExtractAll));
            Assert.IsTrue(features.HasFlag(LibraryFeature.CompressAll));
            Assert.IsTrue(features.HasFlag(LibraryFeature.Modify));
        }
    }
}
