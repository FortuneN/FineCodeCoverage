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
        private readonly IEventAggregator eventAggregator;
        private readonly IFontsAndColorsHelper fontsAndColorsHelper;
        
        private readonly IThreadHelper threadHelper;
        private CoverageColours lastCoverageColours;
        private ICoverageFontAndColorsCategoryItemNames coverageFontAndColorsCategoryItemNames;

        public ICoverageFontAndColorsCategoryItemNames CoverageFontAndColorsCategoryItemNames { set => coverageFontAndColorsCategoryItemNames = value; }

        private struct NameIndex
        {
            public NameIndex(string name, int index)
            {
                Name = name;
                Index = index;
            }
            public string Name { get; }
            public int Index { get; }
        }

        private class CategoryNameIndices
        {
            public CategoryNameIndices(Guid category)
            {
                Category = category;
            }
            public Guid Category { get; }
            public List<NameIndex> NameIndices { get; } = new List<NameIndex>();
        }

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


        private List<CategoryNameIndices> GetCategoryNameIndices()
        {
            var lookup = new Dictionary<Guid,CategoryNameIndices>();
            
            var items = new List<(FontAndColorsCategoryItemName, int)>
            {
                (coverageFontAndColorsCategoryItemNames.Covered, 0),
                (coverageFontAndColorsCategoryItemNames.NotCovered, 1),
                (coverageFontAndColorsCategoryItemNames.PartiallyCovered, 2),
                (coverageFontAndColorsCategoryItemNames.Dirty,3),
                (coverageFontAndColorsCategoryItemNames.NewLines,4),
                 (coverageFontAndColorsCategoryItemNames.NotIncluded,5)
            };

            foreach(var item in items)
            {
               if(!lookup.TryGetValue(item.Item1.Category, out var categoryNameIndices))
                {
                    categoryNameIndices = new CategoryNameIndices(item.Item1.Category);
                    lookup.Add(item.Item1.Category, categoryNameIndices);
                }
               categoryNameIndices.NameIndices.Add(new NameIndex(item.Item1.ItemName, item.Item2));
            }
            return lookup.Values.ToList();
            
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
                fromFontsAndColors[0],//touched
                fromFontsAndColors[1],//not touched
                fromFontsAndColors[2],//partial
                fromFontsAndColors[3],//dirty
                fromFontsAndColors[4],//newlines
                fromFontsAndColors[5]//not included
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
            var allCategoryNameIndices = GetCategoryNameIndices();
            var tasks = new List<Task<List<(IFontAndColorsInfo, int)>>>();
            foreach(var categoryNameIndices in allCategoryNameIndices)
            {
                tasks.Add(GetAsync(categoryNameIndices));
            }
            var results = await Task.WhenAll(tasks);
            return results.SelectMany(r=> r).OrderBy(r=>r.Item2).Select(r=>r.Item1).ToList();
        }

        private async Task<List<(IFontAndColorsInfo,int)>> GetAsync(CategoryNameIndices categoryNameIndices)
        {
            var fontAndColorsInfos = await fontsAndColorsHelper.GetInfosAsync(
                    categoryNameIndices.Category,
                    categoryNameIndices.NameIndices.Select(ni => ni.Name).ToArray());
            return fontAndColorsInfos.Select((fontAndColorsInfo, i) => (fontAndColorsInfo, categoryNameIndices.NameIndices[i].Index)).ToList();
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
