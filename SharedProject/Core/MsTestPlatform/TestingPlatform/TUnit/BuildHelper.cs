using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System.Threading;
using Microsoft;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    internal class BuildStartEnd : IVsUpdateSolutionEvents {
        public event EventHandler<BuildStartEndArgs> BuildEvent;

        public int UpdateSolution_Begin(ref int pfCancelUpdate)
        {
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            BuildEvent?.Invoke(this, new BuildStartEndArgs(false));
            return VSConstants.S_OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate)
        {
            BuildEvent?.Invoke(this, new BuildStartEndArgs(true));
            return VSConstants.S_OK;
        }

        public int UpdateSolution_Cancel()
        {
            return VSConstants.S_OK;
        }

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy)
        {
            return VSConstants.S_OK;
        }
    }

    internal class BuildCompletionHandler : IVsUpdateSolutionEvents, IDisposable
    {
        private readonly TaskCompletionSource<bool> _tcs = new TaskCompletionSource<bool>();
        private readonly CancellationTokenRegistration registration;
        public BuildCompletionHandler(CancellationToken cancellationToken)
        {
            registration = cancellationToken.Register(() => _tcs.TrySetCanceled());
        }

        /// <summary>
        /// Task that completes when the build finishes.
        /// </summary>
        public Task<bool> BuildCompleted => _tcs.Task;

        public int UpdateSolution_Begin(ref int pfCancelUpdate) => VSConstants.S_OK;

        public int UpdateSolution_Cancel() => VSConstants.S_OK;

        public int UpdateSolution_Done(int fSucceeded, int fModified, int fCancelCommand)
        {
            var cancelled = fCancelCommand == 1;
            var nonFailed = fSucceeded == 1;
            var anySucceeded = fModified == 1;

            // Signal the task completion.
            _tcs.TrySetResult(fSucceeded != 0);
            return VSConstants.S_OK;
        }

        public int UpdateSolution_StartUpdate(ref int pfCancelUpdate) => VSConstants.S_OK;

        public int OnActiveProjectCfgChange(IVsHierarchy pIVsHierarchy) => VSConstants.S_OK;

        public void Dispose()
        {
            registration.Dispose();
        }
    }

    [Export(typeof(IBuildHelper))]
    internal class BuildHelper : IBuildHelper
    {
        private IVsSolutionBuildManager2 solutionBuildManager2;
        private BuildStartEnd buildStartEnd;
        private bool building;
        public event EventHandler<BuildStartEndArgs> ExternalBuildEvent;

        [ImportingConstructor]
        public BuildHelper(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider
        )
        {
#pragma warning disable VSTHRD102 // Implement internal logic asynchronously
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                this.solutionBuildManager2 = serviceProvider.GetService(typeof(SVsSolutionBuildManager)) as IVsSolutionBuildManager2;
                Assumes.Present(this.solutionBuildManager2);
                buildStartEnd = new BuildStartEnd();
                this.solutionBuildManager2.AdviseUpdateSolutionEvents(buildStartEnd, out var cookie);
                buildStartEnd.BuildEvent += BuildStartEnd_BuildEvent;
            });
#pragma warning restore VSTHRD102 // Implement internal logic asynchronously
        }

        private void BuildStartEnd_BuildEvent(object sender, BuildStartEndArgs e)
        {
            if (!building)
            {
                ExternalBuildEvent?.Invoke(this, e);
            }
        }

        public async Task<bool> BuildAsync(List<IVsHierarchy> projects,CancellationToken cancellationToken)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            var buildHandler = new BuildCompletionHandler(cancellationToken);
            int hr = solutionBuildManager2.AdviseUpdateSolutionEvents(buildHandler, out uint cookie);
            ErrorHandler.ThrowOnFailure(hr);
            var succeeded = false;
            try
            {
                building = true;
                var result = solutionBuildManager2.StartSimpleUpdateSolutionConfiguration(
                    (uint)VSSOLNBUILDUPDATEFLAGS.SBF_OPERATION_BUILD, 0, 0);
                ErrorHandler.ThrowOnFailure(result);
                succeeded = await buildHandler.BuildCompleted;
            }
            catch (OperationCanceledException)
            {
                solutionBuildManager2.CancelUpdateSolutionConfiguration();
                throw;
            }
            finally
            {
                building = false;
                buildHandler.Dispose();
                solutionBuildManager2.UnadviseUpdateSolutionEvents(cookie);
            }
            return succeeded;
        }
    }
}
