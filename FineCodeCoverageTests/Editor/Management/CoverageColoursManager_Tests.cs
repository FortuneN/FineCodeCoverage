using AutoMoq;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.TextManager.Interop;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;

namespace FineCodeCoverageTests.Editor.Management
{
    public class CoverageColoursManager_Tests
    {
        [Test]
        public void Should_Not_GetTextMarkerType_If_Should_Not()
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.Setup<IShouldAddCoverageMarkersLogic, bool>(shouldAddCoverageMarkersLogic => shouldAddCoverageMarkersLogic.ShouldAddCoverageMarkers()).Returns(false);

            var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();

            var guids = new List<string> { CoverageColoursManager.TouchedGuidString, CoverageColoursManager.PartiallyTouchedGuidString, CoverageColoursManager.NotTouchedGuidString };
            guids.ForEach(guid =>
            {
                var markerGuid = new Guid(guid);
                var success = coverageColoursManager.GetTextMarkerType(ref markerGuid, out var markerType);
                Assert.That(success, Is.EqualTo(0));
                Assert.That(markerType, Is.Null);
            });

        }

        [TestCase("Coverage Touched Area", CoverageColoursManager.TouchedGuidString)]
        [TestCase("Coverage Not Touched Area", CoverageColoursManager.NotTouchedGuidString)]
        [TestCase("Coverage Partially Touched Area", CoverageColoursManager.PartiallyTouchedGuidString)]
        public void Should_Get_CoverageTouchedArea_MarkerType_If_Should_Matching_Enterprise_Names(string name, string guidString)
        {
            var autoMoqer = new AutoMoqer();
            autoMoqer.Setup<IShouldAddCoverageMarkersLogic, bool>(shouldAddCoverageMarkersLogic => shouldAddCoverageMarkersLogic.ShouldAddCoverageMarkers()).Returns(true);

            var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();

            var guid = new Guid(guidString);
            var success = coverageColoursManager.GetTextMarkerType(ref guid, out var markerType);
            Assert.That(success, Is.EqualTo(0));

            var vsMergeableUIItem = markerType as IVsMergeableUIItem;
            success = vsMergeableUIItem.GetDisplayName(out var displayName);
            Assert.That(success, Is.EqualTo(0));
            Assert.That(displayName, Is.EqualTo(name));

            success = vsMergeableUIItem.GetCanonicalName(out var canonicalName);
            Assert.That(success, Is.EqualTo(0));
            Assert.That(canonicalName, Is.EqualTo(name));

            var vsHiColorItem = markerType as IVsHiColorItem;
            //0 is foreground, 1 is background, 2 is line color
            success = vsHiColorItem.GetColorData(0, out var foregroundColor);
            Assert.That(success, Is.EqualTo(0));
            success = vsHiColorItem.GetColorData(1, out var backgroundColor);
            Assert.That(success, Is.EqualTo(0));
        }

        [Test]
        public void Should_Listen_For_EditorFormatMap_Text_Changes_To_Markers()
        {
            var autoMoqer = new AutoMoqer();

            var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();

            autoMoqer.Verify<IEditorFormatMapTextSpecificListener>(
                editorFormatMapTextSpecificListener => editorFormatMapTextSpecificListener.ListenFor(
                    new List<string> { "Coverage Touched Area", "Coverage Not Touched Area", "Coverage Partially Touched Area" }, It.IsAny<Action>()
                ));
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Set_Classification_Type_Colors_When_Changes_Pausing_Listening_For_Changes(bool executePauseListeningWhenExecuting)
        {
            var autoMoqer = new AutoMoqer();

            var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();
            var changedFontAndColorsInfos = new Dictionary<CoverageType, IFontAndColorsInfo>
            {
                { CoverageType.Covered, new Mock<IFontAndColorsInfo>().Object },
                { CoverageType.NotCovered, new Mock<IFontAndColorsInfo>().Object },
                { CoverageType.Partial, new Mock<IFontAndColorsInfo>().Object }
            };
            var coverageTypes = changedFontAndColorsInfos.Keys.ToList();
            autoMoqer.Setup<IFontAndColorsInfosProvider, Dictionary<CoverageType, IFontAndColorsInfo>>(
                fontAndColorsInfosProvider => fontAndColorsInfosProvider.GetChangedFontAndColorsInfos()
            ).Returns(changedFontAndColorsInfos);
            var mockTextFormattingRunPropertiesFactory = autoMoqer.GetMock<ITextFormattingRunPropertiesFactory>();
            var changedTextFormattingRunProperties = new List<TextFormattingRunProperties>
            {
                TextFormattingRunProperties.CreateTextFormattingRunProperties(),
                TextFormattingRunProperties.CreateTextFormattingRunProperties().SetBold(true),
                TextFormattingRunProperties.CreateTextFormattingRunProperties().SetItalic(true)
            };
            var count = 0;
            foreach (var change in changedFontAndColorsInfos)
            {
                mockTextFormattingRunPropertiesFactory.Setup(
                    textFormattingRunPropertiesFactory => textFormattingRunPropertiesFactory.Create(change.Value)
                )
                .Returns(changedTextFormattingRunProperties[count]);
                count++;
            }

            var mockEditorFormatMapTextSpecificListener = autoMoqer.GetMock<IEditorFormatMapTextSpecificListener>();
            if (executePauseListeningWhenExecuting)
            {
                mockEditorFormatMapTextSpecificListener.Setup(efmtsl => efmtsl.PauseListeningWhenExecuting(It.IsAny<Action>())).Callback<Action>(action => action());
            }
            var listener = mockEditorFormatMapTextSpecificListener.Invocations[0].Arguments[1] as Action;
            listener();


            if (executePauseListeningWhenExecuting)
            {
                var coverageTypeColours = (autoMoqer.GetMock<ICoverageClassificationColourService>().Invocations[0].Arguments[0] as IEnumerable<ICoverageTypeColour>).ToList();
                Assert.That(coverageTypeColours.Count, Is.EqualTo(3));
                count = 0;
                foreach (var coverageTypeColour in coverageTypeColours)
                {
                    Assert.That(coverageTypeColour.CoverageType, Is.EqualTo(coverageTypes[count]));
                    Assert.That(coverageTypeColour.TextFormattingRunProperties, Is.SameAs(changedTextFormattingRunProperties[count]));
                    count++;
                }
            }
            else
            {
                autoMoqer.Verify<ICoverageClassificationColourService>(
                    coverageClassificationColourService => coverageClassificationColourService.SetCoverageColours(
                        It.IsAny<IEnumerable<ICoverageTypeColour>>()), Times.Never());
            }
        }

        [Test]
        public void Should_Not_Set_Classification_Type_Colors_When_No_Changes()
        {
            var autoMoqer = new AutoMoqer();

            var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();
            autoMoqer.Setup<IFontAndColorsInfosProvider, Dictionary<CoverageType, IFontAndColorsInfo>>(
                fontAndColorsInfosProvider => fontAndColorsInfosProvider.GetChangedFontAndColorsInfos()
                           ).Returns(new Dictionary<CoverageType, IFontAndColorsInfo>());
            var mockEditorFormatMapTextSpecificListener = autoMoqer.GetMock<IEditorFormatMapTextSpecificListener>();
            var listener = mockEditorFormatMapTextSpecificListener.Invocations[0].Arguments[1] as Action;
            listener();

            autoMoqer.Verify<ICoverageClassificationColourService>(
                coverageClassificationColourService => coverageClassificationColourService.SetCoverageColours(
                    It.IsAny<IEnumerable<ICoverageTypeColour>>()
                ),
                Times.Never()
            );
        }

        [Test]
        public void Should_Initially_Set_Classification_Type_Colors_If_Has_Not_Already_Set()
        {
            var autoMoqer = new AutoMoqer();

            var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();
            autoMoqer.Setup<IFontAndColorsInfosProvider, Dictionary<CoverageType, IFontAndColorsInfo>>(
                fontAndColorsInfosProvider => fontAndColorsInfosProvider.GetFontAndColorsInfos()
            ).Returns(new Dictionary<CoverageType, IFontAndColorsInfo> { { CoverageType.Partial, new Mock<IFontAndColorsInfo>().Object } });

            var mockEditorFormatMapTextSpecificListener = autoMoqer.GetMock<IEditorFormatMapTextSpecificListener>();
            mockEditorFormatMapTextSpecificListener.Setup(efmtsl => efmtsl.PauseListeningWhenExecuting(It.IsAny<Action>())).Callback<Action>(action => action());
            var mockCoverageTextMarkerInitializeTiming = autoMoqer.GetMock<ICoverageTextMarkerInitializeTiming>();
            (mockCoverageTextMarkerInitializeTiming.Invocations[0].Arguments[0] as ICoverageInitializable).Initialize();

            autoMoqer.Verify<ICoverageClassificationColourService>(
                coverageClassificationColourService => coverageClassificationColourService.SetCoverageColours(
                    It.IsAny<IEnumerable<ICoverageTypeColour>>()
                ),
                Times.Once()
            );
        }

        [TestCase(0, true)]
        [TestCase(1, true)]
        [TestCase(2, true)]
        [TestCase(3, false)]
        public void Should_RequireInitialization_If_Has_Not_Already_Set_All_From_Listening(int numChanges, bool requiresInitialization)
        {
            var changes = new Dictionary<CoverageType, IFontAndColorsInfo>();
            for (var i = 0; i < numChanges; i++)
            {
                changes.Add((CoverageType)i, new Mock<IFontAndColorsInfo>().Object);
            }
            var autoMoqer = new AutoMoqer();
            var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();
            autoMoqer.Setup<IFontAndColorsInfosProvider, Dictionary<CoverageType, IFontAndColorsInfo>>(
                    fontAndColorsInfosProvider => fontAndColorsInfosProvider.GetChangedFontAndColorsInfos()
            ).Returns(changes);

            var mockEditorFormatMapTextSpecificListener = autoMoqer.GetMock<IEditorFormatMapTextSpecificListener>();
            var listener = mockEditorFormatMapTextSpecificListener.Invocations[0].Arguments[1] as Action;
            listener();

            Assert.That(coverageColoursManager.RequiresInitialization, Is.EqualTo(requiresInitialization));
        }
    }
}