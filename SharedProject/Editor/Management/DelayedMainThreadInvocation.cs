using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;

namespace FineCodeCoverage.Editor.Management
{
    [ExcludeFromCodeCoverage]
    [Export(typeof(IDelayedMainThreadInvocation))]
    internal class DelayedMainThreadInvocation : IDelayedMainThreadInvocation
    {
        public void DelayedInvoke(Action action)
        {
            _ = System.Threading.Tasks.Task.Delay(0).ContinueWith(_ =>
            {
#pragma warning disable VSTHRD110 // Observe result of async calls
                ThreadHelper.JoinableTaskFactory.RunAsync(async () =>
                {
                    await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                    action();
                });
#pragma warning restore VSTHRD110 // Observe result of async calls

            }, TaskScheduler.Default);
        }
    }
}
