using FineCodeCoverage.Engine.Model;
using Microsoft.VisualStudio.Text;
using System.Collections.Generic;

namespace FineCodeCoverage.Impl
{
    interface ITrackedLinesFactory
    {
        ITrackedLines Create(List<ILine> lines, ITextSnapshot textSnapshot, Language language);
    }
}
