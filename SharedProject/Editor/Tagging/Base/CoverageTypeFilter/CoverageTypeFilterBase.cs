using System;
using System.Collections.Generic;
using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Options;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal abstract class CoverageTypeFilterBase : ICoverageTypeFilter
    {
        private static readonly Dictionary<DynamicCoverageType, bool> doNotShowLookup = new Dictionary<DynamicCoverageType, bool>()
        {
            { DynamicCoverageType.Covered, false },
            { DynamicCoverageType.Partial, false },
            { DynamicCoverageType.NotCovered, false },
            { DynamicCoverageType.Dirty, false },
            { DynamicCoverageType.NewLine, false },
            { DynamicCoverageType.NotIncluded, false }
        };
        private Dictionary<DynamicCoverageType, bool> showLookup = doNotShowLookup;

        public void Initialize(IAppOptions appOptions)
        {
            if (appOptions.ShowEditorCoverage && this.EnabledPrivate(appOptions))
            {
                this.showLookup = this.GetShowLookup(appOptions);
                if (this.showLookup == null || this.showLookup.Count != 6)
                {
                    throw new InvalidOperationException("Invalid showLookup");
                }
            }
        }

        private bool EnabledPrivate(IAppOptions appOptions)
        {
            bool enabled = this.Enabled(appOptions);
            this.Disabled = !enabled;
            return enabled;
        }

        protected abstract bool Enabled(IAppOptions appOptions);
        protected abstract Dictionary<DynamicCoverageType, bool> GetShowLookup(IAppOptions appOptions);

        public abstract string TypeIdentifier { get; }

        public bool Disabled { get; set; } = true;

        public bool Show(DynamicCoverageType coverageType) => this.showLookup[coverageType];

        public bool Changed(ICoverageTypeFilter other)
        {
            if (other.TypeIdentifier != this.TypeIdentifier)
            {
                throw new ArgumentException("Argument of incorrect type", nameof(other));
            }

            Dictionary<DynamicCoverageType, bool> otherShowLookup = (other as CoverageTypeFilterBase).showLookup;
            foreach (KeyValuePair<DynamicCoverageType, bool> kvp in doNotShowLookup)
            {
                DynamicCoverageType coverageType = kvp.Key;
                if (this.showLookup[coverageType] != otherShowLookup[coverageType])
                {
                    return true;
                }
            }

            return false;
        }
    }
}
