using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.VisualStudio.Shell;

namespace FineCodeCoverage.Editor.Management
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IDelayedMainThreadInvocation))]
    internal class DelayedMainThreadInvocation : IDelayedMainThreadInvocation
    {
        public void DelayedInvoke(Action action)
            => _ = System.Threading.Tasks.Task.Delay(0).ContinueWith(_ =>
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    action();
                }), TaskScheduler.Default);
    }
}
