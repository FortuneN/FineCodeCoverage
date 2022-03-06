using System;
using System.ComponentModel.Composition;
using FineCodeCoverage.Options;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ICoverageColoursProvider))]
    [Export(typeof(ICoverageColours))]
    internal class CoverageColorProvider : ICoverageColoursProvider, ICoverageColours
    {
        private readonly IVsFontAndColorStorage fontAndColorStorage;
        private readonly ILogger logger;
        private readonly uint storeFlags = (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS | __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS | __FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES);
        private readonly System.Windows.Media.Color defaultCoverageTouchedArea = System.Windows.Media.Colors.Green;
        private readonly System.Windows.Media.Color defaultCoverageNotTouchedArea = System.Windows.Media.Colors.Red;
        private readonly System.Windows.Media.Color defaultCoveragePartiallyTouchedArea = System.Windows.Media.Color.FromRgb(255, 165, 0);
        private Guid categoryWithCoverage = Guid.Parse("ff349800-ea43-46c1-8c98-878e78f46501");
        private bool coverageColoursFromFontsAndColours;
        private bool dirty = true;
        private bool canUseFontsAndColours = true;
        public System.Windows.Media.Color CoverageTouchedArea { get; set; }

        public System.Windows.Media.Color CoverageNotTouchedArea { get; set; }

        public System.Windows.Media.Color CoveragePartiallyTouchedArea { get; set; }

        

        [ImportingConstructor]
        public CoverageColorProvider(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, 
            IAppOptionsProvider appOptionsProvider,
            ILogger logger
        )
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            fontAndColorStorage = (IVsFontAndColorStorage)serviceProvider.GetService(typeof(IVsFontAndColorStorage));
            Assumes.Present(fontAndColorStorage);
            coverageColoursFromFontsAndColours = appOptionsProvider.Get().CoverageColoursFromFontsAndColours;
            UseDefaultColoursIfNotFontsAndColours();
            appOptionsProvider.OptionsChanged += AppOptionsProvider_OptionsChanged;
            this.logger = logger;
        }

        private void AppOptionsProvider_OptionsChanged(IAppOptions appOptions)
        {
            coverageColoursFromFontsAndColours = appOptions.CoverageColoursFromFontsAndColours;
            UseDefaultColoursIfNotFontsAndColours();
            dirty = true;
        }

        private void UseDefaultColoursIfNotFontsAndColours()
        {
            if (!coverageColoursFromFontsAndColours)
            {
                UseDefaultColours();
            }
        }

        private void UseDefaultColours()
        {
            CoverageTouchedArea = defaultCoverageTouchedArea;
            CoverageNotTouchedArea = defaultCoverageNotTouchedArea;
            CoveragePartiallyTouchedArea = defaultCoveragePartiallyTouchedArea;
        }

        public async Task PrepareAsync()
        {
            if (coverageColoursFromFontsAndColours && canUseFontsAndColours && dirty)
            {
                await UpdateColoursFromFontsAndColorsAsync();
            }
            dirty = false;
        }

        private async Task UpdateColoursFromFontsAndColorsAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var success = fontAndColorStorage.OpenCategory(ref categoryWithCoverage, storeFlags);
            var usedFontsAndColors = false;
            if (success == VSConstants.S_OK)
            {
                // https://github.com/microsoft/vs-threading/issues/993
                System.Windows.Media.Color GetColor(string displayName)
                {
                    var touchAreaInfo = new ColorableItemInfo[1];
                    var getItemSuccess = fontAndColorStorage.GetItem(displayName, touchAreaInfo);
                    if (getItemSuccess == VSConstants.S_OK)
                    {
                        return ParseColor(touchAreaInfo[0].crBackground);
                    }
                    throw new NotSupportedException($"{getItemSuccess}");
                }
                try
                {
                    // https://developercommunity.visualstudio.com/t/fonts-and-colors-coverage-settings-available-in-vs/1683898
                    CoverageTouchedArea = GetColor("Coverage Touched Area");
                    CoverageNotTouchedArea = GetColor("Coverage Not Touched Area");
                    CoveragePartiallyTouchedArea = GetColor("Coverage Partially Touched Area");
                    usedFontsAndColors = true;
                    
                }catch(NotSupportedException)
                {
                    logger.Log("No coverage settings available from Fonts and Colors");
                }
            }
            
            fontAndColorStorage.CloseCategory();
            if (!usedFontsAndColors)
            {
                canUseFontsAndColours = false;
                UseDefaultColours();
            }
            
        }

        private System.Windows.Media.Color ParseColor(uint color)
        {
            var dcolor = System.Drawing.ColorTranslator.FromOle(Convert.ToInt32(color));
            return System.Windows.Media.Color.FromArgb(dcolor.A, dcolor.R, dcolor.G, dcolor.B);
        }

    }

}