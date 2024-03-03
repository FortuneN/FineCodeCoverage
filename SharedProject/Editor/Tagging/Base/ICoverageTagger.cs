using System;
using Microsoft.VisualStudio.Text.Tagging;

namespace FineCodeCoverage.Editor.Tagging.Base
{
    internal interface ICoverageTagger<T> : ITagger<T>, IDisposable where T : ITag
    {
        void RaiseTagsChanged();
        bool HasCoverage { get; }
    }
}
