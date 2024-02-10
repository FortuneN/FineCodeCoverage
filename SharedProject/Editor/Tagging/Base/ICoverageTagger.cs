using Microsoft.VisualStudio.Text.Tagging;
using System;

namespace FineCodeCoverage.Impl
{
    internal interface ICoverageTagger<T> : ITagger<T>, IDisposable where T : ITag
    {
        void RaiseTagsChanged();
        bool HasCoverage { get; }
    }
}
