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

namespace FineCodeCoverage.Impl
{
    internal static class EnterpriseFontsAndColorsNames
    {
        public const string CoverageTouchedArea = "Coverage Touched Area";
        public const string CoverageNotTouchedArea = "Coverage Not Touched Area";
        public const string CoveragePartiallyTouchedArea = "Coverage Partially Touched Area";
    }

    internal interface IFontsAndColorsHelper
    {
        System.Threading.Tasks.Task<List<System.Windows.Media.Color>> GetColorsAsync(Guid category, IEnumerable<string> names);
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

        public async System.Threading.Tasks.Task<List<System.Windows.Media.Color>> GetColorsAsync(Guid category,IEnumerable<string> names)
        {
            var colors = new List<System.Windows.Media.Color>();
            var fontAndColorStorage = await lazyIVsFontAndColorStorage.GetValueAsync();
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var success = fontAndColorStorage.OpenCategory(ref category, storeFlags);
            if (success == VSConstants.S_OK)
            {
                // https://github.com/microsoft/vs-threading/issues/993
                System.Windows.Media.Color? GetColor(string displayName)
                {
                    var touchAreaInfo = new ColorableItemInfo[1];
                    var getItemSuccess = fontAndColorStorage.GetItem(displayName, touchAreaInfo);
                    if (getItemSuccess == VSConstants.S_OK)
                    {
                        return ParseColor(touchAreaInfo[0].crBackground);
                    }
                    return null;
                }
                colors = names.Select(name => GetColor(name)).Where(color => color.HasValue).Select(color => color.Value).ToList();
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
        private readonly CoverageType _type;

        public CoverageMarkerType(CoverageType t)
        {
            this._type = t;
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
            piPriorityIndex = 300;
            switch (this._type)
            {
                case CoverageType.Covered:
                    piPriorityIndex = 300;
                    break;
                case CoverageType.NotCovered:
                    piPriorityIndex = 302;
                    break;
                case CoverageType.Partial:
                    piPriorityIndex = 301;
                    break;
            }
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
            switch (this._type)
            {
                case CoverageType.Covered:
                    pbstrDisplayName = EnterpriseFontsAndColorsNames.CoverageTouchedArea;
                    break;
                case CoverageType.NotCovered:
                    pbstrDisplayName = EnterpriseFontsAndColorsNames.CoverageNotTouchedArea;
                    break;
                case CoverageType.Partial:
                    pbstrDisplayName = EnterpriseFontsAndColorsNames.CoveragePartiallyTouchedArea;
                    break;
                default:
                    pbstrDisplayName = string.Empty;
                    return -2147467263;
            }
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

        private Color GetCoveredColorData(bool foreground)
        {
            return foreground ? Colors.Black: Colors.Green;
        }

        private Color GetNotCoveredColorData(bool foreground)
        {
            return foreground ? Colors.Black : Colors.Red;
        }

        private Color GetPartiallyCoveredColorData(bool foreground)
        {
            return foreground ? Colors.Black : Color.FromRgb(255, 165, 0);
        }

        public int GetColorData(int cdElement, out uint crColor)
        {
            crColor = 0U;
            if(cdElement == 2)
            {
                return -2147467259;
            }
            var foreground = cdElement == 0;
            Color color = default;
            switch (this._type)
            {
                case CoverageType.Covered:
                    color = GetCoveredColorData(foreground);
                    break;
                case CoverageType.NotCovered:
                    color = GetNotCoveredColorData(foreground);
                    break;
                case CoverageType.Partial:
                    color = GetPartiallyCoveredColorData(foreground);
                    break;
            }
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
        private System.Windows.Media.Color coverageTouchedArea;
        private System.Windows.Media.Color coverageNotTouchedArea;
        private System.Windows.Media.Color coveragePartiallyTouchedArea;

        public CoverageColours(Color coverageTouchedArea, Color coverageNotTouchedArea, Color coveragePartiallyTouchedArea)
        {
            this.coverageTouchedArea = coverageTouchedArea;
            this.coverageNotTouchedArea = coverageNotTouchedArea;
            this.coveragePartiallyTouchedArea = coveragePartiallyTouchedArea;
        }

        internal bool AreEqual(CoverageColours lastCoverageColours)
        {
            if (lastCoverageColours == null) return false;

            return coverageTouchedArea == lastCoverageColours.coverageTouchedArea &&
                coverageNotTouchedArea == lastCoverageColours.coverageNotTouchedArea &&
                coveragePartiallyTouchedArea == lastCoverageColours.coveragePartiallyTouchedArea;
        }

        public Color GetColor(CoverageType coverageType)
        {
            switch (coverageType)
            {
                case CoverageType.Partial:
                    return coveragePartiallyTouchedArea;
                case CoverageType.NotCovered:
                    return coverageNotTouchedArea;
                case CoverageType.Covered:
                    return coverageTouchedArea;
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
        private readonly bool shouldAddCoverageMarkers;
        private CoverageColours lastCoverageColours;

        [ImportingConstructor]
        public CoverageColoursProvider(
            IEditorFormatMapService editorFormatMapService,
            IEventAggregator eventAggregator,
            IShouldAddCoverageMarkersLogic shouldAddCoverageMarkersLogic
        )
        {
            this._covTouched = new CoverageMarkerType(CoverageType.Covered);
            this._covNotTouched = new CoverageMarkerType(CoverageType.NotCovered);
            this._covPartiallyTouched = new CoverageMarkerType(CoverageType.Partial);

            editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
            editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
            this.eventAggregator = eventAggregator;
            shouldAddCoverageMarkers = shouldAddCoverageMarkersLogic.ShouldAddCoverageMarkers();
        }

        private void EditorFormatMap_FormatMappingChanged(object sender, FormatItemsEventArgs e)
        {
            var coverageChanged = e.ChangedItems.Any(c => 
                c == EnterpriseFontsAndColorsNames.CoverageTouchedArea || 
                c == EnterpriseFontsAndColorsNames.CoverageNotTouchedArea || 
                c == EnterpriseFontsAndColorsNames.CoveragePartiallyTouchedArea
            );
            if (coverageChanged)
            {
                var currentCoverageColours = GetCoverageColoursFromEditorFormatMap();
                if (!currentCoverageColours.AreEqual(lastCoverageColours))
                {
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
            lastCoverageColours = GetCoverageColoursFromEditorFormatMap();
            return lastCoverageColours;
        }

        private CoverageColours GetCoverageColoursFromEditorFormatMap()
        {
            return new CoverageColours(
                GetBackgroundColor(EnterpriseFontsAndColorsNames.CoverageTouchedArea),
                GetBackgroundColor(EnterpriseFontsAndColorsNames.CoverageNotTouchedArea),
                GetBackgroundColor(EnterpriseFontsAndColorsNames.CoveragePartiallyTouchedArea)
            );
        }

        private Color GetBackgroundColor(string coverageName)
        {
            return (Color)editorFormatMap.GetProperties(coverageName)["BackgroundColor"];
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