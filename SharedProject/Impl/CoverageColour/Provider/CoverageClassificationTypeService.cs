using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ICoverageTypeService))]
    [Export(typeof(ICoverageColoursEditorFormatMapNames))]
    [Export(typeof(ICoverageClassificationColourService))]
    internal class CoverageClassificationTypeService : ICoverageClassificationColourService, ICoverageColoursEditorFormatMapNames, ICoverageTypeService
    {
        public const string FCCCoveredClassificationTypeName = "FCCCovered";
        public const string FCCNotCoveredClassificationTypeName = "FCCNotCovered";
        public const string FCCPartiallyCoveredClassificationTypeName = "FCCPartial";

        private readonly IClassificationFormatMap classificationFormatMap;
        private readonly ReadOnlyDictionary<CoverageType, IClassificationType> classificationTypes;
        private readonly IClassificationType highestPriorityClassificationType;

        [Export]
        [Name(FCCNotCoveredClassificationTypeName)]
        public ClassificationTypeDefinition FCCNotCoveredTypeDefinition { get; set; }

        [Export]
        [Name(FCCCoveredClassificationTypeName)]
        public ClassificationTypeDefinition FCCCoveredTypeDefinition { get; set; }

        [Export]
        [Name(FCCPartiallyCoveredClassificationTypeName)]
        public ClassificationTypeDefinition FCCPartiallyCoveredTypeDefinition { get; set; }


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
            classificationTypes = new ReadOnlyDictionary<CoverageType, IClassificationType>(new Dictionary<CoverageType, IClassificationType>
                {
                    { CoverageType.Covered, coveredClassificationType },
                    { CoverageType.NotCovered, notCoveredClassificationType },
                    { CoverageType.Partial, partiallyCoveredClassificationType }
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

        public string GetEditorFormatDefinitionName(CoverageType coverageType)
        {
            var editorFormatDefinitionName = FCCCoveredClassificationTypeName;
            switch (coverageType)
            {
                case CoverageType.Partial:
                    editorFormatDefinitionName = FCCPartiallyCoveredClassificationTypeName;
                    break;
                case CoverageType.NotCovered:
                    editorFormatDefinitionName = FCCNotCoveredClassificationTypeName;
                    break;
            }
            return editorFormatDefinitionName;
        }

        public IClassificationType GetClassificationType(CoverageType coverageType)
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
            classificationFormatMap.AddExplicitTextProperties(classificationType, coverageTypeColour.TextFormattingRunProperties, highestPriorityClassificationType);
        }
    }

}
