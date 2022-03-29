using System;

namespace FineCodeCoverage.Core.Utilities
{
    interface ISolutionEvents
    {
        event EventHandler AfterClosing;
        event EventHandler AfterOpen;
    }
}
