using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Text.Classification;
using FineCodeCoverage.Core.Utilities;
using Microsoft.VisualStudio.Utilities;
using Microsoft.VisualStudio.Text.Formatting;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ICoverageTypeService))]
    [Export(typeof(CoverageColoursProvider))]
    [Export(typeof(ICoverageColoursProvider))]
    [Export(typeof(ICoverageColoursEditorFormatMapNames))]
    [Guid(TextMarkerProviderString)]
    internal class CoverageColoursProvider : 
        ICoverageColoursProvider, ICoverageTypeService, ICoverageColoursEditorFormatMapNames, IVsTextMarkerTypeProvider
    {
        private readonly Guid EditorTextMarkerFontAndColorCategory = new Guid("FF349800-EA43-46C1-8C98-878E78F46501");
        private readonly IEventAggregator eventAggregator;
        private readonly IFontsAndColorsHelper fontsAndColorsHelper;

        #region markers
        #region names
        private const string CoverageTouchedArea = "Coverage Touched Area";
        private const string CoverageNotTouchedArea = "Coverage Not Touched Area";
        private const string CoveragePartiallyTouchedArea = "Coverage Partially Touched Area";
        #endregion
        #region marker guids
        public const string TouchedGuidString = "{E25C42FC-2A01-4C17-B553-AF3F9B93E1D5}";
        public const string NotTouchedGuidString = "{0B46CA71-A74C-40F2-A3C8-8FE5542F5DE5}";
        public const string PartiallyTouchedGuidString = "{5E04DD15-3061-4C03-B23E-93AAB9D923A2}";

        public const string TextMarkerProviderString = "1D1E3CAA-74ED-48B3-9923-5BDC48476CB0";
        #endregion
        private readonly IReadOnlyDictionary<Guid, IVsPackageDefinedTextMarkerType> markerTypes;
        private readonly bool shouldAddCoverageMarkers;
        #endregion
        #region classification types
        public const string FCCCoveredClassificationTypeName = "FCCCovered";
        public const string FCCNotCoveredClassificationTypeName = "FCCNotCovered";
        public const string FCCPartiallyCoveredClassificationTypeName = "FCCPartial";

        [Export]
        [Name(FCCNotCoveredClassificationTypeName)]
        public ClassificationTypeDefinition FCCNotCoveredTypeDefinition { get; set; }

        [Export]
        [Name(FCCCoveredClassificationTypeName)]
        public ClassificationTypeDefinition FCCCoveredTypeDefinition { get; set; }

        [Export]
        [Name(FCCPartiallyCoveredClassificationTypeName)]
        public ClassificationTypeDefinition FCCPartiallyCoveredTypeDefinition { get; set; }

        private class ClassificationTypes
        {
            public ClassificationTypes(IClassificationFormatMap classificationFormatMap, IClassificationTypeRegistryService classificationTypeRegistryService)
            {
                HighestPriorityClassificationType = classificationFormatMap.CurrentPriorityOrder.Where(ct => ct != null).Last();

                var notCoveredClassificationType = classificationTypeRegistryService.GetClassificationType(FCCNotCoveredClassificationTypeName);
                var coveredClassificationType = classificationTypeRegistryService.GetClassificationType(FCCCoveredClassificationTypeName);
                var partiallyCoveredClassificationType = classificationTypeRegistryService.GetClassificationType(FCCPartiallyCoveredClassificationTypeName);
                CoverageClassificationTypes = new ReadOnlyDictionary<CoverageType, IClassificationType>(new Dictionary<CoverageType, IClassificationType>
                {
                    { CoverageType.Covered, coveredClassificationType },
                    { CoverageType.NotCovered, notCoveredClassificationType },
                    { CoverageType.Partial, partiallyCoveredClassificationType }
                });
            }

            public IClassificationType HighestPriorityClassificationType { get; }
            public ReadOnlyDictionary<CoverageType, IClassificationType> CoverageClassificationTypes { get; }
        }

        private readonly ClassificationTypes classificationTypes;
        #endregion

        private readonly IClassificationFormatMap classificationFormatMap;
        private readonly IEditorFormatMap editorFormatMap;
        private CoverageColours lastCoverageColours;
        private bool changingColours;
        private bool hasSetClassificationTypeColours;

        [ImportingConstructor]
        public CoverageColoursProvider(
            IEditorFormatMapService editorFormatMapService,
            IEventAggregator eventAggregator,
            IShouldAddCoverageMarkersLogic shouldAddCoverageMarkersLogic,
            IClassificationFormatMapService classificationFormatMapService,
            IClassificationTypeRegistryService classificationTypeRegistryService,
            IFontsAndColorsHelper fontsAndColorsHelper
        )
        {
            this.eventAggregator = eventAggregator;
            this.fontsAndColorsHelper = fontsAndColorsHelper;

            classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("text");
            classificationTypes = new ClassificationTypes(classificationFormatMap, classificationTypeRegistryService);

            shouldAddCoverageMarkers = shouldAddCoverageMarkersLogic.ShouldAddCoverageMarkers();
            if (shouldAddCoverageMarkers)
            {
                this.markerTypes = CreateMarkerTypes();
            }

            editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
            editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;

            InitializeClassificationTypeColours();
        }

        private IReadOnlyDictionary<Guid, IVsPackageDefinedTextMarkerType> CreateMarkerTypes()
        {
            var coverageColours = new CoverageColours(
                new ItemCoverageColours(Colors.Black, Colors.Green),
                new ItemCoverageColours(Colors.Black, Colors.Red),
                new ItemCoverageColours(Colors.Black, Color.FromRgb(255, 165, 0))
            );

            var _covTouched = new CoverageMarkerType(CoverageTouchedArea, coverageColours.CoverageTouchedColours);
            var _covNotTouched = new CoverageMarkerType(CoverageNotTouchedArea, coverageColours.CoverageNotTouchedColours);
            var _covPartiallyTouched = new CoverageMarkerType(CoveragePartiallyTouchedArea, coverageColours.CoveragePartiallyTouchedColours);
            
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

        private void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            if (changingColours) return;

            var coverageChanged = e.ChangedItems.Any(c => 
                c == CoverageTouchedArea || 
                c == CoverageNotTouchedArea || 
                c == CoveragePartiallyTouchedArea
            );
            if (coverageChanged)
            {
                var currentCoverageColours = GetCoverageColoursFromFontsAndColors();
                SetClassificationTypeColoursIfChanged(currentCoverageColours,lastCoverageColours);
            }
        }

        private void InitializeClassificationTypeColours()
        {
            // if being loaded for the IVsTextMarkerTypeProvider service then this will run after 
            // GetTextMarkerType has been called.
            _ = System.Threading.Tasks.Task.Delay(0).ContinueWith(async (t) =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                if(!hasSetClassificationTypeColours)
                {
                    // if not being loaded for the IVsTextMarkerTypeProvider service then this will get vs to ask for the markers
                    var _ = editorFormatMap.GetProperties(CoverageTouchedArea);
                    // markers available now
                    var coverageColors = GetCoverageColoursPrivate();
                    SetClassificationTypeColoursIfChanged(coverageColors, null);
                }
            },TaskScheduler.Default);
        }

        private void SetClassificationTypeColoursIfChanged(CoverageColours coverageColours,CoverageColours last)
        {
            var changes = coverageColours.GetChanges(last);
            if (changes.Any())
            {
                changingColours = true; 
                BatchUpdateIfRequired(() =>
                {
                    foreach (var change in changes)
                    {
                        SetCoverageColour(classificationTypes.CoverageClassificationTypes[change.Key], change.Value);
                    }
                });
                hasSetClassificationTypeColours = changes.Count == 3;
                changingColours = false;
                lastCoverageColours = coverageColours;
                eventAggregator.SendMessage(new CoverageColoursChangedMessage(coverageColours));
            }
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

        // todo - consider a MEF export to allow other extensions to change the formatting
        private void SetCoverageColour(IClassificationType classificationType, IItemCoverageColours coverageColours)
        {
            classificationFormatMap.AddExplicitTextProperties(classificationType, TextFormattingRunProperties.CreateTextFormattingRunProperties(
                new SolidColorBrush(coverageColours.Foreground), new SolidColorBrush(coverageColours.Background), 
                null, // Typeface
                null, // size
                null, // hinting size
               /*
                   TextDecorationCollection
                    https://docs.microsoft.com/en-us/dotnet/api/system.windows.textdecorations?view=windowsdesktop-8.0
                    https://learn.microsoft.com/en-us/dotnet/api/system.windows.textdecorations?view=windowsdesktop-8.0
               */
               null, 
                // TextEffectCollection https://learn.microsoft.com/en-us/dotnet/api/system.windows.media.texteffect?view=windowsdesktop-8.0
                null, // 
                null // CultureInfo
                ),
                classificationTypes.HighestPriorityClassificationType
            );
        }

        public ICoverageColours GetCoverageColours()
        {
            return GetCoverageColoursPrivate();
        }

        private CoverageColours GetCoverageColoursPrivate()
        {
            if (lastCoverageColours != null)
            {
                return lastCoverageColours;
            }
            lastCoverageColours = GetCoverageColoursFromFontsAndColors();
            return lastCoverageColours;
        }

        private CoverageColours GetCoverageColoursFromFontsAndColors()
        {
            var fromFontsAndColors = GetItemCoverageColoursFromFontsAndColors();
            return CreateCoverageColours(fromFontsAndColors);
        }

        private List<IItemCoverageColours> GetItemCoverageColoursFromFontsAndColors()
        {
            return ThreadHelper.JoinableTaskFactory.Run(() =>
            {
                return fontsAndColorsHelper.GetColorsAsync(
                    EditorTextMarkerFontAndColorCategory,
                    new[] {
                        CoverageTouchedArea,
                        CoverageNotTouchedArea,
                        CoveragePartiallyTouchedArea
                     }
                );
            });
        }
        
        private static CoverageColours CreateCoverageColours(List<IItemCoverageColours> fromFontsAndColors)
        {
            return new CoverageColours(
                fromFontsAndColors[0],
                fromFontsAndColors[1],
                fromFontsAndColors[2]
            );
        }
        
        public IClassificationType GetClassificationType(CoverageType coverageType)
        {
            return classificationTypes.CoverageClassificationTypes[coverageType];
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
    }
}