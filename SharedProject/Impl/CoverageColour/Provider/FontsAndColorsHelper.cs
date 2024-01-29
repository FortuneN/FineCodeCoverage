using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Impl
{
    [Export(typeof(IFontsAndColorsHelper))]
    internal class FontsAndColorsHelper : IFontsAndColorsHelper
    {
        private readonly AsyncLazy<IVsFontAndColorStorage> lazyIVsFontAndColorStorage;
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

        public async System.Threading.Tasks.Task<List<IItemCoverageColours>> GetColorsAsync(Guid category, IEnumerable<string> names)
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
                        var bgColor = ParseColor(touchAreaInfo[0].crBackground);
                        var fgColor = ParseColor(touchAreaInfo[0].crForeground);
                        return new ItemCoverageColours(fgColor, bgColor);

                    }
                    return null;
                }
                colors = names.Select(name => GetColor(name)).Where(color => color != null).ToList();
            }

            fontAndColorStorage.CloseCategory();
            return colors;
        }
    }

}
