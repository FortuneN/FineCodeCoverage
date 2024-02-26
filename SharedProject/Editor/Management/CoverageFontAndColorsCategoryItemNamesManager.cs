using FineCodeCoverage.Options;
using System;
using System.ComponentModel.Composition;

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
            appOptionsProvider.OptionsChanged += AppOptionsProvider_OptionsChanged;
            this.hasCoverageMarkers = vsHasCoverageMarkersLogic.HasCoverageMarkers();
            this.appOptionsProvider = appOptionsProvider;
        }

        private void AppOptionsProvider_OptionsChanged(IAppOptions appOptions)
        {
            if (initialized)
            {
                var preUsingEnterprise = usingEnterprise;
                Set(() => appOptions.UseEnterpriseFontsAndColors);
                if(usingEnterprise != preUsingEnterprise)
                {
                    Changed?.Invoke(this, new EventArgs());
                }
            }
        }

        public void Initialize(FCCEditorFormatDefinitionNames fCCEditorFormatDefinitionNames)
        {
            this.fCCEditorFormatDefinitionNames = fCCEditorFormatDefinitionNames;
            Set();
            initialized = true;
        }

        private void Set()
        {
            Set(() => appOptionsProvider.Get().UseEnterpriseFontsAndColors);
        }

        private void Set(Func<bool> getUseEnterprise)
        {
            if (!hasCoverageMarkers)
            {
                SetMarkersFromFCC();
            }
            else
            {

                SetPossiblyEnterprise(getUseEnterprise());
            }

            SetFCCOnly();
        }

        private void SetPossiblyEnterprise(bool useEnterprise)
        {
            usingEnterprise = useEnterprise;
            if (useEnterprise)
            {
                SetMarkersFromEnterprise();
            }
            else
            {
                SetMarkersFromFCC();
            }
        }

        private void SetFCCOnly()
        {
            NewLines = CreateMef(fCCEditorFormatDefinitionNames.NewLines);
            Dirty = CreateMef(fCCEditorFormatDefinitionNames.Dirty);
            NotIncluded = CreateMef(fCCEditorFormatDefinitionNames.NotIncluded);
        }

        private void SetMarkersFromFCC()
        {
            Covered = CreateMef(fCCEditorFormatDefinitionNames.Covered);
            NotCovered = CreateMef(fCCEditorFormatDefinitionNames.NotCovered);
            PartiallyCovered = CreateMef(fCCEditorFormatDefinitionNames.PartiallyCovered);
        }

        private void SetMarkersFromEnterprise()
        {
            Covered = CreateEnterprise(MarkerTypeNames.Covered);
            NotCovered = CreateEnterprise(MarkerTypeNames.NotCovered);
            PartiallyCovered = CreateEnterprise(MarkerTypeNames.PartiallyCovered);
        }

        private FontAndColorsCategoryItemName CreateMef(string itemName)
        {
            return new FontAndColorsCategoryItemName(itemName, EditorMEFCategory);
        }

        private FontAndColorsCategoryItemName CreateEnterprise(string itemName)
        {
            return new FontAndColorsCategoryItemName(itemName, EditorTextMarkerFontAndColorCategory);
        }

        public FontAndColorsCategoryItemName Covered { get; private set; }
        public FontAndColorsCategoryItemName NotCovered { get; private set; }
        public FontAndColorsCategoryItemName PartiallyCovered { get; private set; }
        public FontAndColorsCategoryItemName NewLines { get; private set; }
        public FontAndColorsCategoryItemName Dirty { get; private set; }

        public FontAndColorsCategoryItemName NotIncluded { get; private set; }
        public ICoverageFontAndColorsCategoryItemNames CategoryItemNames => this;
    }

}
