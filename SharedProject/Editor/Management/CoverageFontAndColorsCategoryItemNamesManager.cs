using System;
using System.ComponentModel.Composition;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Editor.Management
{
    [Export(typeof(ICoverageFontAndColorsCategoryItemNames))]
    [Export(typeof(ICoverageFontAndColorsCategoryItemNamesManager))]
    internal class CoverageFontAndColorsCategoryItemNamesManager : ICoverageFontAndColorsCategoryItemNames, ICoverageFontAndColorsCategoryItemNamesManager
    {
        private readonly Guid EditorTextMarkerFontAndColorCategory = new Guid("FF349800-EA43-46C1-8C98-878E78F46501");
        private readonly Guid EditorMEFCategory = new Guid("75A05685-00A8-4DED-BAE5-E7A50BFA929A");
        private readonly bool hasCoverageMarkers;
        private readonly IAppOptionsProvider appOptionsProvider;
        private FCCEditorFormatDefinitionNames fCCEditorFormatDefinitionNames;
        private bool usingEnterprise = false;
        private bool initialized = false;

        public event EventHandler Changed;

        [ImportingConstructor]
        public CoverageFontAndColorsCategoryItemNamesManager(
           IVsHasCoverageMarkersLogic vsHasCoverageMarkersLogic,
            IAppOptionsProvider appOptionsProvider
        )
        {
            appOptionsProvider.OptionsChanged += this.AppOptionsProvider_OptionsChanged;
            this.hasCoverageMarkers = vsHasCoverageMarkersLogic.HasCoverageMarkers();
            this.appOptionsProvider = appOptionsProvider;
        }

        private void AppOptionsProvider_OptionsChanged(IAppOptions appOptions)
        {
            if (this.initialized)
            {
                this.ReactToAppOptionsChanging(appOptions);
            }
        }

        private void ReactToAppOptionsChanging(IAppOptions appOptions)
        {
            bool preUsingEnterprise = this.usingEnterprise;
            this.Set(() => appOptions.UseEnterpriseFontsAndColors);
            if (this.usingEnterprise != preUsingEnterprise)
            {
                Changed?.Invoke(this, new EventArgs());
            }
        }

        public void Initialize(FCCEditorFormatDefinitionNames fCCEditorFormatDefinitionNames)
        {
            this.fCCEditorFormatDefinitionNames = fCCEditorFormatDefinitionNames;
            this.Set();
            this.initialized = true;
        }

        private void Set() => this.Set(() => this.appOptionsProvider.Get().UseEnterpriseFontsAndColors);

        private void Set(Func<bool> getUseEnterprise)
        {
            if (!this.hasCoverageMarkers)
            {
                this.SetMarkersFromFCC();
            }
            else
            {

                this.SetPossiblyEnterprise(getUseEnterprise());
            }

            this.SetFCCOnly();
        }

        private void SetPossiblyEnterprise(bool useEnterprise)
        {
            this.usingEnterprise = useEnterprise;
            if (useEnterprise)
            {
                this.SetMarkersFromEnterprise();
            }
            else
            {
                this.SetMarkersFromFCC();
            }
        }

        private void SetFCCOnly()
        {
            this.NewLines = this.CreateMef(this.fCCEditorFormatDefinitionNames.NewLines);
            this.Dirty = this.CreateMef(this.fCCEditorFormatDefinitionNames.Dirty);
            this.NotIncluded = this.CreateMef(this.fCCEditorFormatDefinitionNames.NotIncluded);
        }

        private void SetMarkersFromFCC()
        {
            this.Covered = this.CreateMef(this.fCCEditorFormatDefinitionNames.Covered);
            this.NotCovered = this.CreateMef(this.fCCEditorFormatDefinitionNames.NotCovered);
            this.PartiallyCovered = this.CreateMef(this.fCCEditorFormatDefinitionNames.PartiallyCovered);
        }

        private void SetMarkersFromEnterprise()
        {
            this.Covered = this.CreateEnterprise(MarkerTypeNames.Covered);
            this.NotCovered = this.CreateEnterprise(MarkerTypeNames.NotCovered);
            this.PartiallyCovered = this.CreateEnterprise(MarkerTypeNames.PartiallyCovered);
        }

        private FontAndColorsCategoryItemName CreateMef(string itemName)
            => new FontAndColorsCategoryItemName(itemName, this.EditorMEFCategory);

        private FontAndColorsCategoryItemName CreateEnterprise(string itemName)
            => new FontAndColorsCategoryItemName(itemName, this.EditorTextMarkerFontAndColorCategory);

        public FontAndColorsCategoryItemName Covered { get; private set; }
        public FontAndColorsCategoryItemName NotCovered { get; private set; }
        public FontAndColorsCategoryItemName PartiallyCovered { get; private set; }
        public FontAndColorsCategoryItemName NewLines { get; private set; }
        public FontAndColorsCategoryItemName Dirty { get; private set; }

        public FontAndColorsCategoryItemName NotIncluded { get; private set; }
        public ICoverageFontAndColorsCategoryItemNames CategoryItemNames => this;
    }
}
