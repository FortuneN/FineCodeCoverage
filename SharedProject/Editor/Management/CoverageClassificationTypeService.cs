using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

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
        public const string FCCNotIncludedClassificationTypeName = "FCCNotIncluded";
        private readonly Dictionary<DynamicCoverageType, string> editorFormatNames = new Dictionary<DynamicCoverageType, string>
        {
            {DynamicCoverageType.Partial, FCCPartiallyCoveredClassificationTypeName },
            {DynamicCoverageType.NotCovered, FCCNotCoveredClassificationTypeName },
            {DynamicCoverageType.Covered, FCCCoveredClassificationTypeName },
            {DynamicCoverageType.Dirty, FCCDirtyClassificationTypeName },
            {DynamicCoverageType.NewLine, FCCNewLineClassificationTypeName },
            {DynamicCoverageType.NotIncluded, FCCNotIncludedClassificationTypeName }
        };

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

        [ExcludeFromCodeCoverage]
        [Export]
        [Name(FCCNotIncludedClassificationTypeName)]
        public ClassificationTypeDefinition FCCNotIncludedTypeDefinition { get; set; }

        [ImportingConstructor]
        public CoverageClassificationTypeService(
            IClassificationFormatMapService classificationFormatMapService,
            IClassificationTypeRegistryService classificationTypeRegistryService
        )
        {
            this.classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("text");
            this.highestPriorityClassificationType = this.classificationFormatMap.CurrentPriorityOrder.Where(ct => ct != null).Last();

            IClassificationType notCoveredClassificationType = classificationTypeRegistryService.GetClassificationType(FCCNotCoveredClassificationTypeName);
            IClassificationType coveredClassificationType = classificationTypeRegistryService.GetClassificationType(FCCCoveredClassificationTypeName);
            IClassificationType partiallyCoveredClassificationType = classificationTypeRegistryService.GetClassificationType(FCCPartiallyCoveredClassificationTypeName);
            IClassificationType dirtyClassificationType = classificationTypeRegistryService.GetClassificationType(FCCDirtyClassificationTypeName);
            IClassificationType newCodeClassificationType = classificationTypeRegistryService.GetClassificationType(FCCNewLineClassificationTypeName);
            IClassificationType notIncludedClassificationType = classificationTypeRegistryService.GetClassificationType(FCCNotIncludedClassificationTypeName);

            this.classificationTypes = new ReadOnlyDictionary<DynamicCoverageType, IClassificationType>(
                new Dictionary<DynamicCoverageType, IClassificationType>
                {
                    { DynamicCoverageType.Covered, coveredClassificationType },
                    { DynamicCoverageType.NotCovered, notCoveredClassificationType },
                    { DynamicCoverageType.Partial, partiallyCoveredClassificationType },
                    { DynamicCoverageType.Dirty, dirtyClassificationType },
                    { DynamicCoverageType.NewLine, newCodeClassificationType },
                    { DynamicCoverageType.NotIncluded, notIncludedClassificationType }
                });
        }

        private void BatchUpdateIfRequired(Action action)
        {
            if (this.classificationFormatMap.IsInBatchUpdate)
            {
                action();
            }
            else
            {
                this.classificationFormatMap.BeginBatchUpdate();
                action();
                this.classificationFormatMap.EndBatchUpdate();
            }
        }

        public string GetEditorFormatDefinitionName(DynamicCoverageType coverageType) => this.editorFormatNames[coverageType];

        public IClassificationType GetClassificationType(DynamicCoverageType coverageType) => this.classificationTypes[coverageType];

        public void SetCoverageColours(IEnumerable<ICoverageTypeColour> coverageTypeColours)
            => this.BatchUpdateIfRequired(() =>
            {
                foreach (ICoverageTypeColour coverageTypeColour in coverageTypeColours)
                {
                    this.SetCoverageColour(coverageTypeColour);
                }
            });

        private void SetCoverageColour(ICoverageTypeColour coverageTypeColour)
        {
            IClassificationType classificationType = this.classificationTypes[coverageTypeColour.CoverageType];
            this.classificationFormatMap.AddExplicitTextProperties(
                classificationType, coverageTypeColour.TextFormattingRunProperties, this.highestPriorityClassificationType);
        }
    }
}
