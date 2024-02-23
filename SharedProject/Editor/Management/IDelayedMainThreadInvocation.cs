using System;

namespace FineCodeCoverage.Editor.Management
{
    interface IDelayedMainThreadInvocation
    {
        void DelayedInvoke(Action action);
    }
}
