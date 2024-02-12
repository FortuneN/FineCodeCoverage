using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using Microsoft.VisualStudio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ComponentModel.Composition;
using Microsoft.VisualStudio.TextManager.Interop;
using FineCodeCoverage.Core.Utilities.VsThreading;
using System.Threading.Tasks;
using FineCodeCoverage.Core.Utilities;

namespace FineCodeCoverage.Editor.Management
{
    [Export(typeof(IFontsAndColorsHelper))]
    internal class FontsAndColorsHelper : IFontsAndColorsHelper
    {
        private readonly uint storeFlags = (uint)(__FCSTORAGEFLAGS.FCSF_READONLY | __FCSTORAGEFLAGS.FCSF_LOADDEFAULTS | __FCSTORAGEFLAGS.FCSF_NOAUTOCOLORS | __FCSTORAGEFLAGS.FCSF_PROPAGATECHANGES);
        private readonly System.IServiceProvider serviceProvider;
        private readonly IThreadHelper threadHelper;

        [ImportingConstructor]
        public FontsAndColorsHelper(
            [Import(typeof(SVsServiceProvider))] System.IServiceProvider serviceProvider,
            IThreadHelper threadHelper
        )
        {
            this.serviceProvider = serviceProvider;
            this.threadHelper = threadHelper;
        }

        private System.Windows.Media.Color ParseColor(uint color)
        {
            var dcolor = System.Drawing.ColorTranslator.FromOle(Convert.ToInt32(color));
            return System.Windows.Media.Color.FromArgb(dcolor.A, dcolor.R, dcolor.G, dcolor.B);
        }

        private IVsFontAndColorStorage vsFontAndColorStorage;
        private async Task<IVsFontAndColorStorage> GetVsFontAndColorStorageAsync()
        {
            if (vsFontAndColorStorage == null)
            {
                await threadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                vsFontAndColorStorage = serviceProvider.GetService<IVsFontAndColorStorage>();
            }
            return vsFontAndColorStorage;
        }

        public async System.Threading.Tasks.Task<List<IFontAndColorsInfo>> GetInfosAsync(Guid category, IEnumerable<string> names)
        {
            var infos = new List<IFontAndColorsInfo>();
            var fontAndColorStorage = await GetVsFontAndColorStorageAsync();
            await threadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            var success = fontAndColorStorage.OpenCategory(ref category, storeFlags);
            if (success == VSConstants.S_OK)
            {
                // https://github.com/microsoft/vs-threading/issues/993
                IFontAndColorsInfo GetInfo(string displayName)
                {
                    var touchAreaInfo = new ColorableItemInfo[1];
                    var getItemSuccess = fontAndColorStorage.GetItem(displayName, touchAreaInfo);

                    if (getItemSuccess == VSConstants.S_OK)
                    {
                        var bgColor = ParseColor(touchAreaInfo[0].crBackground);
                        var fgColor = ParseColor(touchAreaInfo[0].crForeground);
                        return new FontAndColorsInfo(new ItemCoverageColours(fgColor, bgColor), touchAreaInfo[0].dwFontFlags == (uint)FONTFLAGS.FF_BOLD);
                    }
                    return null;
                }
                infos = names.Select(name => GetInfo(name)).Where(color => color != null).ToList();
            }

            fontAndColorStorage.CloseCategory();
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            return infos;
        }
    }
}
