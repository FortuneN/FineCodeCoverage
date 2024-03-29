﻿using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Composition;
using Task = System.Threading.Tasks.Task;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(ISolutionEvents))]
    public class SolutionEvents : ISolutionEvents, IVsSolutionEvents
    {
        public event EventHandler AfterClosing;
        public event EventHandler AfterOpen;

        [ImportingConstructor]
        public SolutionEvents(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider
            )
        {
#pragma warning disable VSTHRD104 // Offer async methods
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var vsSolution = (IVsSolution)serviceProvider.GetService(typeof(SVsSolution));
                Assumes.Present(vsSolution);
                vsSolution.AdviseSolutionEvents(this, out uint _);
            });
#pragma warning restore VSTHRD104 // Offer async methods
        }

        public int OnAfterOpenProject(IVsHierarchy pHierarchy, int fAdded)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryCloseProject(IVsHierarchy pHierarchy, int fRemoving, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseProject(IVsHierarchy pHierarchy, int fRemoved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterLoadProject(IVsHierarchy pStubHierarchy, IVsHierarchy pRealHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnQueryUnloadProject(IVsHierarchy pRealHierarchy, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeUnloadProject(IVsHierarchy pRealHierarchy, IVsHierarchy pStubHierarchy)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterOpenSolution(object pUnkReserved, int fNewSolution)
        {
            AfterOpen?.Invoke(this, EventArgs.Empty);
            return VSConstants.S_OK;
        }

        public int OnQueryCloseSolution(object pUnkReserved, ref int pfCancel)
        {
            return VSConstants.S_OK;
        }

        public int OnBeforeCloseSolution(object pUnkReserved)
        {
            return VSConstants.S_OK;
        }

        public int OnAfterCloseSolution(object pUnkReserved)
        {
            AfterClosing?.Invoke(this, EventArgs.Empty);
            return VSConstants.S_OK;
        }
    }
}
