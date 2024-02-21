using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace FineCodeCoverage.Editor.Management
{
    [Export(typeof(ICoverageTypeService))]
    [Export(typeof(ICoverageColoursEditorFormatMapNames))]
    [Export(typeof(ICoverageClassificationColourService))]
    internal class CoverageClassificationTypeService : 
        ICoverageClassificationColourService, ICoverageColoursEditorFormatMapNames, ICoverageTypeService
    {
        public const string FCCCoveredClassificationTypeName = "FCCCovered";
        public const string FCCNotCoveredClassificationTypeName = "FCCNotCovered";
        public const string FCCPartiallyCoveredClassificationTypeName = "FCCPartial";
        public const string FCCDirtyClassificationTypeName = "FCCDirty";
        public const string FCCNewLineClassificationTypeName = "FCCNewLine";

        private readonly IClassificationFormatMap classificationFormatMap;
        private readonly ReadOnlyDictionary<DynamicCoverageType, IClassificationType> classificationTypes;
        private readonly IClassificationType highestPriorityClassificationType;

        [ExcludeFromCodeCoverage]
        [Export]
        [Name(FCCNotCoveredClassificationTypeName)]
        public ClassificationTypeDefinition FCCNotCoveredTypeDefinition { get; set; }

        [ExcludeFromCodeCoverage]
        [Export]
        [Name(FCCCoveredClassificationTypeName)]
        public ClassificationTypeDefinition FCCCoveredTypeDefinition { get; set; }

        [ExcludeFromCodeCoverage]
        [Export]
        [Name(FCCPartiallyCoveredClassificationTypeName)]
        public ClassificationTypeDefinition FCCPartiallyCoveredTypeDefinition { get; set; }

        [ExcludeFromCodeCoverage]
        [Export]
        [Name(FCCDirtyClassificationTypeName)]
        public ClassificationTypeDefinition FCCDirtyTypeDefinition { get; set; }

        [ExcludeFromCodeCoverage]
        [Export]
        [Name(FCCNewLineClassificationTypeName)]
        public ClassificationTypeDefinition FCCNewLineTypeDefinition { get; set; }

        [ImportingConstructor]
        public CoverageClassificationTypeService(
            IClassificationFormatMapService classificationFormatMapService,
            IClassificationTypeRegistryService classificationTypeRegistryService
        )
        {
            classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("text");
            highestPriorityClassificationType = classificationFormatMap.CurrentPriorityOrder.Where(ct => ct != null).Last();

            var notCoveredClassificationType = classificationTypeRegistryService.GetClassificationType(FCCNotCoveredClassificationTypeName);
            var coveredClassificationType = classificationTypeRegistryService.GetClassificationType(FCCCoveredClassificationTypeName);
            var partiallyCoveredClassificationType = classificationTypeRegistryService.GetClassificationType(FCCPartiallyCoveredClassificationTypeName);
            var dirtyClassificationType = classificationTypeRegistryService.GetClassificationType(FCCDirtyClassificationTypeName);
            var newCodeClassificationType = classificationTypeRegistryService.GetClassificationType(FCCNewLineClassificationTypeName);

            classificationTypes = new ReadOnlyDictionary<DynamicCoverageType, IClassificationType>(
                new Dictionary<DynamicCoverageType, IClassificationType>
                {
                    { DynamicCoverageType.Covered, coveredClassificationType },
                    { DynamicCoverageType.NotCovered, notCoveredClassificationType },
                    { DynamicCoverageType.Partial, partiallyCoveredClassificationType },
                    {DynamicCoverageType.Dirty, dirtyClassificationType },
                    {DynamicCoverageType.NewLine, newCodeClassificationType }
                });
        }

        private void BatchUpdateIfRequired(Action action)
        {
            if (classificationFormatMap.IsInBatchUpdate)
            {
                action();
            }
            else
            {
                classificationFormatMap.BeginBatchUpdate();
                action();
                classificationFormatMap.EndBatchUpdate();
            }
        }

        public string GetEditorFormatDefinitionName(DynamicCoverageType coverageType)
        {
            var editorFormatDefinitionName = FCCCoveredClassificationTypeName;
            switch (coverageType)
            {
                case DynamicCoverageType.Partial:
                    editorFormatDefinitionName = FCCPartiallyCoveredClassificationTypeName;
                    break;
                case DynamicCoverageType.NotCovered:
                    editorFormatDefinitionName = FCCNotCoveredClassificationTypeName;
                    break;
                case DynamicCoverageType.Dirty:
                    editorFormatDefinitionName = FCCDirtyClassificationTypeName;
                    break;
                case DynamicCoverageType.NewLine:
                    editorFormatDefinitionName = FCCNewLineClassificationTypeName;
                    break;
            }
            return editorFormatDefinitionName;
        }

        public IClassificationType GetClassificationType(DynamicCoverageType coverageType)
        {
            return classificationTypes[coverageType];
        }

        public void SetCoverageColours(IEnumerable<ICoverageTypeColour> coverageTypeColours)
        {
            BatchUpdateIfRequired(() =>
            {
                foreach (var coverageTypeColour in coverageTypeColours)
                {
                    SetCoverageColour(coverageTypeColour);
                }
            });
        }

        private void SetCoverageColour(ICoverageTypeColour coverageTypeColour)
        {
            var classificationType = classificationTypes[coverageTypeColour.CoverageType];
            classificationFormatMap.AddExplicitTextProperties(
                classificationType, coverageTypeColour.TextFormattingRunProperties, highestPriorityClassificationType);
        }
    }

}
