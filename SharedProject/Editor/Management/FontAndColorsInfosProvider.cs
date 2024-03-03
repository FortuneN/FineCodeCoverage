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

        public ICoverageFontAndColorsCategoryItemNames CoverageFontAndColorsCategoryItemNames { set => this.coverageFontAndColorsCategoryItemNames = value; }

        private readonly struct NameIndex
        {
            public NameIndex(string name, int index)
            {
                this.Name = name;
                this.Index = index;
            }
            public string Name { get; }
            public int Index { get; }
        }

        private class CategoryNameIndices
        {
            public CategoryNameIndices(Guid category) => this.Category = category;
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
                (this.coverageFontAndColorsCategoryItemNames.Covered, 0),
                (this.coverageFontAndColorsCategoryItemNames.NotCovered, 1),
                (this.coverageFontAndColorsCategoryItemNames.PartiallyCovered, 2),
                (this.coverageFontAndColorsCategoryItemNames.Dirty,3),
                (this.coverageFontAndColorsCategoryItemNames.NewLines,4),
                (this.coverageFontAndColorsCategoryItemNames.NotIncluded,5)
            };

            foreach((FontAndColorsCategoryItemName, int) item in items)
            {
                if(!lookup.TryGetValue(item.Item1.Category, out CategoryNameIndices categoryNameIndices))
                {
                    categoryNameIndices = new CategoryNameIndices(item.Item1.Category);
                    lookup.Add(item.Item1.Category, categoryNameIndices);
                }

                categoryNameIndices.NameIndices.Add(new NameIndex(item.Item1.ItemName, item.Item2));
            }

            return lookup.Values.ToList();
        }

        public ICoverageColours GetCoverageColours() => this.GetCoverageColoursIfRequired();

        private CoverageColours GetCoverageColoursIfRequired()
        {
            if (this.lastCoverageColours == null)
            {
                this.lastCoverageColours = this.GetCoverageColoursFromFontsAndColors();
            }
           
            return this.lastCoverageColours;
        }

        private CoverageColours GetCoverageColoursFromFontsAndColors()
        {
            List<IFontAndColorsInfo> fromFontsAndColors = this.GetItemCoverageInfosFromFontsAndColors();
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
            => this.threadHelper.JoinableTaskFactory.Run(() => this.GetItemCoverageInfosFromFontsAndColorsAsync());

        private async Task<List<IFontAndColorsInfo>> GetItemCoverageInfosFromFontsAndColorsAsync()
        {
            List<CategoryNameIndices> allCategoryNameIndices = this.GetCategoryNameIndices();
            var tasks = new List<Task<List<(IFontAndColorsInfo, int)>>>();
            foreach(CategoryNameIndices categoryNameIndices in allCategoryNameIndices)
            {
                tasks.Add(this.GetAsync(categoryNameIndices));
            }

            List<(IFontAndColorsInfo, int)>[] results = await Task.WhenAll(tasks);
            return results.SelectMany(r=> r).OrderBy(r=>r.Item2).Select(r=>r.Item1).ToList();
        }

        private async Task<List<(IFontAndColorsInfo,int)>> GetAsync(CategoryNameIndices categoryNameIndices)
        {
            List<IFontAndColorsInfo> fontAndColorsInfos = await this.fontsAndColorsHelper.GetInfosAsync(
                    categoryNameIndices.Category,
                    categoryNameIndices.NameIndices.Select(ni => ni.Name).ToArray());
            return fontAndColorsInfos.Select((fontAndColorsInfo, i) => (fontAndColorsInfo, categoryNameIndices.NameIndices[i].Index)).ToList();
        }

        public Dictionary<DynamicCoverageType, IFontAndColorsInfo> GetChangedFontAndColorsInfos()
        {
            CoverageColours currentColors = this.GetCoverageColoursFromFontsAndColors();
            Dictionary<DynamicCoverageType, IFontAndColorsInfo> changes = currentColors.GetChanges(this.lastCoverageColours);
            this.lastCoverageColours = currentColors;
            if (changes.Any())
            {
                this.eventAggregator.SendMessage(new CoverageColoursChangedMessage());
            }

            return changes;
        }

        public Dictionary<DynamicCoverageType, IFontAndColorsInfo> GetFontAndColorsInfos() 
            => this.GetCoverageColoursIfRequired().GetChanges(null);
    }
}
