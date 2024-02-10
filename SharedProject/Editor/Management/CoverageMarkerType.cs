using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.TextManager.Interop;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media;

namespace FineCodeCoverage.Editor.Management
{
    // Thiis will be converted internally to AllColorableItemInfo
    internal class CoverageMarkerType :
    IVsPackageDefinedTextMarkerType,
    IVsMergeableUIItem,
    IVsHiColorItem
    {
        private readonly string name;
        private readonly Color foregroundColor;
        private readonly Color backgroundColor;
        public CoverageMarkerType(string name, IItemCoverageColours itemCoverageColors)
        {
            this.name = name;
            this.foregroundColor = itemCoverageColors.Foreground;
            this.backgroundColor = itemCoverageColors.Background;
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
        [ExcludeFromCodeCoverage]
        public int GetMergingPriority(out int piMergingPriority)
        {
            piMergingPriority = 0x2000;
            return 0;
        }

        // This is not called.  Could be AllColorableItemInfo.bstrDescription - ( This feature is currently disabled )
        [ExcludeFromCodeCoverage]
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
            if (cdElement == 2)
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

}
