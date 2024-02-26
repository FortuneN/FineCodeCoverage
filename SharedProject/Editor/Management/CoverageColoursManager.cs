using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace FineCodeCoverage.Editor.Management
{
    [Export(typeof(IInitializable))]
    internal class CoverageColoursManager : IInitializable
    {
        private readonly ICoverageClassificationColourService coverageClassificationColourService;
        private readonly IFontAndColorsInfosProvider fontAndColorsInfosProvider;
        private readonly IEditorFormatMapTextSpecificListener editorFormatMapTextSpecificListener;
        private readonly ITextFormattingRunPropertiesFactory textFormattingRunPropertiesFactory;

        #region format definitions
        private const string partiallyCoveredEditorFormatDefinitionName = "Coverage Partially Touched Area FCC";
        private const string notCoveredEditorFormatDefinitionName = "Coverage Not Touched Area FCC";
        private const string coveredEditorFormatDefinitionName = "Coverage Touched Area FCC";
        private const string newLinesEditorFormatDefinitionName = "Coverage New Lines Area FCC";
        private const string dirtyEditorFormatDefinitionName = "Coverage Dirty Area FCC";
        private const string notIncludedEditorFormatDefintionName = "Coverage Not Included Area FCC";

        [Export]
        [Name(notIncludedEditorFormatDefintionName)]
        [UserVisible(true)]
        public EditorFormatDefinition NotIncludedEditorFormatDefinition { get; } = new ColoursClassificationFormatDefinition(Colors.Black, Colors.LightPink);

        [Export]
        [Name(newLinesEditorFormatDefinitionName)]
        [UserVisible(true)]
        public EditorFormatDefinition NewLinesEditorFormatDefinition { get; } = new ColoursClassificationFormatDefinition(Colors.Black, Colors.Yellow);

        [Export]
        [Name(dirtyEditorFormatDefinitionName)]
        [UserVisible(true)]
        public EditorFormatDefinition DirtyEditorFormatDefinition { get;} = new ColoursClassificationFormatDefinition(Colors.White, Colors.Brown);

        [Export]
        [Name(coveredEditorFormatDefinitionName)]
        [UserVisible(true)]
        public EditorFormatDefinition CoveredEditorFormatDefinition { get; } = new ColoursClassificationFormatDefinition(Colors.Black, Color.FromRgb(16, 135, 24));

        [Export]
        [Name(notCoveredEditorFormatDefinitionName)]
        [UserVisible(true)]
        public EditorFormatDefinition NotCoveredEditorFormatDefinition { get;} = new ColoursClassificationFormatDefinition(Colors.White, Colors.Red);

        [Export]
        [Name(partiallyCoveredEditorFormatDefinitionName)]
        [UserVisible(true)]
        public EditorFormatDefinition PartiallyCoveredEditorFormatDefinition { get; } = new ColoursClassificationFormatDefinition(Colors.Black, Color.FromRgb(255, 165, 0));
        
        #endregion

        [ImportingConstructor]
        public CoverageColoursManager(
            IVsHasCoverageMarkersLogic vsHasCoverageMarkersLogic,
            ICoverageClassificationColourService coverageClassificationColourService,
            IFontAndColorsInfosProvider fontAndColorsInfosProvider,
            IEditorFormatMapTextSpecificListener editorFormatMapTextSpecificListener,
            ITextFormattingRunPropertiesFactory textFormattingRunPropertiesFactory,
            IDelayedMainThreadInvocation delayedMainThreadInvocation,
            ICoverageFontAndColorsCategoryItemNamesManager coverageFontAndColorsCategoryItemNamesManager
        )
        {
            this.coverageClassificationColourService = coverageClassificationColourService;
            this.fontAndColorsInfosProvider = fontAndColorsInfosProvider;
            this.editorFormatMapTextSpecificListener = editorFormatMapTextSpecificListener;
            this.textFormattingRunPropertiesFactory = textFormattingRunPropertiesFactory;

            coverageFontAndColorsCategoryItemNamesManager.Initialize(
                new FCCEditorFormatDefinitionNames(
                    coveredEditorFormatDefinitionName,
                    notCoveredEditorFormatDefinitionName,
                    partiallyCoveredEditorFormatDefinitionName,
                    newLinesEditorFormatDefinitionName,
                    dirtyEditorFormatDefinitionName,
                    notIncludedEditorFormatDefintionName
            ));
            coverageFontAndColorsCategoryItemNamesManager.Changed += (sender, args) =>
            {
                Changed();
            };
            fontAndColorsInfosProvider.CoverageFontAndColorsCategoryItemNames = coverageFontAndColorsCategoryItemNamesManager.CategoryItemNames;
           
            this.editorFormatMapTextSpecificListener.ListenFor(
                new List<string> {
                    MarkerTypeNames.Covered,
                    MarkerTypeNames.NotCovered,
                    MarkerTypeNames.PartiallyCovered,
                    coveredEditorFormatDefinitionName,
                    notCoveredEditorFormatDefinitionName,
                    partiallyCoveredEditorFormatDefinitionName,

                    newLinesEditorFormatDefinitionName,
                    dirtyEditorFormatDefinitionName,
                    notIncludedEditorFormatDefintionName
                },
                () =>
                {
                    Changed();
                }
            );

            delayedMainThreadInvocation.DelayedInvoke(InitializeColours);
        }

        private void InitializeColours()
        {
            var coverageColors = fontAndColorsInfosProvider.GetFontAndColorsInfos();
            SetClassificationTypeColoursIfChanged(coverageColors);
        }

        private void Changed()
        {
            var changedColours = fontAndColorsInfosProvider.GetChangedFontAndColorsInfos();
            SetClassificationTypeColoursIfChanged(changedColours);
        }

        private void SetClassificationTypeColoursIfChanged(Dictionary<DynamicCoverageType, IFontAndColorsInfo> changes)
        {
            if (changes.Any())
            {
                editorFormatMapTextSpecificListener.PauseListeningWhenExecuting(
                    () => SetClassificationTypeColours(changes)
                );
            }
        }
        
        private void SetClassificationTypeColours(Dictionary<DynamicCoverageType,IFontAndColorsInfo> changes)
        {
            var coverageTypeColours = changes.Select(
                change => new CoverageTypeColour(change.Key, textFormattingRunPropertiesFactory.Create(change.Value))
            );
            coverageClassificationColourService.SetCoverageColours(coverageTypeColours);
        }
    }
}