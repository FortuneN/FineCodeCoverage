using AutoMoq;
using Castle.Core.Internal;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;

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
                coverageFontAndColorsCategoryItemNamesManager => coverageFontAndColorsCategoryItemNamesManager.ItemNames)
                .Returns(coverageFontAndColorsCategoryItemNames);

            autoMoqer.Create<CoverageColoursManager>();

            var mockFontAndColorsInfosProvider = autoMoqer.GetMock<IFontAndColorsInfosProvider>();
            mockFontAndColorsInfosProvider.VerifySet(fontAndColorsInfosProvider => fontAndColorsInfosProvider.CoverageFontAndColorsCategoryItemNames = coverageFontAndColorsCategoryItemNames);
                               
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Set_Classification_Type_Colors_When_Changes_Pausing_Listening_For_Changes(bool executePauseListeningWhenExecuting)
        {
            throw new System.NotImplementedException();
            //var autoMoqer = new AutoMoqer();

            //var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();
            //var changedFontAndColorsInfos = new Dictionary<CoverageType, IFontAndColorsInfo>
            //{
            //    { CoverageType.Covered, new Mock<IFontAndColorsInfo>().Object },
            //    { CoverageType.NotCovered, new Mock<IFontAndColorsInfo>().Object },
            //    { CoverageType.Partial, new Mock<IFontAndColorsInfo>().Object }
            //};
            //var coverageTypes = changedFontAndColorsInfos.Keys.ToList();
            //autoMoqer.Setup<IFontAndColorsInfosProvider, Dictionary<CoverageType, IFontAndColorsInfo>>(
            //    fontAndColorsInfosProvider => fontAndColorsInfosProvider.GetChangedFontAndColorsInfos()
            //).Returns(changedFontAndColorsInfos);
            //var mockTextFormattingRunPropertiesFactory = autoMoqer.GetMock<ITextFormattingRunPropertiesFactory>();
            //var changedTextFormattingRunProperties = new List<TextFormattingRunProperties>
            //{
            //    TextFormattingRunProperties.CreateTextFormattingRunProperties(),
            //    TextFormattingRunProperties.CreateTextFormattingRunProperties().SetBold(true),
            //    TextFormattingRunProperties.CreateTextFormattingRunProperties().SetItalic(true)
            //};
            //var count = 0;
            //foreach (var change in changedFontAndColorsInfos)
            //{
            //    mockTextFormattingRunPropertiesFactory.Setup(
            //        textFormattingRunPropertiesFactory => textFormattingRunPropertiesFactory.Create(change.Value)
            //    )
            //    .Returns(changedTextFormattingRunProperties[count]);
            //    count++;
            //}

            //var mockEditorFormatMapTextSpecificListener = autoMoqer.GetMock<IEditorFormatMapTextSpecificListener>();
            //if (executePauseListeningWhenExecuting)
            //{
            //    mockEditorFormatMapTextSpecificListener.Setup(efmtsl => efmtsl.PauseListeningWhenExecuting(It.IsAny<Action>())).Callback<Action>(action => action());
            //}
            //var listener = mockEditorFormatMapTextSpecificListener.Invocations[0].Arguments[1] as Action;
            //listener();


            //if (executePauseListeningWhenExecuting)
            //{
            //    var coverageTypeColours = (autoMoqer.GetMock<ICoverageClassificationColourService>().Invocations[0].Arguments[0] as IEnumerable<ICoverageTypeColour>).ToList();
            //    Assert.That(coverageTypeColours.Count, Is.EqualTo(3));
            //    count = 0;
            //    foreach (var coverageTypeColour in coverageTypeColours)
            //    {
            //        Assert.That(coverageTypeColour.CoverageType, Is.EqualTo(coverageTypes[count]));
            //        Assert.That(coverageTypeColour.TextFormattingRunProperties, Is.SameAs(changedTextFormattingRunProperties[count]));
            //        count++;
            //    }
            //}
            //else
            //{
            //    autoMoqer.Verify<ICoverageClassificationColourService>(
            //        coverageClassificationColourService => coverageClassificationColourService.SetCoverageColours(
            //            It.IsAny<IEnumerable<ICoverageTypeColour>>()), Times.Never());
            //}
        }

        [Test]
        public void Should_Not_Set_Classification_Type_Colors_When_No_Changes()
        {
            throw new System.NotImplementedException();
            //var autoMoqer = new AutoMoqer();

            //var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();
            //autoMoqer.Setup<IFontAndColorsInfosProvider, Dictionary<CoverageType, IFontAndColorsInfo>>(
            //    fontAndColorsInfosProvider => fontAndColorsInfosProvider.GetChangedFontAndColorsInfos()
            //               ).Returns(new Dictionary<CoverageType, IFontAndColorsInfo>());
            //var mockEditorFormatMapTextSpecificListener = autoMoqer.GetMock<IEditorFormatMapTextSpecificListener>();
            //var listener = mockEditorFormatMapTextSpecificListener.Invocations[0].Arguments[1] as Action;
            //listener();

            //autoMoqer.Verify<ICoverageClassificationColourService>(
            //    coverageClassificationColourService => coverageClassificationColourService.SetCoverageColours(
            //        It.IsAny<IEnumerable<ICoverageTypeColour>>()
            //    ),
            //    Times.Never()
            //);
        }

        [Test]
        public void Should_Initially_Set_Classification_Type_Colors_If_Has_Not_Already_Set()
        {
            throw new System.NotImplementedException();
            //var autoMoqer = new AutoMoqer();

            //var coverageColoursManager = autoMoqer.Create<CoverageColoursManager>();
            //autoMoqer.Setup<IFontAndColorsInfosProvider, Dictionary<CoverageType, IFontAndColorsInfo>>(
            //    fontAndColorsInfosProvider => fontAndColorsInfosProvider.GetFontAndColorsInfos()
            //).Returns(new Dictionary<CoverageType, IFontAndColorsInfo> { { CoverageType.Partial, new Mock<IFontAndColorsInfo>().Object } });

            //var mockEditorFormatMapTextSpecificListener = autoMoqer.GetMock<IEditorFormatMapTextSpecificListener>();
            //mockEditorFormatMapTextSpecificListener.Setup(efmtsl => efmtsl.PauseListeningWhenExecuting(It.IsAny<Action>())).Callback<Action>(action => action());
            //var mockCoverageTextMarkerInitializeTiming = autoMoqer.GetMock<ICoverageTextMarkerInitializeTiming>();
            //(mockCoverageTextMarkerInitializeTiming.Invocations[0].Arguments[0] as ICoverageInitializable).Initialize();

            //autoMoqer.Verify<ICoverageClassificationColourService>(
            //    coverageClassificationColourService => coverageClassificationColourService.SetCoverageColours(
            //        It.IsAny<IEnumerable<ICoverageTypeColour>>()
            //    ),
            //    Times.Once()
            //);
        }

        
    }
}