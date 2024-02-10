using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Core.Utilities.VsThreading;
using FineCodeCoverage.Engine.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;

namespace FineCodeCoverage.Editor.Management
{
    [Export(typeof(ICoverageColoursProvider))]
    [Export(typeof(IFontAndColorsInfosProvider))]
    internal class FontAndColorsInfosProvider : ICoverageColoursProvider, IFontAndColorsInfosProvider
    {
        private readonly Guid EditorTextMarkerFontAndColorCategory = new Guid("FF349800-EA43-46C1-8C98-878E78F46501");
        private readonly IEventAggregator eventAggregator;
        private readonly IFontsAndColorsHelper fontsAndColorsHelper;
        private readonly MarkerTypeNames markerTypeNames;
        private readonly IThreadHelper threadHelper;
        private CoverageColours lastCoverageColours;

        [ImportingConstructor]
        public FontAndColorsInfosProvider(
            IEventAggregator eventAggregator,
            IFontsAndColorsHelper fontsAndColorsHelper,
            MarkerTypeNames markerTypeNames,
            IThreadHelper threadHelper
        )
        {
            this.eventAggregator = eventAggregator;
            this.fontsAndColorsHelper = fontsAndColorsHelper;
            this.markerTypeNames = markerTypeNames;
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
                fromFontsAndColors[2]
            );
        }

        private List<IFontAndColorsInfo> GetItemCoverageInfosFromFontsAndColors()
        {
            return threadHelper.JoinableTaskFactory.Run(() =>
            {
                return fontsAndColorsHelper.GetInfosAsync(
                    EditorTextMarkerFontAndColorCategory,
                    new[] {
                        markerTypeNames.Covered,
                        markerTypeNames.NotCovered,
                        markerTypeNames.PartiallyCovered
                     }
                );
            });
        }

        public Dictionary<CoverageType, IFontAndColorsInfo> GetChangedFontAndColorsInfos()
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

        public Dictionary<CoverageType, IFontAndColorsInfo> GetFontAndColorsInfos()
        {
            return GetCoverageColoursIfRequired().GetChanges(null);
        }
    }

}
