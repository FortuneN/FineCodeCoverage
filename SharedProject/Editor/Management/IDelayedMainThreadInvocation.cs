using System;

namespace FineCodeCoverage.Editor.Management
{
    internal interface IDelayedMainThreadInvocation
    {
        void DelayedInvoke(Action action);
    }
}
