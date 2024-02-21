using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using System.Collections.ObjectModel;
using FineCodeCoverage.Editor.DynamicCoverage;
using Microsoft.VisualStudio.Text.Classification;
using Microsoft.VisualStudio.Utilities;

namespace FineCodeCoverage.Editor.Management
{
    [Export(typeof(CoverageColoursManager))]
    [Guid(TextMarkerProviderString)]
    internal class CoverageColoursManager :  IVsTextMarkerTypeProvider, ICoverageInitializable
    {
        private readonly ICoverageClassificationColourService coverageClassificationColourService;
        private readonly IFontAndColorsInfosProvider fontAndColorsInfosProvider;
        private readonly MarkerTypeNames markerTypeNames;
        private readonly IEditorFormatMapTextSpecificListener editorFormatMapTextSpecificListener;
        private readonly ITextFormattingRunPropertiesFactory textFormattingRunPropertiesFactory;
        private bool hasSetClassificationTypeColours;

        #region markers
        #region marker guids
        public const string TouchedGuidString = "{E25C42FC-2A01-4C17-B553-AF3F9B93E1D5}";
        public const string NotTouchedGuidString = "{0B46CA71-A74C-40F2-A3C8-8FE5542F5DE5}";
        public const string PartiallyTouchedGuidString = "{5E04DD15-3061-4C03-B23E-93AAB9D923A2}";

        public const string TextMarkerProviderString = "1D1E3CAA-74ED-48B3-9923-5BDC48476CB0";
        #endregion
        private readonly IReadOnlyDictionary<Guid, IVsPackageDefinedTextMarkerType> markerTypes;
        private readonly bool shouldAddCoverageMarkers;
        #endregion
        #region New lines / Dirty - format definitions
        private const string newLinesEditorFormatDefinitionName = "Coverage New Lines Area";
        private const string dirtyEditorFormatDefinitionName = "Coverage Dirty Area";

        [Export]
        [Name(newLinesEditorFormatDefinitionName)]
        [UserVisible(true)]
        public EditorFormatDefinition NewLinesEditorFormatDefinition { get; } = new ColoursClassificationFormatDefinition(Colors.Black, Colors.Yellow);

        [Export]
        [Name(dirtyEditorFormatDefinitionName)]
        [UserVisible(true)]
        public EditorFormatDefinition DirtyEditorFormatDefinition { get; set; } = new ColoursClassificationFormatDefinition(Colors.White, Colors.Brown);
        #endregion

        [ImportingConstructor]
        public CoverageColoursManager(
            IShouldAddCoverageMarkersLogic shouldAddCoverageMarkersLogic,
            ICoverageClassificationColourService coverageClassificationColourService,
            IFontAndColorsInfosProvider fontAndColorsInfosProvider,
            MarkerTypeNames markerTypeNames,
            IEditorFormatMapTextSpecificListener editorFormatMapTextSpecificListener,
            ICoverageTextMarkerInitializeTiming initializeTiming,
            ITextFormattingRunPropertiesFactory textFormattingRunPropertiesFactory
        )
        {
            this.coverageClassificationColourService = coverageClassificationColourService;
            this.fontAndColorsInfosProvider = fontAndColorsInfosProvider;
            fontAndColorsInfosProvider.FontAndColorsItemNames = new FontAndColorsItemNames(
                markerTypeNames, 
                new MEFItemNames(newLinesEditorFormatDefinitionName, dirtyEditorFormatDefinitionName)
            );
            this.markerTypeNames = markerTypeNames;
            this.editorFormatMapTextSpecificListener = editorFormatMapTextSpecificListener;
            this.textFormattingRunPropertiesFactory = textFormattingRunPropertiesFactory;
            this.editorFormatMapTextSpecificListener.ListenFor(
                new List<string> { 
                    markerTypeNames.Covered, 
                    markerTypeNames.NotCovered, 
                    markerTypeNames.PartiallyCovered,
                    newLinesEditorFormatDefinitionName,
                    dirtyEditorFormatDefinitionName
                },
                () =>
                {
                    var changedColours = fontAndColorsInfosProvider.GetChangedFontAndColorsInfos();
                    SetClassificationTypeColoursIfChanged(changedColours);
                }
            );
            shouldAddCoverageMarkers = shouldAddCoverageMarkersLogic.ShouldAddCoverageMarkers();
            if (shouldAddCoverageMarkers)
            {
                this.markerTypes = CreateMarkerTypes();
            }

            initializeTiming.Initializable = this; 
        }

        public bool RequiresInitialization => !hasSetClassificationTypeColours;
        
        public void Initialize()
        {
            var coverageColors = fontAndColorsInfosProvider.GetFontAndColorsInfos();
            SetClassificationTypeColoursIfChanged(coverageColors);
        }

        private IReadOnlyDictionary<Guid, IVsPackageDefinedTextMarkerType> CreateMarkerTypes()
        {
            //Colors.Green fails WCAG AA
            var _covTouched = new CoverageMarkerType(markerTypeNames.Covered, new ItemCoverageColours(Colors.Black, Color.FromRgb(16,135,24)));
            var _covNotTouched = new CoverageMarkerType(markerTypeNames.NotCovered, new ItemCoverageColours(Colors.Black, Colors.Red));
            var _covPartiallyTouched = new CoverageMarkerType(markerTypeNames.PartiallyCovered, new ItemCoverageColours(Colors.Black, Color.FromRgb(255, 165, 0)));
            
            return new ReadOnlyDictionary<Guid, IVsPackageDefinedTextMarkerType>(new Dictionary<Guid, IVsPackageDefinedTextMarkerType>
            {
                {new Guid(TouchedGuidString),_covTouched },
                {new Guid(NotTouchedGuidString),_covNotTouched },
                {new Guid(PartiallyTouchedGuidString),_covPartiallyTouched }
            });
        }

        public int GetTextMarkerType(ref Guid markerGuid, out IVsPackageDefinedTextMarkerType markerType)
        {
            markerType = shouldAddCoverageMarkers ? markerTypes[markerGuid] : null;
            return 0;
        }

        private void SetClassificationTypeColoursIfChanged(Dictionary<DynamicCoverageType, IFontAndColorsInfo> changes)
        {
            if (changes.Any())
            {
                editorFormatMapTextSpecificListener.PauseListeningWhenExecuting(
                    () => SetClassificationTypeColours(changes)
                );
                hasSetClassificationTypeColours = changes.Count == 5;
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