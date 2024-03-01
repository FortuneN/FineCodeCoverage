using AutoMoq;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Engine;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Settings;
using Moq;
using NUnit.Framework;
using System;

namespace FineCodeCoverageTests.Editor.DynamicCoverage
{
    internal class DynamicCoverageStore_Tests
    {
        [Test]
        public void Should_Add_Itself_As_EventAggregator_Listener()
        {
            var autoMoqer = new AutoMoqer();
            var dynamicCoverageStore = autoMoqer.Create<DynamicCoverageStore>();

            autoMoqer.Verify<IEventAggregator>(eventAggregator => eventAggregator.AddListener(dynamicCoverageStore,null));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Delete_WritableUserSettingsStore_Collection_When_NewCoverageLinesMessage(bool collectionExists)
        {
            var autoMoqer = new AutoMoqer();
            var mockWritableSettingsStore = new Mock<WritableSettingsStore>();
            mockWritableSettingsStore.Setup(writableSettingsStore => writableSettingsStore.CollectionExists("FCC.DynamicCoverageStore")).Returns(collectionExists);
            autoMoqer.Setup<IWritableUserSettingsStoreProvider, WritableSettingsStore>(
                writableUserSettingsStoreProvider => writableUserSettingsStoreProvider.Provide()).Returns(mockWritableSettingsStore.Object);
            
            var dynamicCoverageStore = autoMoqer.Create<DynamicCoverageStore>();

            dynamicCoverageStore.Handle(new NewCoverageLinesMessage());

            mockWritableSettingsStore.Verify(writableSettingsStore => writableSettingsStore.DeleteCollection("FCC.DynamicCoverageStore"), Times.Exactly(collectionExists ? 1 : 0));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_SaveSerializedCoverage_To_The_Store_Creating_Collection_If_Does_Not_Exist(bool collectionExists)
        {
            var autoMoqer = new AutoMoqer();
            var mockWritableSettingsStore = new Mock<WritableSettingsStore>();
            mockWritableSettingsStore.Setup(writableSettingsStore => writableSettingsStore.CollectionExists("FCC.DynamicCoverageStore")).Returns(collectionExists);
            autoMoqer.Setup<IWritableUserSettingsStoreProvider, WritableSettingsStore>(
                writableUserSettingsStoreProvider => writableUserSettingsStoreProvider.Provide()).Returns(mockWritableSettingsStore.Object);

            var dynamicCoverageStore = autoMoqer.Create<DynamicCoverageStore>();

            dynamicCoverageStore.SaveSerializedCoverage("filePath", "serialized");

            mockWritableSettingsStore.Verify(writableSettingsStore => writableSettingsStore.CreateCollection("FCC.DynamicCoverageStore"), Times.Exactly(collectionExists ? 0 : 1));
            mockWritableSettingsStore.Verify(writableSettingsStore => writableSettingsStore.SetString("FCC.DynamicCoverageStore", "filePath", "serialized"), Times.Once);
        }

        [Test]
        public void Should_Return_Null_For_GetSerializedCoverage_When_Collection_Does_Not_Exist()
        {
            var autoMoqer = new AutoMoqer();
            
            autoMoqer.Setup<IWritableUserSettingsStoreProvider, WritableSettingsStore>(
                               writableUserSettingsStoreProvider => writableUserSettingsStoreProvider.Provide()).Returns(new Mock<WritableSettingsStore>().Object);

            var dynamicCoverageStore = autoMoqer.Create<DynamicCoverageStore>();

            var serializedCoverage = dynamicCoverageStore.GetSerializedCoverage("filePath");

            Assert.IsNull(serializedCoverage);
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Return_From_Collection_When_Property_Exists(bool propertyExists)
        {
            var autoMoqer = new AutoMoqer();
            var mockWritableSettingsStore = new Mock<WritableSettingsStore>();
            mockWritableSettingsStore.Setup(writableSettingsStore => writableSettingsStore.CollectionExists("FCC.DynamicCoverageStore")).Returns(true);
            mockWritableSettingsStore.Setup(writableSettingsStore => writableSettingsStore.PropertyExists("FCC.DynamicCoverageStore", "filePath")).Returns(propertyExists);
            mockWritableSettingsStore.Setup(writableSettingsStore => writableSettingsStore.GetString("FCC.DynamicCoverageStore", "filePath")).Returns("serialized");
            autoMoqer.Setup<IWritableUserSettingsStoreProvider, WritableSettingsStore>(
                               writableUserSettingsStoreProvider => writableUserSettingsStoreProvider.Provide()).Returns(mockWritableSettingsStore.Object);

            var dynamicCoverageStore = autoMoqer.Create<DynamicCoverageStore>();

            var serializedCoverage = dynamicCoverageStore.GetSerializedCoverage("filePath");

            Assert.AreEqual(propertyExists ? "serialized" : null, serializedCoverage);
        }

        private void FileRename(
            Action<Mock<WritableSettingsStore>> setupWritableSettingsStore = null, 
            Action<Mock<WritableSettingsStore>> verifyWritableSettingsStore = null
        )
        {
            var autoMoqer = new AutoMoqer();
            var mockWritableSettingsStore = new Mock<WritableSettingsStore>();
            setupWritableSettingsStore?.Invoke(mockWritableSettingsStore);
            autoMoqer.Setup<IWritableUserSettingsStoreProvider, WritableSettingsStore>(
                              writableUserSettingsStoreProvider => writableUserSettingsStoreProvider.Provide()).Returns(mockWritableSettingsStore.Object);
            var mockFileRenameListener = autoMoqer.GetMock<IFileRenameListener>();
            mockFileRenameListener.Setup(fileRenameListener => fileRenameListener.ListenForFileRename(It.IsAny<Action<string, string>>()))
                .Callback<Action<string, string>>(action => action("oldFileName", "newFileName"));

            var dynamicCoverageStore = autoMoqer.Create<DynamicCoverageStore>();

            mockFileRenameListener.VerifyAll();
            mockWritableSettingsStore.VerifyAll();
            verifyWritableSettingsStore?.Invoke(mockWritableSettingsStore);
        }

        [Test]
        public void Should_Listen_For_File_Rename_And_Not_Throw_If_Collection_Does_Not_Exist()
        {
            FileRename();
            
        }

        [Test]
        public void Should_Not_Throw_When_Rename_File_Not_In_The_Store()
        {
            FileRename(mockWritableSettingsStore =>
            {
                mockWritableSettingsStore.Setup(writableSettingsStore => writableSettingsStore.CollectionExists("FCC.DynamicCoverageStore")).Returns(true);
                mockWritableSettingsStore.Setup(writableSettingsStore => writableSettingsStore.PropertyExists("FCC.DynamicCoverageStore", "oldFileName")).Returns(false);
            });
        }

        [Test]
        public void Should_Update_The_FileName_In_The_Store()
        {
            FileRename(mockWritableSettingsStore =>
            {
                mockWritableSettingsStore.Setup(writableSettingsStore => writableSettingsStore.CollectionExists("FCC.DynamicCoverageStore")).Returns(true);
                mockWritableSettingsStore.Setup(writableSettingsStore => writableSettingsStore.PropertyExists("FCC.DynamicCoverageStore", "oldFileName")).Returns(true);
                mockWritableSettingsStore.Setup(writableSettingsStore => writableSettingsStore.GetString("FCC.DynamicCoverageStore", "oldFileName")).Returns("serialized");
            }, mockWritableSettingsStore =>
            {
                mockWritableSettingsStore.Verify(writableSettingsStore => writableSettingsStore.SetString("FCC.DynamicCoverageStore", "newFileName", "serialized"));
                mockWritableSettingsStore.Verify(writableSettingsStore => writableSettingsStore.DeleteProperty("FCC.DynamicCoverageStore", "oldFileName"));
            });
        }

        [Test]
        public void Should_Remove_SerializedCoverage_From_Store()
        {
            var autoMoqer = new AutoMoqer();
            var mockWritableSettingsStore = new Mock<WritableSettingsStore>();
            autoMoqer.Setup<IWritableUserSettingsStoreProvider, WritableSettingsStore>(
                writableUserSettingsStoreProvider => writableUserSettingsStoreProvider.Provide()).Returns(mockWritableSettingsStore.Object);

            var dynamicCoverageStore = autoMoqer.Create<DynamicCoverageStore>();

            dynamicCoverageStore.RemoveSerializedCoverage("filePath");

            mockWritableSettingsStore.Verify(writableSettingsStore => writableSettingsStore.DeleteProperty("FCC.DynamicCoverageStore", "filePath"));
        }
    }
}
