using System;
using System.ComponentModel.Composition;
using FineCodeCoverage.Options;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Threading;
using Task = System.Threading.Tasks.Task;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(ICoverageColoursProvider))]
    [Export(typeof(ICoverageColours))]
    internal class CoverageColorProvider : ICoverageColoursProvider, ICoverageColours
    {
        private readonly ILogger logger;
        private readonly uint storeFlags = (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS | __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS | __FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES);
        private readonly System.Windows.Media.Color defaultCoverageTouchedArea = System.Windows.Media.Colors.Green;
        private readonly System.Windows.Media.Color defaultCoverageNotTouchedArea = System.Windows.Media.Colors.Red;
        private readonly System.Windows.Media.Color defaultCoveragePartiallyTouchedArea = System.Windows.Media.Color.FromRgb(255, 165, 0);
        private Guid categoryWithCoverage = Guid.Parse("ff349800-ea43-46c1-8c98-878e78f46501");
        private AsyncLazy<IVsFontAndColorStorage> lazyIVsFontAndColorStorage;
        private bool coverageColoursFromFontsAndColours;
        private bool canUseFontsAndColours = true;
        public System.Windows.Media.Color CoverageTouchedArea { get; set; }

        public System.Windows.Media.Color CoverageNotTouchedArea { get; set; }

        public System.Windows.Media.Color CoveragePartiallyTouchedArea { get; set; }

        public event EventHandler<EventArgs> ColoursChanged;

        [ImportingConstructor]
        public CoverageColorProvider(
            [Import(typeof(SVsServiceProvider))] IServiceProvider serviceProvider, 
            IAppOptionsProvider appOptionsProvider,
            ILogger logger
        )
        {
            lazyIVsFontAndColorStorage = new AsyncLazy<IVsFontAndColorStorage>(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                return (IVsFontAndColorStorage)serviceProvider.GetService(typeof(IVsFontAndColorStorage));
            }, ThreadHelper.JoinableTaskFactory);

            coverageColoursFromFontsAndColours = appOptionsProvider.Get().CoverageColoursFromFontsAndColours;
            appOptionsProvider.OptionsChanged += AppOptionsProvider_OptionsChanged;
            this.logger = logger;
            DetermineColors();
        }

        private void AppOptionsProvider_OptionsChanged(IAppOptions appOptions)
        {
            coverageColoursFromFontsAndColours = appOptions.CoverageColoursFromFontsAndColours;
            DetermineColors();
        }

        private void DetermineColors()
        {
            ThreadHelper.JoinableTaskFactory.Run(DetermineColorsAsync);
        }

        private async Task DetermineColorsAsync()
        {
            if (coverageColoursFromFontsAndColours && canUseFontsAndColours)
            {
                await UpdateColoursFromFontsAndColorsAsync();
            }
            else
            {
                UseDefaultColours();
            }
        }

        private void UseDefaultColours()
        {
            SetColors(defaultCoverageTouchedArea, defaultCoverageNotTouchedArea, defaultCoveragePartiallyTouchedArea);
        }

        public async Task PrepareAsync()
        {
            await DetermineColorsAsync();
        }

        private async Task UpdateColoursFromFontsAndColorsAsync()
        {
            var fontAndColorStorage = await lazyIVsFontAndColorStorage.GetValueAsync();
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
                    var newCoverageTouchedArea = GetColor("Coverage Touched Area");
                    var newCoverageNotTouchedArea = GetColor("Coverage Not Touched Area");
                    var newCoveragePartiallyTouchedArea = GetColor("Coverage Partially Touched Area");
                    SetColors(newCoverageTouchedArea, newCoverageNotTouchedArea, newCoveragePartiallyTouchedArea);
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

        private void SetColors(
            System.Windows.Media.Color coverageTouchedArea,
            System.Windows.Media.Color coverageNotTouchedArea,
            System.Windows.Media.Color coveragePartiallyTouchedArea
        )
        {
            var fontsAndColorsChanged = FontsAndColorsChanged(coverageTouchedArea, coverageNotTouchedArea, coveragePartiallyTouchedArea);
            if (fontsAndColorsChanged)
            {
                CoverageTouchedArea = coverageTouchedArea;
                CoverageNotTouchedArea = coverageNotTouchedArea;
                CoveragePartiallyTouchedArea = coveragePartiallyTouchedArea;

                ColoursChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        private bool FontsAndColorsChanged(
            System.Windows.Media.Color coverageTouchedArea,
            System.Windows.Media.Color coverageNotTouchedArea,
            System.Windows.Media.Color coveragePartiallyTouchedArea
        )
        {
            return !(CoverageTouchedArea == coverageTouchedArea && 
                CoverageNotTouchedArea == coverageNotTouchedArea && 
                CoveragePartiallyTouchedArea == coveragePartiallyTouchedArea);
        }

        private System.Windows.Media.Color ParseColor(uint color)
        {
            var dcolor = System.Drawing.ColorTranslator.FromOle(Convert.ToInt32(color));
            return System.Windows.Media.Color.FromArgb(dcolor.A, dcolor.R, dcolor.G, dcolor.B);
        }

    }

}