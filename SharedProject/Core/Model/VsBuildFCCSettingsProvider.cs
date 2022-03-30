using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace FineCodeCoverage.Engine.Model
{
    [Export(typeof(IVsBuildFCCSettingsProvider))]
    internal class VsBuildFCCSettingsProvider : IVsBuildFCCSettingsProvider
    {
        private readonly IServiceProvider serviceProvider;
        private const string FCCSettingsElementName = "FineCodeCoverage";

        [ImportingConstructor]
        public VsBuildFCCSettingsProvider(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider
        )
        {
            this.serviceProvider = serviceProvider;
        }

        public async Task<XElement> GetSettingsAsync(Guid projectId)
        {
            XElement fccSettingsElement = null;
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var vsSolution = serviceProvider.GetService(typeof(SVsSolution)) as IVsSolution;
            Assumes.Present(vsSolution);
            if (vsSolution.GetProjectOfGuid(ref projectId, out var vsHierarchy) == VSConstants.S_OK)
            {
                if (vsHierarchy is IVsBuildPropertyStorage vsBuildPropertyStorage)
                {
                    if (vsBuildPropertyStorage.GetPropertyValue(FCCSettingsElementName, null, 1, out string value) == VSConstants.S_OK)
                    {
                        if (!string.IsNullOrEmpty(value))
                        {
                            try
                            {
                                fccSettingsElement = XElement.Parse($"<FineCodeCoverage>{value}</FineCodeCoverage>");
                            }
                            catch { }
                        }

                    }
                }
            }
            return fccSettingsElement;
        }
    }

}
