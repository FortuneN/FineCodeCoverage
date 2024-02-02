using FineCodeCoverage.Core.Utilities.VsThreading;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverageTests.Test_helpers
{
    internal class TestThreadHelper : IThreadHelper
    {
        public IJoinableTaskFactory JoinableTaskFactory { get; } = new TestJoinableTaskFactory();
    }

    internal class TestJoinableTaskFactory : IJoinableTaskFactory
    {
        public void Run(Func<Task> asyncMethod)
        {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            asyncMethod().Wait();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }

        public T Run<T>(Func<Task<T>> asyncMethod)
        {
#pragma warning disable VSTHRD002 // Avoid problematic synchronous waits
            return asyncMethod().GetAwaiter().GetResult();
#pragma warning restore VSTHRD002 // Avoid problematic synchronous waits
        }

        public Task SwitchToMainThreadAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }
    }
}
