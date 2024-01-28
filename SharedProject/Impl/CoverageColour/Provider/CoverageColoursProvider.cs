using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Media;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio.TextManager.Interop;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Text.Classification;
using FineCodeCoverage.Core.Utilities;
using Microsoft.VisualStudio.Utilities;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.VisualStudio.Text.Formatting;

namespace FineCodeCoverage.Impl
{
    interface ICoverageColoursEditorFormatMapNames
    {
        string CoverageTouchedArea { get; }
        string CoverageNotTouchedArea { get; }
        string CoveragePartiallyTouchedArea {get;}
    }

    [Export(typeof(ICoverageColoursEditorFormatMapNames))]
    internal class EnterpriseFontsAndColorsNames : ICoverageColoursEditorFormatMapNames
    {
        public string CoverageTouchedArea { get; } = "Coverage Touched Area";
        public string CoverageNotTouchedArea { get; } = "Coverage Not Touched Area";
        public string CoveragePartiallyTouchedArea { get; } = "Coverage Partially Touched Area";
    }

    internal interface IFontsAndColorsHelper
    {
        System.Threading.Tasks.Task<List<IItemCoverageColours>> GetColorsAsync(Guid category, IEnumerable<string> names);
    }

    [Export(typeof(IFontsAndColorsHelper))]
    internal class FontsAndColorsHelper : IFontsAndColorsHelper
    {
        private AsyncLazy<IVsFontAndColorStorage> lazyIVsFontAndColorStorage;
        private readonly uint storeFlags = (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS | __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS | __FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES);


        [ImportingConstructor]
        public FontsAndColorsHelper(
            [Import(typeof(SVsServiceProvider))] System.IServiceProvider serviceProvider
        )
        {
            lazyIVsFontAndColorStorage = new AsyncLazy<IVsFontAndColorStorage>(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return (IVsFontAndColorStorage)serviceProvider.GetService(typeof(IVsFontAndColorStorage));
            }, ThreadHelper.JoinableTaskFactory);
        }

        private System.Windows.Media.Color ParseColor(uint color)
        {
            var dcolor = System.Drawing.ColorTranslator.FromOle(Convert.ToInt32(color));
            return System.Windows.Media.Color.FromArgb(dcolor.A, dcolor.R, dcolor.G, dcolor.B);
        }

        public async System.Threading.Tasks.Task<List<IItemCoverageColours>> GetColorsAsync(Guid category,IEnumerable<string> names)
        {
            var colors = new List<IItemCoverageColours>();
            var fontAndColorStorage = await lazyIVsFontAndColorStorage.GetValueAsync();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var success = fontAndColorStorage.OpenCategory(ref category, storeFlags);
            if (success == VSConstants.S_OK)
            {
                // https://github.com/microsoft/vs-threading/issues/993
                IItemCoverageColours GetColor(string displayName)
                {
                    var touchAreaInfo = new ColorableItemInfo[1];
                    var getItemSuccess = fontAndColorStorage.GetItem(displayName, touchAreaInfo);
                    if (getItemSuccess == VSConstants.S_OK)
                    {
                        var bgColor =  ParseColor(touchAreaInfo[0].crBackground);
                        var fgColor = ParseColor(touchAreaInfo[0].crForeground);
                        return new ItemCoverageColours(fgColor,bgColor);

                    }
                    return null;
                }
                colors = names.Select(name => GetColor(name)).Where(color => color!=null).ToList();
            }

            fontAndColorStorage.CloseCategory();
            return colors;
        }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = true)]
    public class ProvideTextMarker : RegistrationAttribute
    {
        private readonly string _markerName, _markerGUID, _markerProviderGUID, _displayName;

        public ProvideTextMarker(string markerName,string displayName, string markerGUID, string markerProviderGUID)
        {
            Contract.Requires(markerName != null);
            Contract.Requires(markerGUID != null);
            Contract.Requires(markerProviderGUID != null);

            _markerName = markerName;
             _displayName = displayName;
            _markerGUID = markerGUID;
            _markerProviderGUID = markerProviderGUID;
        }

        public override void Register(RegistrationAttribute.RegistrationContext context)
        {
            //Key markerkey = context.CreateKey("Text Editor\\External Markers\\{" + _markerGUID + "}");
            Key markerkey = context.CreateKey("Text Editor\\External Markers\\" + _markerGUID);
            markerkey.SetValue("", _markerName);
            markerkey.SetValue("Service", "{" + _markerProviderGUID + "}");
            markerkey.SetValue("DisplayName", _displayName);
            markerkey.SetValue("Package", "{" + context.ComponentType.GUID + "}");
        }

        public override void Unregister(RegistrationAttribute.RegistrationContext context)
        {
            context.RemoveKey("Text Editor\\External Markers\\" + _markerGUID);
        }
    }

    // Thiis will be converted internally to AllColorableItemInfo
    internal class CoverageMarkerType :
    IVsPackageDefinedTextMarkerType,
    IVsMergeableUIItem,
    IVsHiColorItem
    {
        private readonly string name;
        private readonly Color foregroundColor;
        private readonly Color backgroundColor;

        public CoverageMarkerType(string name,Color foregroundColor, Color backgroundColor)
        {
            this.name = name;
            this.foregroundColor = foregroundColor;
            this.backgroundColor = backgroundColor;
        }

        #region IVsPackageDefinedTextMarkerType - mainly irrelevant as not using as a marker - need to sets colors
        public int GetVisualStyle(out uint pdwVisualFlags)
        {
            // no line style calls
            pdwVisualFlags = (uint)MARKERVISUAL.MV_GLYPH;
            return 0;
        }

        // Docs state
        // The environment only calls this method if you specify a value of MV_LINE or MV_BORDER for your marker type.
        // ( Which must be GetVisualStyle )
        // but in reality - if (((int) pdwVisualFlags & 8456) != 0) - which also includes MV_COLOR_SPAN_IF_ZERO_LENGTH
        public int GetDefaultLineStyle(COLORINDEX[] piLineColor, LINESTYLE[] piLineIndex)
        {
            return -2147467263;
        }

        /*
            Docs state
            The environment only calls this method if you specify a value of MV_LINE or MV_BORDER for your marker type.
            
            if (((int) pdwVisualFlags & 71) != 0) - 1000111 - BUT WILL GO TO IVsHiColorItem.GetColorData instead if present
        */
        public int GetDefaultColors(COLORINDEX[] piForeground, COLORINDEX[] piBackground)
        {
            return -2147467263;
        }
        
        public int GetBehaviorFlags(out uint pdwFlags)
        {
            pdwFlags = 0U;
            return 0;
        }
        public int GetDefaultFontFlags(out uint pdwFontFlags)
        {
            pdwFontFlags = 0U;
            return 0;
        }
        public int GetPriorityIndex(out int piPriorityIndex)
        {
            piPriorityIndex = 0;
            return 0;
        }
        public int DrawGlyphWithColors(
            IntPtr hdc,
            RECT[] pRect,
            int iMarkerType,
            IVsTextMarkerColorSet pMarkerColors,
            uint dwGlyphDrawFlags,
            int iLineHeight
        )
        {
            return 0;
        }

        #endregion

        //If yours is the primary package to be defining the marker, use 0x2000 or greater.
        public int GetMergingPriority(out int piMergingPriority)
        {
            piMergingPriority = 0x2000;
            return 0;
        }

        // This is not called.  Could be AllColorableItemInfo.bstrDescription - ( This feature is currently disabled )
        public int GetDescription(out string pbstrDesc)
        {
            pbstrDesc = "Coverage Description goes here";
            return 0;
        }

        public int GetDisplayName(out string pbstrDisplayName)
        {
            pbstrDisplayName = this.name;
            return 0;
        }

        public int GetCanonicalName(out string pbstrNonLocalizeName)
        {
            return GetDisplayName(out pbstrNonLocalizeName);
        }

        /*
            IVsHiColorItem 
            Notes to Callers
            If this interface can be obtained from an object that implements the IVsColorableItem or IVsPackageDefinedTextMarkerType interface, 
            then that object is advertising support for high color values. 
            Call the GetColorData(Int32, UInt32) method to get the RGB values for the individual foreground, background, and line colors. 
            If the GetColorData(Int32, UInt32) method returns an error, gracefully fall back to 
            accessing the colors on the original IVsColorableItem or IVsPackageDefinedTextMarkerType interfaces.

            --
            cdElement 
            0 is foreground, 1 is background, 2 is line color
        */

        

        public int GetColorData(int cdElement, out uint crColor)
        {
            crColor = 0U;
            if(cdElement == 2)
            {
                return -2147467259;
            }
            var foreground = cdElement == 0;
            var color = foreground ? foregroundColor : backgroundColor;
            crColor = ColorToRgb(color);
            return 0;
        }

        private uint ColorToRgb(Color color)
        {
            int r = (int)color.R;
            short g = (short)color.G;
            int b = (int)color.B;
            int num = (int)g << 8;
            return (uint)(r | num | b << 16);
        }
        
    }

    internal class CoverageColours : ICoverageColours
    {
        public IItemCoverageColours CoverageTouchedColours { get; }
        public IItemCoverageColours CoverageNotTouchedColours { get; }
        public IItemCoverageColours CoveragePartiallyTouchedColours { get; }
        public CoverageColours(
            IItemCoverageColours coverageTouchedColors,
            IItemCoverageColours coverageNotTouched, 
            IItemCoverageColours coveragePartiallyTouchedColors
        )
        {
            CoverageTouchedColours = coverageTouchedColors;
            CoverageNotTouchedColours = coverageNotTouched;
            CoveragePartiallyTouchedColours = coveragePartiallyTouchedColors;
        }

        internal bool AreEqual(CoverageColours lastCoverageColours)
        {
            if (lastCoverageColours == null) return false;

            return CoverageTouchedColours.Equals(lastCoverageColours.CoverageTouchedColours) &&
                CoverageNotTouchedColours.Equals(lastCoverageColours.CoverageNotTouchedColours) &&
                CoveragePartiallyTouchedColours.Equals(lastCoverageColours.CoveragePartiallyTouchedColours);
        }

        public IItemCoverageColours GetColor(CoverageType coverageType)
        {
            switch (coverageType)
            {
                case CoverageType.Partial:
                    return CoveragePartiallyTouchedColours;
                case CoverageType.NotCovered:
                    return CoverageNotTouchedColours;
                case CoverageType.Covered:
                    return CoverageTouchedColours;
            }
            return default;
        }

    }

    internal interface IShouldAddCoverageMarkersLogic
    {
        bool ShouldAddCoverageMarkers();
    }

    [Export(typeof(IShouldAddCoverageMarkersLogic))]
    class ShouldAddCoverageMarkersLogic : IShouldAddCoverageMarkersLogic
    {
        public bool ShouldAddCoverageMarkers()
        {
            return !AppDomain.CurrentDomain.GetAssemblies().Any(a => a.GetName().Name == "Microsoft.CodeCoverage.VisualStudio.Window");
        }
    }

    public interface IClassificationFormatMetadata : IEditorFormatMetadata, IOrderable
    {
        string[] ClassificationTypeNames { get; }
    }
    public interface IEditorFormatMetadata
    {
        string Name { get; }

        [DefaultValue(false)]
        bool UserVisible { get; }

        [DefaultValue(0)]
        int Priority { get; }
    }

    public interface IClassificationTypeDefinitionMetadata
    {
        string Name { get; }

        [DefaultValue(null)]
        IEnumerable<string> BaseDefinition { get; }
    }

    // this will sync up using CoverageColoursProvider / Use the base ClassificationFormatDefinition
    internal abstract class CoverageEditorFormatDefinition : EditorFormatDefinition, ICoverageEditorFormatDefinition
    {
        public CoverageEditorFormatDefinition(
            string identifier,
            ICoverageColoursProvider coverageColoursProvider,
            CoverageType coverageType)
        {
            Identifier = identifier;
            CoverageType = coverageType;
            BackgroundColor = Colors.Pink;
            ForegroundColor = Colors.Black;
            ForegroundBrush = new SolidColorBrush(ForegroundColor.Value);
            // WHY IS IT NOT AVAILABLE YET ?
            //var coverageColours = coverageColoursProvider.GetCoverageColours();
            //BackgroundColor = coverageColours.GetColor(coverageType);
        }
        public string Identifier { get; private set; }
        public void SetBackgroundColor(Color backgroundColor)
        {
            BackgroundColor = backgroundColor;
        }
        public CoverageType CoverageType { get; }
    }


   // [Export(typeof(EditorFormatDefinition))]
    [Name(ResourceName)]
    [UserVisible(false)]
    [Microsoft.VisualStudio.Utilities.Order(Before = "formal language")]
    internal class NotCoveredEditorFormatDefinition : CoverageEditorFormatDefinition
    {
        public const string ResourceName = "FCCNotCovered";
        [ImportingConstructor]
        public NotCoveredEditorFormatDefinition(
            ICoverageColoursProvider coverageColoursProvider
        ) : base(ResourceName,coverageColoursProvider, CoverageType.NotCovered)
        {
        }
    }

    //[Export(typeof(EditorFormatDefinition))]
    [Name(ResourceName)]
    [UserVisible(false)]
    [Microsoft.VisualStudio.Utilities.Order(Before = "formal language")]
    internal class PartiallyCoveredEditorFormatDefinition : CoverageEditorFormatDefinition
    {
        public const string ResourceName = "FCCPartial";
        [ImportingConstructor]
        public PartiallyCoveredEditorFormatDefinition(
            ICoverageColoursProvider coverageColoursProvider
        ) : base(ResourceName,coverageColoursProvider, CoverageType.Partial)
        {
        }
    }

    //[Export(typeof(EditorFormatDefinition))]
    [Name(ResourceName)]
    [UserVisible(false)]
    [Microsoft.VisualStudio.Utilities.Order(Before = "formal language")]
    internal class CoveredEditorFormatDefinition : CoverageEditorFormatDefinition
    {
        public const string ResourceName = "FCCCovered";
        [ImportingConstructor]
        public CoveredEditorFormatDefinition(
            ICoverageColoursProvider coverageColoursProvider
        ) : base(ResourceName,coverageColoursProvider, CoverageType.Covered)
        {
        }
    }

    internal interface IItemCoverageColours:IEquatable<IItemCoverageColours>
    {
        Color Foreground { get; }
        Color Background { get; }
        
    }

    internal class ItemCoverageColours : IItemCoverageColours
    {
        public ItemCoverageColours(Color foreground, Color background)
        {
            this.Foreground = foreground;
            this.Background = background;
        }

        public Color Foreground { get; }
        public Color Background { get; }

        public bool Equals(IItemCoverageColours other)
        {
            if (other == this) return true;
            if (other == null) return false;
            return Foreground == other.Foreground && Background == other.Background;

        }
    }


    [Export(typeof(CoverageColoursProvider))]
    [Export(typeof(ICoverageColoursProvider))]
    [Guid(TextMarkerProviderString)]
    internal class CoverageColoursProvider : ICoverageColoursProvider, IVsTextMarkerTypeProvider
    {
        public const string TouchedGuidString = "{E25C42FC-2A01-4C17-B553-AF3F9B93E1D5}";
        public static readonly Guid TouchedGuid = new Guid(TouchedGuidString);
        public const string NotTouchedGuidString = "{0B46CA71-A74C-40F2-A3C8-8FE5542F5DE5}";
        public static readonly Guid NotTouchedGuid = new Guid(NotTouchedGuidString);
        public const string PartiallyTouchedGuidString = "{5E04DD15-3061-4C03-B23E-93AAB9D923A2}";
        public static readonly Guid PartiallyTouchedGuid = new Guid(PartiallyTouchedGuidString);

        public const string TextMarkerProviderString = "1D1E3CAA-74ED-48B3-9923-5BDC48476CB0";

        public static readonly Guid EditorTextMarkerFontAndColorCategory = new Guid("FF349800-EA43-46C1-8C98-878E78F46501");

        private readonly CoverageMarkerType _covTouched;
        private readonly CoverageMarkerType _covNotTouched;
        private readonly CoverageMarkerType _covPartiallyTouched;

        private readonly IEditorFormatMap editorFormatMap;
        private readonly IEventAggregator eventAggregator;
        private readonly ICoverageColoursEditorFormatMapNames coverageColoursEditorFormatMapNames;
        private readonly IFontsAndColorsHelper fontsAndColorsHelper;
        private readonly bool shouldAddCoverageMarkers;
        private CoverageColours lastCoverageColours;


        [Export]
        [Name(NotCoveredEditorFormatDefinition.ResourceName)]
        public ClassificationTypeDefinition FCCNotCoveredTypeDefinition { get; set; }

        [Export]
        [Name(CoveredEditorFormatDefinition.ResourceName)]
        public ClassificationTypeDefinition FCCCoveredTypeDefinition { get; set; }

        [Export]
        [Name(PartiallyCoveredEditorFormatDefinition.ResourceName)]
        public ClassificationTypeDefinition FCCPartiallyCoveredTypeDefinition { get; set; }

        private IClassificationType coveredClassificationType;
        private IClassificationFormatMap classificationFormatMap;
        private IClassificationType highestPriorityClassificationType;
        private IClassificationType notCoveredClassificationType;
        private IClassificationType partiallyCoveredClassificationType;

        [ImportingConstructor]
        public CoverageColoursProvider(
            IEditorFormatMapService editorFormatMapService,
            IEventAggregator eventAggregator,
            IShouldAddCoverageMarkersLogic shouldAddCoverageMarkersLogic,
            ICoverageColoursEditorFormatMapNames coverageColoursEditorFormatMapNames,
            IClassificationFormatMapService classificationFormatMapService,
            IClassificationTypeRegistryService classificationTypeRegistryService,
            IFontsAndColorsHelper fontsAndColorsHelper
        )
        {
            this.fontsAndColorsHelper = fontsAndColorsHelper;
            var touchedColours = GetColors(coverageColoursEditorFormatMapNames.CoverageTouchedArea);
            
            classificationFormatMap = classificationFormatMapService.GetClassificationFormatMap("text");
            highestPriorityClassificationType = classificationFormatMap.CurrentPriorityOrder.Where(ct => ct != null).Last();
            notCoveredClassificationType = classificationTypeRegistryService.GetClassificationType(NotCoveredEditorFormatDefinition.ResourceName);
            coveredClassificationType = classificationTypeRegistryService.GetClassificationType(CoveredEditorFormatDefinition.ResourceName);
            partiallyCoveredClassificationType =  classificationTypeRegistryService.GetClassificationType(PartiallyCoveredEditorFormatDefinition.ResourceName);
            
            SetCoverageColour(notCoveredClassificationType, new ItemCoverageColours(Colors.Black, Colors.Red));
            SetCoverageColour(coveredClassificationType, new ItemCoverageColours(Colors.Black, Colors.Green));
            SetCoverageColour(partiallyCoveredClassificationType, new ItemCoverageColours(Colors.Black, Color.FromRgb(255, 165, 0)));

            this._covTouched = new CoverageMarkerType(coverageColoursEditorFormatMapNames.CoverageTouchedArea,Colors.Black,Colors.Green);
            this._covNotTouched = new CoverageMarkerType(coverageColoursEditorFormatMapNames.CoverageNotTouchedArea,Colors.Black,Colors.Red);
            this._covPartiallyTouched = new CoverageMarkerType(coverageColoursEditorFormatMapNames.CoveragePartiallyTouchedArea, Colors.Black, Color.FromRgb(255, 165, 0));

            editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
            editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
            this.eventAggregator = eventAggregator;
            this.coverageColoursEditorFormatMapNames = coverageColoursEditorFormatMapNames;
            
            shouldAddCoverageMarkers = shouldAddCoverageMarkersLogic.ShouldAddCoverageMarkers();
        }

        private void SetCoverageColour(IClassificationType classificationType, IItemCoverageColours coverageColours)
        {
            changingColours = true;
            classificationFormatMap.AddExplicitTextProperties(classificationType, TextFormattingRunProperties.CreateTextFormattingRunProperties(
                new SolidColorBrush(coverageColours.Foreground), new SolidColorBrush(coverageColours.Background), null, null, null, null, null, null
                ),
                highestPriorityClassificationType
            );
            changingColours = false;
        }

        private bool changingColours;
        private void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            if (changingColours) return;

            var coverageChanged = e.ChangedItems.Any(c => 
                c == coverageColoursEditorFormatMapNames.CoverageTouchedArea || 
                c == coverageColoursEditorFormatMapNames.CoverageNotTouchedArea || 
                c == coverageColoursEditorFormatMapNames.CoveragePartiallyTouchedArea
            );
            if (coverageChanged)
            {
                var currentCoverageColours = GetCoverageColoursFromFontsAndColors();
                if (!currentCoverageColours.AreEqual(lastCoverageColours))
                {
                    if(!lastCoverageColours.CoverageNotTouchedColours.Equals(currentCoverageColours.CoverageNotTouchedColours))
                    {
                        SetCoverageColour(notCoveredClassificationType,currentCoverageColours.CoverageNotTouchedColours);
                    }
                    if(!lastCoverageColours.CoveragePartiallyTouchedColours.Equals(currentCoverageColours.CoveragePartiallyTouchedColours))
                    {
                        SetCoverageColour(partiallyCoveredClassificationType,currentCoverageColours.CoveragePartiallyTouchedColours);
                    }
                    if(!lastCoverageColours.CoverageTouchedColours.Equals(currentCoverageColours.CoverageTouchedColours))
                    {
                        SetCoverageColour(coveredClassificationType, currentCoverageColours.CoverageTouchedColours);
                    }
                    lastCoverageColours = currentCoverageColours;
                    eventAggregator.SendMessage(new CoverageColoursChangedMessage(currentCoverageColours));
                    
                }
            }
        }

        public int GetTextMarkerType(ref Guid markerGuid, out IVsPackageDefinedTextMarkerType markerType)
        {
            markerType = null;
            if (shouldAddCoverageMarkers)
            {
                markerType = markerGuid.Equals(TouchedGuid) ? this._covTouched :
                (markerGuid.Equals(PartiallyTouchedGuid) ? this._covPartiallyTouched :
                this._covNotTouched);
                return 0;
            }
            
            return 0;
        }

        public ICoverageColours GetCoverageColours()
        {
            if(lastCoverageColours != null)
            {
                return lastCoverageColours;
            }
            lastCoverageColours = GetCoverageColoursFromFontsAndColors();
            return lastCoverageColours;
        }

        private CoverageColours GetCoverageColoursFromFontsAndColors()
        {
            var fromFontsAndColors = fontsAndColorsHelper.GetColorsAsync(EditorTextMarkerFontAndColorCategory, new[] {
                coverageColoursEditorFormatMapNames.CoverageTouchedArea,
                coverageColoursEditorFormatMapNames.CoverageNotTouchedArea,
                coverageColoursEditorFormatMapNames.CoveragePartiallyTouchedArea
            }).GetAwaiter().GetResult();
            
            return new CoverageColours(
                fromFontsAndColors[0],
                fromFontsAndColors[1],
                fromFontsAndColors[2]
            );
        }



        private IItemCoverageColours GetColors(string coverageName)
        {
            //var backgroundColor = (Color)editorFormatMap.GetProperties(coverageName)["BackgroundColor"];
            // use the visual studio way of waiting
            var allColors = fontsAndColorsHelper.GetColorsAsync(EditorTextMarkerFontAndColorCategory, new[] { coverageName }).GetAwaiter().GetResult();
            return allColors.FirstOrDefault();
            //var foregroundColor = (Color)editorFormatMap.GetProperties(coverageName)["ForegroundColor"];
            //return new ItemCoverageColours(foregroundColor, backgroundColor);
        }
    }

    internal class CoverageColoursChangedMessage
    {
        public ICoverageColours CoverageColours { get; }

        public CoverageColoursChangedMessage(ICoverageColours currentCoverageColours)
        {
            this.CoverageColours = currentCoverageColours;
        }
    }
}