using System;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    interface ITUnitCoverage
    {
        bool Ready { get; }

        event EventHandler<bool> CollectingChangedEvent;
        event EventHandler ReadyEvent;
        void CollectCoverage();
        void Cancel();
    }
}
