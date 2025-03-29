using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Threading;
using System;
using System.ComponentModel.Composition;
using System.Threading;
using Task = System.Threading.Tasks.Task;

namespace FineCodeCoverage.Core.Utilities
{
    internal interface ICancellationTokenSource : IDisposable
    {
        CancellationToken Token { get; }
        bool IsCancellationRequested { get; }

        void Cancel();
    }

    internal class CancellationTokenSourceWrapper : ICancellationTokenSource
    {
        private readonly CancellationTokenSource cancellationTokenSource;

        public CancellationTokenSourceWrapper(CancellationTokenSource cancellationTokenSource)
        {
            this.cancellationTokenSource = cancellationTokenSource;
        }

        public CancellationToken Token => cancellationTokenSource.Token;

        public bool IsCancellationRequested => cancellationTokenSource.IsCancellationRequested;

        public void Cancel()
        {
            cancellationTokenSource.Cancel();
        }

        public void Dispose()
        {
            cancellationTokenSource.Dispose();
        }
    }

    internal interface IDisposeAwareTaskRunner
    {
        void RunAsyncFunc(Func<Task> taskProvider);
        CancellationToken DisposalToken { get; }
        ICancellationTokenSource CreateLinkedTokenSource();
        bool IsVsShutdown { get; }
    }

    [Export(typeof(IDisposeAwareTaskRunner))]
    internal class DisposeAwareTaskRunner : IDisposable, IDisposeAwareTaskRunner
    {
        private readonly CancellationTokenSource disposeCancellationTokenSource = new CancellationTokenSource();

        internal DisposeAwareTaskRunner()
        {
            this.JoinableTaskCollection = ThreadHelper.JoinableTaskContext.CreateCollection();
            this.JoinableTaskFactory = ThreadHelper.JoinableTaskContext.CreateFactory(this.JoinableTaskCollection);
        }

        public JoinableTaskFactory JoinableTaskFactory { get; }
        JoinableTaskCollection JoinableTaskCollection { get; }

        /// <summary>
        /// Gets a <see cref="CancellationToken"/> that can be used to check if the package has been disposed.
        /// </summary>
        public CancellationToken DisposalToken => this.disposeCancellationTokenSource.Token;

        public bool IsVsShutdown => DisposalToken.IsCancellationRequested;

        public ICancellationTokenSource CreateLinkedTokenSource()
        {
            return new CancellationTokenSourceWrapper(CancellationTokenSource.CreateLinkedTokenSource(DisposalToken));
        }

        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                this.disposeCancellationTokenSource.Cancel();

                try
                {
                    // Block Dispose until all async work has completed.
#pragma warning disable IDE0079 // Remove unnecessary suppression 
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
                    ThreadHelper.JoinableTaskFactory.Run(this.JoinableTaskCollection.JoinTillEmptyAsync);
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
#pragma warning restore IDE0079 // Remove unnecessary suppression
                }
                catch (OperationCanceledException)
                {
                    // this exception is expected because we signaled the cancellation token
                }
                catch (AggregateException ex)
                {
                    // ignore AggregateException containing only OperationCanceledException
                    ex.Handle(inner => (inner is OperationCanceledException));
                }
                finally
                {
                    this.disposeCancellationTokenSource.Dispose();
                }
            }
        }

        public void RunAsyncFunc(Func<Task> taskProvider)
        {
            _ = JoinableTaskFactory.RunAsync(taskProvider);
        }
    }
}
