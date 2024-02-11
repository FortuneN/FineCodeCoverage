using AutoMoq;
using FineCodeCoverage.Editor.Management;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Text.Formatting;
using Microsoft.VisualStudio.Utilities;
using Moq;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;
using System.Reflection;

namespace FineCodeCoverageTests.Editor.Management
{
    internal class CapturingClassificationTypeRegistryService : IClassificationTypeRegistryService
    {
        public Dictionary<string, IClassificationType> ClassificationTypes { get; set; } = new Dictionary<string, IClassificationType>();
        public IClassificationType CreateClassificationType(string type, IEnumerable<IClassificationType> baseTypes)
        {
            throw new System.NotImplementedException();
        }

        public ILayeredClassificationType CreateClassificationType(ClassificationLayer layer, string type, IEnumerable<IClassificationType> baseTypes)
        {
            throw new System.NotImplementedException();
        }

        public IClassificationType CreateTransientClassificationType(IEnumerable<IClassificationType> baseTypes)
        {
            throw new System.NotImplementedException();
        }

        public IClassificationType CreateTransientClassificationType(params IClassificationType[] baseTypes)
        {
            throw new System.NotImplementedException();
        }

        public IClassificationType GetClassificationType(string type)
        {
            var classificationType = new Mock<IClassificationType>().Object;
            ClassificationTypes.Add(type, classificationType);
            return classificationType;
        }

        public ILayeredClassificationType GetClassificationType(ClassificationLayer layer, string type)
        {
            throw new System.NotImplementedException();
        }
    }
    internal class CoverageClassificationTypeService_Tests
    {
        [Test]
        public void Should_Export_ClassificationTypeDefinitions_For_The_Types_Requested_From_The_ClassificationTypeRegistryService()
        {
            var autoMoqer = new AutoMoqer();
            var mockClassificationTypeRegistryService = autoMoqer.GetMock<IClassificationTypeRegistryService>();
            var mockClassificationFormatMapService = autoMoqer.GetMock<IClassificationFormatMapService>();
            mockClassificationFormatMapService.Setup(
                classificationFormatMapService => classificationFormatMapService.GetClassificationFormatMap("text").CurrentPriorityOrder
            ).Returns(new ReadOnlyCollection<IClassificationType>(new List<IClassificationType> { new Mock<IClassificationType>().Object }));

            autoMoqer.Create<CoverageClassificationTypeService>();

            var classificationTypeDefinitionProperties = typeof(CoverageClassificationTypeService).GetProperties().Where(p => p.PropertyType == typeof(ClassificationTypeDefinition));
            Assert.That(classificationTypeDefinitionProperties.Count(), Is.EqualTo(3));
            var names = new List<string>();
            foreach (var classificationTypeDefinitionProperty in classificationTypeDefinitionProperties)
            {
                var exportAttribute = classificationTypeDefinitionProperty.GetCustomAttribute<ExportAttribute>();
                Assert.That(exportAttribute, Is.Not.Null);
                var name = classificationTypeDefinitionProperty.GetCustomAttribute<NameAttribute>().Name;
                mockClassificationTypeRegistryService.Verify(classificationTypeRegistryService => classificationTypeRegistryService.GetClassificationType(name));
                names.Add(name);
            }
            Assert.That(names.Distinct(), Is.EquivalentTo(new List<string> { CoverageClassificationTypeService.FCCNotCoveredClassificationTypeName, CoverageClassificationTypeService.FCCCoveredClassificationTypeName, CoverageClassificationTypeService.FCCPartiallyCoveredClassificationTypeName }));
        }

        [Test]
        public void Should_Correspond()
        {
            var autoMoqer = new AutoMoqer();
            var classificationTypeRegistryService = new CapturingClassificationTypeRegistryService();
            autoMoqer.SetInstance<IClassificationTypeRegistryService>(classificationTypeRegistryService);
            
            var mockClassificationFormatMapService = autoMoqer.GetMock<IClassificationFormatMapService>();
            var mockClassificationFormatMap = new Mock<IClassificationFormatMap>();
            mockClassificationFormatMap.SetupGet(classificationFormatMap => classificationFormatMap.CurrentPriorityOrder)
                .Returns(new ReadOnlyCollection<IClassificationType>(new List<IClassificationType> { new Mock<IClassificationType>().Object }));
            mockClassificationFormatMapService.Setup(
                classificationFormatMapService => classificationFormatMapService.GetClassificationFormatMap("text")
            ).Returns(mockClassificationFormatMap.Object);

            var coverageClassificationTypeService = autoMoqer.Create<CoverageClassificationTypeService>();
            foreach(var coverageType in Enum.GetValues(typeof(CoverageType)).Cast<CoverageType>())
            {
                var editorFormatDefinition = coverageClassificationTypeService.GetEditorFormatDefinitionName(coverageType);
                var classificationType = classificationTypeRegistryService.ClassificationTypes[editorFormatDefinition];
                Assert.That(classificationType, Is.SameAs(coverageClassificationTypeService.GetClassificationType(coverageType)));
                var mockCoverageTypeColour = new Mock<ICoverageTypeColour>();
                mockCoverageTypeColour.SetupGet(coverageTypeColour => coverageTypeColour.CoverageType).Returns(coverageType);
                coverageClassificationTypeService.SetCoverageColours(new List<ICoverageTypeColour>() { mockCoverageTypeColour.Object });
                mockClassificationFormatMap.Verify(
                    classificationFormatMap => classificationFormatMap.AddExplicitTextProperties(
                        classificationType, 
                        It.IsAny<TextFormattingRunProperties>(),
                        It.IsAny<IClassificationType>()));
            }
        }

        [TestCase(true)]
        [TestCase(false)]
        public void Should_Prioriitize_Clasifications_In_The_ClassificationFormatMap_Batch_Updating_If_Not_In_Batch_Update(bool isInBatchUpdate)
        {
            var autoMoqer = new AutoMoqer();
            var mockClassificationTypeRegistryService = autoMoqer.GetMock<IClassificationTypeRegistryService>();
            var mockClassificationFormatMapService = autoMoqer.GetMock<IClassificationFormatMapService>();
            var currentPriorityOrder = new List<IClassificationType> { new Mock<IClassificationType>().Object, null, new Mock<IClassificationType>().Object };
            var mockClassificationFormatMap = new Mock<IClassificationFormatMap>();
            mockClassificationFormatMap.SetupGet(classificationFormatMap => classificationFormatMap.IsInBatchUpdate).Returns(isInBatchUpdate);
            mockClassificationFormatMapService.Setup(
                classificationFormatMapService => classificationFormatMapService.GetClassificationFormatMap("text")
            ).Returns(mockClassificationFormatMap.Object);
            mockClassificationFormatMap.SetupGet(
                classificationFormatMap => classificationFormatMap.CurrentPriorityOrder
            ).Returns(new ReadOnlyCollection<IClassificationType>(currentPriorityOrder));
            

            var coverageClassificationTypeService = autoMoqer.Create<CoverageClassificationTypeService>();
            var textFormattingRunProperties = TextFormattingRunProperties.CreateTextFormattingRunProperties().SetBold(true);
            List<ICoverageTypeColour> coverageTypeColours = new List<ICoverageTypeColour>
            {
                CreateCoverageTypeColour(CoverageType.Covered, textFormattingRunProperties)
            };
            coverageClassificationTypeService.SetCoverageColours(coverageTypeColours);

            mockClassificationFormatMap.Verify(
                classificationFormatMap => classificationFormatMap.AddExplicitTextProperties(
                        It.IsAny<IClassificationType>(), 
                        textFormattingRunProperties,
                        currentPriorityOrder[2]
                ));

            mockClassificationFormatMap.Verify(classificationFormatMap => classificationFormatMap.BeginBatchUpdate(), Times.Exactly(isInBatchUpdate ? 0 : 1));
            mockClassificationFormatMap.Verify(classificationFormatMap => classificationFormatMap.EndBatchUpdate(), Times.Exactly(isInBatchUpdate ? 0 : 1));
        }
        private static ICoverageTypeColour CreateCoverageTypeColour(CoverageType coverageType, TextFormattingRunProperties textFormattingRunProperties)
        {
            var mockCoverageTypeColour = new Mock<ICoverageTypeColour>();
            mockCoverageTypeColour.SetupGet(coverageTypeColour => coverageTypeColour.CoverageType).Returns(coverageType);
            mockCoverageTypeColour.SetupGet(coverageTypeColour => coverageTypeColour.TextFormattingRunProperties).Returns(textFormattingRunProperties);
            return mockCoverageTypeColour.Object;
        }
    }
}