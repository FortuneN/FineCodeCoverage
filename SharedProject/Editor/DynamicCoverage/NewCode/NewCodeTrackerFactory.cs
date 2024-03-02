﻿using Microsoft.VisualStudio.Text;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(INewCodeTrackerFactory))]
    internal class NewCodeTrackerFactory : INewCodeTrackerFactory
    {
        private readonly ITrackedNewCodeLineFactory trackedNewCodeLineFactory;
        private readonly ILineExcluder codeLineExcluder;

        [ImportingConstructor]
        public NewCodeTrackerFactory(
            ITrackedNewCodeLineFactory trackedNewCodeLineFactory,
            ILineExcluder codeLineExcluder
        )
        {
            this.trackedNewCodeLineFactory = trackedNewCodeLineFactory;
            this.codeLineExcluder = codeLineExcluder;
        }
        public INewCodeTracker Create(bool isCSharp)
        {
            return new NewCodeTracker(isCSharp, trackedNewCodeLineFactory, codeLineExcluder);
        }

        public INewCodeTracker Create(bool isCSharp, List<int> lineNumbers, ITextSnapshot textSnapshot)
        {
            return new NewCodeTracker(isCSharp, trackedNewCodeLineFactory, codeLineExcluder, lineNumbers, textSnapshot);
        }
    }
}