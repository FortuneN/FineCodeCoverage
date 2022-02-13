using EnvDTE;
using Microsoft;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Core.Utilities
{
    [Export(typeof(ISolutionEvents))]
    public class SolutionEvents : ISolutionEvents
    {
        private Events Events;
        private EnvDTE.SolutionEvents dteSolutionEvents;
        public event EventHandler AfterClosing;

        [ImportingConstructor]
        public SolutionEvents(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider
            )
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var Dte = (DTE)serviceProvider.GetService(typeof(DTE));
            Assumes.Present(Dte);
            Events = Dte.Events;
            dteSolutionEvents = Events.SolutionEvents;
            dteSolutionEvents.AfterClosing += () => AfterClosing?.Invoke(this, EventArgs.Empty);
        }
    }
}
