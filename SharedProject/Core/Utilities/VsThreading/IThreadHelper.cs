using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using System.Threading.Tasks;
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
        T Run<T>(Func<Task<T>> asyncMethod);
        Task SwitchToMainThreadAsync(CancellationToken cancellationToken = default);
    }

    internal class VsJoinableTaskFactory : IJoinableTaskFactory
    {
        public void Run(Func<Task> asyncMethod)
        {
            ThreadHelper.JoinableTaskFactory.Run(asyncMethod);
        }

        public T Run<T>(Func<Task<T>> asyncMethod)
        {
            return ThreadHelper.JoinableTaskFactory.Run(asyncMethod);
        }

        public async Task SwitchToMainThreadAsync(CancellationToken cancellationToken = default)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }
    }

    [Export(typeof(IThreadHelper))]
    internal class VsThreadHelper : IThreadHelper
    {
        public IJoinableTaskFactory JoinableTaskFactory { get; } = new VsJoinableTaskFactory();
    }
}
