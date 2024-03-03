using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading.Tasks;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Core.Utilities.VsThreading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

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
            System.Drawing.Color dcolor = System.Drawing.ColorTranslator.FromOle(Convert.ToInt32(color));
            return System.Windows.Media.Color.FromArgb(dcolor.A, dcolor.R, dcolor.G, dcolor.B);
        }

        private IVsFontAndColorStorage vsFontAndColorStorage;
        private async Task<IVsFontAndColorStorage> GetVsFontAndColorStorageAsync()
        {
            if (this.vsFontAndColorStorage == null)
            {
                await this.threadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.vsFontAndColorStorage = this.serviceProvider.GetService<IVsFontAndColorStorage>();
            }

            return this.vsFontAndColorStorage;
        }

        public async System.Threading.Tasks.Task<List<IFontAndColorsInfo>> GetInfosAsync(Guid category, IEnumerable<string> names)
        {
            var infos = new List<IFontAndColorsInfo>();
            IVsFontAndColorStorage fontAndColorStorage = await this.GetVsFontAndColorStorageAsync();
            await this.threadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
#pragma warning disable VSTHRD010 // Invoke single-threaded types on Main thread
            int success = fontAndColorStorage.OpenCategory(ref category, this.storeFlags);
            if (success == VSConstants.S_OK)
            {
                // https://github.com/microsoft/vs-threading/issues/993
                IFontAndColorsInfo GetInfo(string displayName)
                {
                    var touchAreaInfo = new ColorableItemInfo[1];
                    int getItemSuccess = fontAndColorStorage.GetItem(displayName, touchAreaInfo);

                    if (getItemSuccess == VSConstants.S_OK)
                    {
                        System.Windows.Media.Color bgColor = this.ParseColor(touchAreaInfo[0].crBackground);
                        System.Windows.Media.Color fgColor = this.ParseColor(touchAreaInfo[0].crForeground);
                        return new FontAndColorsInfo(new ItemCoverageColours(fgColor, bgColor), touchAreaInfo[0].dwFontFlags == (uint)FONTFLAGS.FF_BOLD);
                    }

                    return null;
                }

                infos = names.Select(name => GetInfo(name)).Where(color => color != null).ToList();
            }

            _ = fontAndColorStorage.CloseCategory();
#pragma warning restore VSTHRD010 // Invoke single-threaded types on Main thread
            return infos;
        }
    }
}
