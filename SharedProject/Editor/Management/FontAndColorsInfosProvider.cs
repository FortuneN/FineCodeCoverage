using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Editor.DynamicCoverage;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace FineCodeCoverage.Editor.Management
{
    [Export(typeof(ICoverageColoursProvider))]
    [Export(typeof(IFontAndColorsInfosProvider))]
    internal class FontAndColorsInfosProvider : ICoverageColoursProvider, IFontAndColorsInfosProvider
    {
        private readonly Guid EditorTextMarkerFontAndColorCategory = new Guid("FF349800-EA43-46C1-8C98-878E78F46501");
        private readonly Guid EditorMEFCategory = new Guid("75A05685-00A8-4DED-BAE5-E7A50BFA929A");
        private readonly IEventAggregator eventAggregator;
        private readonly IFontsAndColorsHelper fontsAndColorsHelper;
        private FontAndColorsItemNames fontAndColorsItemNames;
        private readonly IThreadHelper threadHelper;
        private CoverageColours lastCoverageColours;

        public FontAndColorsItemNames FontAndColorsItemNames { set => fontAndColorsItemNames = value; }

        [ImportingConstructor]
        public FontAndColorsInfosProvider(
            IEventAggregator eventAggregator,
            IFontsAndColorsHelper fontsAndColorsHelper,
            IThreadHelper threadHelper
        )
        {
            this.eventAggregator = eventAggregator;
            this.fontsAndColorsHelper = fontsAndColorsHelper;
            this.threadHelper = threadHelper;
        }


        public ICoverageColours GetCoverageColours()
        {
            return GetCoverageColoursIfRequired();
        }

        private CoverageColours GetCoverageColoursIfRequired()
        {
            if (lastCoverageColours == null)
            {
                lastCoverageColours = GetCoverageColoursFromFontsAndColors();
            }
           
            return lastCoverageColours;
        }

        private CoverageColours GetCoverageColoursFromFontsAndColors()
        {
            var fromFontsAndColors = GetItemCoverageInfosFromFontsAndColors();
            return new CoverageColours(
                fromFontsAndColors[0],
                fromFontsAndColors[1],
                fromFontsAndColors[2],
                fromFontsAndColors[3],
                fromFontsAndColors[4]
            );
        }

        private List<IFontAndColorsInfo> GetItemCoverageInfosFromFontsAndColors()
        {
            return threadHelper.JoinableTaskFactory.Run(() =>
            {
                return GetItemCoverageInfosFromFontsAndColorsAsync();
            });
        }

        private async Task<List<IFontAndColorsInfo>> GetItemCoverageInfosFromFontsAndColorsAsync()
        {
            var markerFontAndColorsInfos = await GetTextMarkerFontAndColorsInfosAsync();
            var mefFontAndColorsInfos = await GetMEFFontAndColorsInfosAsync();
            return markerFontAndColorsInfos.Concat(mefFontAndColorsInfos).ToList();
        }

        private Task<List<IFontAndColorsInfo>> GetMEFFontAndColorsInfosAsync()
        {
            return fontsAndColorsHelper.GetInfosAsync(
                    EditorMEFCategory,
                    new[] {
                        fontAndColorsItemNames.MEFItemNames.Dirty,
                        fontAndColorsItemNames.MEFItemNames.NewLines
                     }
                );
        }

        private Task<List<IFontAndColorsInfo>> GetTextMarkerFontAndColorsInfosAsync()
        {
            return fontsAndColorsHelper.GetInfosAsync(
                    EditorTextMarkerFontAndColorCategory,
                    new[] {
                        fontAndColorsItemNames.MarkerTypeNames.Covered,
                        fontAndColorsItemNames.MarkerTypeNames.NotCovered,
                        fontAndColorsItemNames.MarkerTypeNames.PartiallyCovered
                     }
                );
        }

        public Dictionary<DynamicCoverageType, IFontAndColorsInfo> GetChangedFontAndColorsInfos()
        {
            var currentColors = GetCoverageColoursFromFontsAndColors();
            var changes = currentColors.GetChanges(lastCoverageColours);
            lastCoverageColours = currentColors;
            if (changes.Any())
            {
                eventAggregator.SendMessage(new CoverageColoursChangedMessage());
            }
            return changes;
        }

        public Dictionary<DynamicCoverageType, IFontAndColorsInfo> GetFontAndColorsInfos()
        {
            return GetCoverageColoursIfRequired().GetChanges(null);
        }
    }

}
