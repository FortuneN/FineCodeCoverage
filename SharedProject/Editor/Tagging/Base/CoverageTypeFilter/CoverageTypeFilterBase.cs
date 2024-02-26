using FineCodeCoverage.Editor.DynamicCoverage;
using FineCodeCoverage.Options;
using System;
using System.Collections.Generic;

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
            if (appOptions.ShowEditorCoverage && EnabledPrivate(appOptions))
            {
                showLookup = GetShowLookup(appOptions);
                if (showLookup == null || showLookup.Count != 6)
                {
                    throw new InvalidOperationException("Invalid showLookup");
                }
            }
        }

        private bool EnabledPrivate(IAppOptions appOptions)
        {
            var enabled = Enabled(appOptions);
            Disabled = !enabled;
            return enabled;
        }

        protected abstract bool Enabled(IAppOptions appOptions);
        protected abstract Dictionary<DynamicCoverageType, bool> GetShowLookup(IAppOptions appOptions);

        public abstract string TypeIdentifier { get; }

        public bool Disabled { get; set; } = true;

        public bool Show(DynamicCoverageType coverageType)
        {
            return showLookup[coverageType];
        }

        public bool Changed(ICoverageTypeFilter other)
        {
            if (other.TypeIdentifier != TypeIdentifier)
            {
                throw new ArgumentException("Argument of incorrect type", nameof(other));
            }
            var otherShowLookup = (other as CoverageTypeFilterBase).showLookup;
            foreach (var kvp in doNotShowLookup)
            {
                var coverageType = kvp.Key;
                if (showLookup[coverageType] != otherShowLookup[coverageType])
                {
                    return true;
                }
            }
            return false;
        }
    }

}
