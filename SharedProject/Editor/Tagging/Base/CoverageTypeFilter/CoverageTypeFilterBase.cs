﻿using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using System;
using System.Collections.Generic;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal abstract class CoverageTypeFilterBase : ICoverageTypeFilter
    {
        private static readonly Dictionary<CoverageType, bool> doNotShowLookup = new Dictionary<CoverageType, bool>()
        {
            { CoverageType.Covered, false },
            { CoverageType.Partial, false },
            { CoverageType.NotCovered, false },
        };
        private Dictionary<CoverageType, bool> showLookup = doNotShowLookup;

        public void Initialize(IAppOptions appOptions)
        {
            if (appOptions.ShowEditorCoverage && EnabledPrivate(appOptions))
            {
                showLookup = GetShowLookup(appOptions);
                if (showLookup == null || showLookup.Count != 3)
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
        protected abstract Dictionary<CoverageType, bool> GetShowLookup(IAppOptions appOptions);

        public abstract string TypeIdentifier { get; }

        public bool Disabled { get; set; } = true;

        public bool Show(CoverageType coverageType)
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