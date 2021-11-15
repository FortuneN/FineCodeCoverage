using System;
using System.Collections.Generic;
using System.Text;

namespace FineCodeCoverage.Engine.ReportGenerator
{
    internal interface IReportColoursProvider
    {
        event EventHandler<IReportColours> ColoursChanged;
        IReportColours GetColours();
    }
}
