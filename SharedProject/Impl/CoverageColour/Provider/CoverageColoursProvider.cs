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

        #region irrelevant as not using as a marker
        public int GetDefaultLineStyle(COLORINDEX[] piLineColor, LINESTYLE[] piLineIndex)
        {
            piLineIndex[0] = LINESTYLE.LI_SOLID;
            switch (this._type)
            {
                case CoverageType.Covered:
                    piLineColor[0] = COLORINDEX.CI_GREEN;
                    break;
                case CoverageType.NotCovered:
                    piLineColor[0] = COLORINDEX.CI_RED;
                    break;
                case CoverageType.Partial:
                    piLineColor[0] = COLORINDEX.CI_BLUE;
                    break;
                default:
                    
                    return -2147467263;
            }
            return 0;
        }
        #endregion

        #region not hit
        #region not hit as not using as a marker
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
          int iLineHeight)
        {
            return 0;
        }

        public int GetMergingPriority(out int piMergingPriority)
        {
            piMergingPriority = 100;
            return 0;
        }
        #endregion
        public int GetDescription(out string pbstrDesc)
        {
            pbstrDesc = "Coverage Description goes here";
            return 0;
        }

        #endregion
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

        public int GetVisualStyle(out uint pdwVisualFlags)
        {
            pdwVisualFlags = 8194U;
            return 0;
        }

        public int GetDefaultColors(COLORINDEX[] piForeground, COLORINDEX[] piBackground)
        {
            switch (this._type)
            {
                case CoverageType.Covered:
                    piBackground[0] = COLORINDEX.CI_GREEN;
                    piForeground[0] = COLORINDEX.CI_FIRSTFIXEDCOLOR;
                    break;
                case CoverageType.NotCovered:
                    piBackground[0] = COLORINDEX.CI_RED;
                    piForeground[0] = COLORINDEX.CI_WHITE;
                    break;
                case CoverageType.Partial:
                    piBackground[0] = COLORINDEX.CI_BLUE;
                    piForeground[0] = COLORINDEX.CI_WHITE;
                    break;
                default:
                    return -2147467263;
            }
            return 0;
        }

        public int GetColorData(int cdElement, out uint crColor)
        {
            crColor = 0U;
            switch (this._type)
            {
                case CoverageType.Covered:
                    switch (cdElement)
                    {
                        case 0:
                            crColor = this.ColorToRgb(Colors.Black);
                            break;
                        case 1:
                            crColor = this.ColorToRgb(Color.FromArgb(224, 237, 253,0));
                            break;
                        default:
                            return -2147467259;
                    }
                    break;
                case CoverageType.NotCovered:
                    switch (cdElement)
                    {
                        case 0:
                            crColor = this.ColorToRgb(Colors.Black);
                            break;
                        case 1:
                            crColor = this.ColorToRgb(Color.FromArgb(230, 176, 165,0));
                            break;
                        default:
                            return -2147467259;
                    }
                    break;
                case CoverageType.Partial:
                    switch (cdElement)
                    {
                        case 0:
                            crColor = this.ColorToRgb(Colors.Black);
                            break;
                        case 1:
                            crColor = this.ColorToRgb(Color.FromArgb((int)byte.MaxValue, 239, 206,0));
                            break;
                        default:
                            return -2147467259;
                    }
                    break;
                default:
                    return -2147467263;
            }
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
            switch (this._type)
            {
                case CoverageType.Covered:
                    pbstrNonLocalizeName = "Coverage Touched Area";
                    break;
                case CoverageType.NotCovered:
                    pbstrNonLocalizeName = "Coverage Not Touched Area";
                    break;
                case CoverageType.Partial:
                    pbstrNonLocalizeName = "Coverage Partially Touched Area";
                    break;
                default:
                    pbstrNonLocalizeName = string.Empty;
                    return -2147467263;
            }
            return 0;
        }


    }

    internal class CoverageColours : ICoverageColours
    {
        public System.Windows.Media.Color CoverageTouchedArea { get; set; }

        public CoverageColours(Color coverageTouchedArea, Color coverageNotTouchedArea, Color coveragePartiallyTouchedArea)
        {
            CoverageTouchedArea = coverageTouchedArea;
            CoverageNotTouchedArea = coverageNotTouchedArea;
            CoveragePartiallyTouchedArea = coveragePartiallyTouchedArea;
        }

        public System.Windows.Media.Color CoverageNotTouchedArea { get; }

        public System.Windows.Media.Color CoveragePartiallyTouchedArea { get; }

        internal bool AreEqual(CoverageColours lastCoverageColours)
        {
            return CoverageTouchedArea == lastCoverageColours.CoverageTouchedArea &&
                CoverageNotTouchedArea == lastCoverageColours.CoverageNotTouchedArea &&
                CoveragePartiallyTouchedArea == lastCoverageColours.CoveragePartiallyTouchedArea;
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

        private readonly CoverageMarkerType _covTouched;
        private readonly CoverageMarkerType _covNotTouched;
        private readonly CoverageMarkerType _covPartiallyTouched;
        private readonly IEditorFormatMap editorFormatMap;
        private readonly IEventAggregator eventAggregator;
        private CoverageColours lastCoverageColours;

        [ImportingConstructor]
        public CoverageColoursProvider(
            IEditorFormatMapService editorFormatMapService,
            IEventAggregator eventAggregator
        )
        {
            this._covTouched = new CoverageMarkerType(CoverageType.Covered);
            this._covNotTouched = new CoverageMarkerType(CoverageType.NotCovered);
            this._covPartiallyTouched = new CoverageMarkerType(CoverageType.Partial);

            editorFormatMap = editorFormatMapService.GetEditorFormatMap("text");
            editorFormatMap.FormatMappingChanged += EditorFormatMap_FormatMappingChanged;
            this.eventAggregator = eventAggregator;
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
            markerType = markerGuid.Equals(TouchedGuid) ? this._covTouched : 
                (markerGuid.Equals(PartiallyTouchedGuid) ? this._covPartiallyTouched : 
                this._covNotTouched);
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