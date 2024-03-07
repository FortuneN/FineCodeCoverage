using FineCodeCoverage.Editor.Management;
using FineCodeCoverageTests.TestHelpers;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace FineCodeCoverageTests.Editor.Management
{
    internal class FontsAndColorsHelper_Tests
    {
        [TestCase(true)]
        [TestCase(false)]
        public async Task Should_Use_IVsFontAndColorStorage_Async(bool isBold)
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var mockVsFontAndColorStorage = new Mock<IVsFontAndColorStorage>();
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            mockVsFontAndColorStorage.Setup(vsFontsAndColorStorage => vsFontsAndColorStorage.GetItem("name", It.IsAny<ColorableItemInfo[]>()))
                .Callback<string, ColorableItemInfo[]>((_, colorableItemsInfos) =>
                {
                    var colorableItemInfo = new ColorableItemInfo
                    {
                        crBackground = (uint)System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.AliceBlue),
                        crForeground = (uint)System.Drawing.ColorTranslator.ToOle(System.Drawing.Color.Aqua),
                        dwFontFlags = isBold ? (uint)FONTFLAGS.FF_BOLD : (uint)FONTFLAGS.FF_DEFAULT
                    };
                    colorableItemsInfos[0] = colorableItemInfo;
                });
            serviceProvider.Setup(x => x.GetService(typeof(IVsFontAndColorStorage))).Returns(mockVsFontAndColorStorage.Object);
            var fontsAndColorsHelper = new FontsAndColorsHelper(serviceProvider.Object, new TestThreadHelper());
            var categoryGuid = Guid.NewGuid();
            var infos = await fontsAndColorsHelper.GetInfosAsync(categoryGuid, new List<string> { "name" });
            var info = infos[0];
            Assert.That(isBold, Is.EqualTo(isBold));
            Assert.That(info.ItemCoverageColours.Background, Is.EqualTo(System.Windows.Media.Colors.AliceBlue));
            Assert.That(info.ItemCoverageColours.Foreground, Is.EqualTo(System.Windows.Media.Colors.Aqua));
            var flags = (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS | __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS | __FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES);
            mockVsFontAndColorStorage.Verify(x => x.OpenCategory(ref categoryGuid, flags), Times.Once);
            mockVsFontAndColorStorage.Verify(x => x.CloseCategory(), Times.Once);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
        }

        [Test]
        public async Task Should_Return_Empty_When_OpenCategory_Fails_Async()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var mockVsFontAndColorStorage = new Mock<IVsFontAndColorStorage>();
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            mockVsFontAndColorStorage.Setup(vsFontAndColorStorage => vsFontAndColorStorage.OpenCategory(ref It.Ref<Guid>.IsAny, It.IsAny<uint>()))
                .Returns(VSConstants.E_FAIL);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            serviceProvider.Setup(x => x.GetService(typeof(IVsFontAndColorStorage))).Returns(mockVsFontAndColorStorage.Object);

            var fontsAndColorsHelper = new FontsAndColorsHelper(serviceProvider.Object, new TestThreadHelper());
            var infos = await fontsAndColorsHelper.GetInfosAsync(Guid.Empty, new List<string> { "name" });
            
            Assert.That(infos, Is.Empty);

        }

        [Test]
        public async Task Should_Not_Throw_Exception_When_GetItem_Fails_Async()
        {
            var serviceProvider = new Mock<IServiceProvider>();
            var mockVsFontAndColorStorage = new Mock<IVsFontAndColorStorage>();
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            mockVsFontAndColorStorage.Setup(vsFontAndColorStorage => vsFontAndColorStorage.GetItem("name", It.IsAny<ColorableItemInfo[]>()))
                .Returns(VSConstants.E_FAIL);
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            serviceProvider.Setup(x => x.GetService(typeof(IVsFontAndColorStorage))).Returns(mockVsFontAndColorStorage.Object);

            var fontsAndColorsHelper = new FontsAndColorsHelper(serviceProvider.Object, new TestThreadHelper());
            var infos = await fontsAndColorsHelper.GetInfosAsync(Guid.Empty, new List<string> { "name" });

            Assert.That(infos, Is.Empty);
        }
    }
}