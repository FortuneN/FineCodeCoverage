using Microsoft.VisualStudio.Shell;
using System;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace FineCodeCoverage.Core.Utilities.VsThreading
{
    internal interface IThreadHelper
    {
        IJoinableTaskFactory JoinableTaskFactory { get; }
    }

    internal interface IJoinableTaskFactory
    {
        void Run(Func<Task> asyncMethod);
        Task SwitchToMainThreadAsync(CancellationToken cancellationToken = default);
    }

    internal class VsJoinableTaskFactory : IJoinableTaskFactory
    {
        public void Run(Func<Task> asyncMethod)
        {
            ThreadHelper.JoinableTaskFactory.Run(asyncMethod);
        }

        public async Task SwitchToMainThreadAsync(CancellationToken cancellationToken = default)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }
    }

    internal class VsThreadHelper : IThreadHelper
    {
        public IJoinableTaskFactory JoinableTaskFactory { get; } = new VsJoinableTaskFactory();
    }
}
