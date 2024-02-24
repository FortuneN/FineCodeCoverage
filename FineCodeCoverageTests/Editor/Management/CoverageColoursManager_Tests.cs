using AutoMoq;
using Castle.Core.Internal;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverageTests.Test_helpers;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;
using FineCodeCoverageTests.TestHelpers;

namespace FineCodeCoverageTests.Editor.Management
{
    internal class CoverageColoursManager_Tests
    {
        private IEnumerable<PropertyInfo> GetEditorFormatDefinitionProperties()
        {
            return typeof(CoverageColoursManager).GetProperties().Where(p => p.PropertyType == typeof(EditorFormatDefinition));
        }

        private IEnumerable<string> GetEditorFormatDefinitionNames()
        {
            return GetEditorFormatDefinitionProperties().Select(p => p.GetAttribute<NameAttribute>().Name);
        }

        [Test]
        public void Should_Export_IInitializable()
        {
            ExportsInitializable.Should_Export_IInitializable(typeof(CoverageColoursManager));
        }

        [Test]
        public void Should_Export_5_UserVisible_EditorFormatDefinitions()
        {
            var editorFormatDefinitionProperties = GetEditorFormatDefinitionProperties().ToList();
            
            Assert.That(editorFormatDefinitionProperties.Count, Is.EqualTo(5));
            editorFormatDefinitionProperties.ForEach(p =>
            {
                Assert.That(p.GetAttribute<ExportAttribute>(), Is.Not.Null);
                Assert.That(p.GetAttribute<NameAttribute>(), Is.Not.Null);
                Assert.That(p.GetAttribute<UserVisibleAttribute>().UserVisible, Is.True);
            });
        }

        [Test]
        public void Should_Listen_For_EditorFormatMap_Text_Changes_To_Markers_And_EditorFormatDefinitions()
        {
            var autoMoqer = new AutoMoqer();

            var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();
           
            var expectedListenFor = new List<string>
            {
                "Coverage Touched Area",
                "Coverage Not Touched Area",
                "Coverage Partially Touched Area",
            }.Concat(GetEditorFormatDefinitionNames()).OrderByDescending(v => v);
            
            autoMoqer.Verify<IEditorFormatMapTextSpecificListener>(
                editorFormatMapTextSpecificListener => editorFormatMapTextSpecificListener.ListenFor(
                    It.Is<List<string>>(listenedFor => expectedListenFor.SequenceEqual(listenedFor.OrderByDescending(v => v))), It.IsAny<Action>()
                ));
        }

        [Test]
        public void Should_Initialize_ICoverageFontAndColorsCategoryItemNamesManager_With_FCCEditorFormatDefinitionNames()
        {
            var autoMoqer = new AutoMoqer();

            autoMoqer.Create<CoverageColoursManager>();

            var fccEditorFormatDefinitionNames = (FCCEditorFormatDefinitionNames)autoMoqer.GetMock<ICoverageFontAndColorsCategoryItemNamesManager>()
                .Invocations.Where(i => i.Method.Name == nameof(ICoverageFontAndColorsCategoryItemNamesManager.Initialize)).Single().Arguments[0];
            var names = new List<string> {
                fccEditorFormatDefinitionNames.NewLines,
                fccEditorFormatDefinitionNames.Dirty,
                fccEditorFormatDefinitionNames.PartiallyCovered,
                fccEditorFormatDefinitionNames.Covered,
                fccEditorFormatDefinitionNames.NotCovered
            }.OrderByDescending(n => n).ToList();

            Assert.That(names.SequenceEqual(GetEditorFormatDefinitionNames().Distinct().OrderByDescending(n => n)), Is.True);
        }

        [Test]
        public void Should_Set_FontAndColorsInfosProvider_CoverageFontAndColorsCategoryItemNames_From_The_Manager()
        {
            var autoMoqer = new AutoMoqer();
            var coverageFontAndColorsCategoryItemNames = new Mock<ICoverageFontAndColorsCategoryItemNames>().Object;
            autoMoqer.Setup<ICoverageFontAndColorsCategoryItemNamesManager, ICoverageFontAndColorsCategoryItemNames>(
                coverageFontAndColorsCategoryItemNamesManager => coverageFontAndColorsCategoryItemNamesManager.CategoryItemNames)
                .Returns(coverageFontAndColorsCategoryItemNames);

            autoMoqer.Create<CoverageColoursManager>();

            var mockFontAndColorsInfosProvider = autoMoqer.GetMock<IFontAndColorsInfosProvider>();
            mockFontAndColorsInfosProvider.VerifySet(fontAndColorsInfosProvider => fontAndColorsInfosProvider.CoverageFontAndColorsCategoryItemNames = coverageFontAndColorsCategoryItemNames);
                               
        }

        [Test]
        public void Should_Delayed_Set_Classification_Type_Colors()
        {
            var autoMoqer = new AutoMoqer();
            var partialFontAndColorsInfo = new Mock<IFontAndColorsInfo>().Object;
            var partialTextFormattingRunProperties = TextFormattingRunProperties.CreateTextFormattingRunProperties().SetBold(true);
            var mockTextFormattingRunPropertiesFactory = autoMoqer.GetMock<ITextFormattingRunPropertiesFactory>();
            mockTextFormattingRunPropertiesFactory.Setup(textFormattingRunPropertiesFactory => textFormattingRunPropertiesFactory.Create(
                partialFontAndColorsInfo
                )).Returns(partialTextFormattingRunProperties);

            var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();
            autoMoqer.Setup<IFontAndColorsInfosProvider, Dictionary<DynamicCoverageType, IFontAndColorsInfo>>(
                fontAndColorsInfosProvider => fontAndColorsInfosProvider.GetFontAndColorsInfos()
            ).Returns(new Dictionary<DynamicCoverageType, IFontAndColorsInfo> { { DynamicCoverageType.Partial, partialFontAndColorsInfo } });

            var mockEditorFormatMapTextSpecificListener = autoMoqer.GetMock<IEditorFormatMapTextSpecificListener>();
            mockEditorFormatMapTextSpecificListener.Setup(efmtsl => efmtsl.PauseListeningWhenExecuting(It.IsAny<Action>())).Callback<Action>(action => action());
            var mockCoverageTextMarkerInitializeTiming = autoMoqer.GetMock<IDelayedMainThreadInvocation>();
            (mockCoverageTextMarkerInitializeTiming.Invocations[0].Arguments[0] as Action)();

            autoMoqer.Verify<ICoverageClassificationColourService>(
                coverageClassificationColourService => coverageClassificationColourService.SetCoverageColours(
                    It.IsAny<IEnumerable<ICoverageTypeColour>>()
                ),
                Times.Once()
            );
            
            var coverageTypeColours = autoMoqer.GetMock<ICoverageClassificationColourService>()
                .Invocations.GetMethodInvocationSingleArgument<IEnumerable<ICoverageTypeColour>>(nameof(ICoverageClassificationColourService.SetCoverageColours)).First().ToList();
            Assert.That(coverageTypeColours.Count, Is.EqualTo(1));
            var coverageTypeColour = coverageTypeColours[0];
            Assert.That(coverageTypeColour.CoverageType, Is.EqualTo(DynamicCoverageType.Partial));
            Assert.That(coverageTypeColour.TextFormattingRunProperties, Is.SameAs(partialTextFormattingRunProperties));
        }

        private void Should_Set_Classification_Type_Colors_When_Changes_Pausing_Listening_For_Changes(Action<AutoMoqer> changer)
        {
            var autoMoqer = new AutoMoqer();

            var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();
            var changedFontAndColorsInfos = new Dictionary<DynamicCoverageType, IFontAndColorsInfo>
            {
                { DynamicCoverageType.Covered, new Mock<IFontAndColorsInfo>().Object },
                { DynamicCoverageType.NotCovered, new Mock<IFontAndColorsInfo>().Object },
                { DynamicCoverageType.Partial, new Mock<IFontAndColorsInfo>().Object }
            };
            var coverageTypes = changedFontAndColorsInfos.Keys.ToList();
            autoMoqer.Setup<IFontAndColorsInfosProvider, Dictionary<DynamicCoverageType, IFontAndColorsInfo>>(
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
            mockEditorFormatMapTextSpecificListener.Setup(efmtsl => efmtsl.PauseListeningWhenExecuting(It.IsAny<Action>())).Callback<Action>(action => action());

            changer(autoMoqer);


            var coverageTypeColours = (autoMoqer.GetMock<ICoverageClassificationColourService>().Invocations[0].Arguments[0] as IEnumerable<ICoverageTypeColour>).ToList();
            Assert.That(coverageTypeColours.Count, Is.EqualTo(3));
            count = 0;
            foreach (var coverageTypeColour in coverageTypeColours)
            {
                Assert.That(coverageTypeColour.CoverageType, Is.EqualTo(coverageTypes[count]));
                Assert.That(coverageTypeColour.TextFormattingRunProperties, Is.SameAs(changedTextFormattingRunProperties[count]));
                count++;
            }

            mockEditorFormatMapTextSpecificListener.VerifyAll();
        }

        [Test]
        public void Should_Set_Classification_Type_Colors_When_EditorFormatMap_Changes_Pausing_Listening_For_Changes()
        {
            Should_Set_Classification_Type_Colors_When_Changes_Pausing_Listening_For_Changes(autoMoqer =>
            {
                var mockEditorFormatMapTextSpecificListener = autoMoqer.GetMock<IEditorFormatMapTextSpecificListener>();
                var listener = mockEditorFormatMapTextSpecificListener.Invocations[0].Arguments[1] as Action;
                listener();
            });
        }

        [Test]
        public void Should_Not_Set_Classification_Type_Colors_When_No_Changes()
        {
            var autoMoqer = new AutoMoqer();

            var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();
            autoMoqer.Setup<IFontAndColorsInfosProvider, Dictionary<DynamicCoverageType, IFontAndColorsInfo>>(
                fontAndColorsInfosProvider => fontAndColorsInfosProvider.GetChangedFontAndColorsInfos()
                           ).Returns(new Dictionary<DynamicCoverageType, IFontAndColorsInfo>());
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
        public void Should_Set_Classification_Type_Colours_When_CoverageFontAndColorsCategoryItemNamesManager_Changed()
        {
            Should_Set_Classification_Type_Colors_When_Changes_Pausing_Listening_For_Changes(autoMoqer =>
            {
                var mockCoverageFontAndColorsCategoryItemNamesManager = autoMoqer.GetMock<ICoverageFontAndColorsCategoryItemNamesManager>();
                mockCoverageFontAndColorsCategoryItemNamesManager.Raise(mgr => mgr.Changed += null, EventArgs.Empty);
            });
        }
    }
}