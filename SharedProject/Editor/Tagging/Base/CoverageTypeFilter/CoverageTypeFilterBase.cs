﻿using System;
using System.Collections.Generic;
using System.Linq;
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
            if (this.ShouldGetShowLookup(appOptions))
            {
                this.showLookup = this.GetShowLookup(appOptions);
                this.ThrowIfInvalidShowLookup();
            }
        }

        private bool ShouldGetShowLookup(IAppOptions appOptions) => appOptions.ShowEditorCoverage && this.EnabledPrivate(appOptions);

        private void ThrowIfInvalidShowLookup()
        {
            if (this.showLookup == null || this.showLookup.Count != 6)
            {
                throw new InvalidOperationException("Invalid showLookup");
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
            this.ThrowIfIncorrectCoverageTypeFilter(other);

            return this.CompareLookups((other as CoverageTypeFilterBase).showLookup);
        }

        private bool CompareLookups(Dictionary<DynamicCoverageType, bool> otherShowLookup)
            => Enum.GetValues(typeof(DynamicCoverageType)).Cast<DynamicCoverageType>()
                .Any(coverageType => this.showLookup[coverageType] != otherShowLookup[coverageType]);

        private void ThrowIfIncorrectCoverageTypeFilter(ICoverageTypeFilter other)
        {
            if (other.TypeIdentifier != this.TypeIdentifier)
            {
                throw new ArgumentException("Argument of incorrect type", nameof(other));
            }
        }
    }
}
