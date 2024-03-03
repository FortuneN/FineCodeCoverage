using System.Collections.Generic;
using FineCodeCoverage.Editor.Tagging.Base;
using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;

namespace FineCodeCoverage.Editor.DynamicCoverage
{
    interface ITrackedLinesFactory
    {
        ITrackedLines Create(List<ILine> lines, ITextSnapshot textSnapshot, Language language);
        ITrackedLines Create(string serializedCoverage, ITextSnapshot currentSnapshot, Language language);
        string Serialize(ITrackedLines trackedLines);
    }
}
