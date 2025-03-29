using Microsoft.VisualStudio.Shell.Interop;
using System;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    internal class ProjectAddedRemoved
    {
        public ProjectAddedRemoved(bool added, IVsHierarchy project)
        {
            Added = added;
            Project = project;
        }
        public bool Added { get; }
        public IVsHierarchy Project { get; }
    }

    internal interface ITUnitChangeNotifier
    {
        event EventHandler<ProjectAddedRemoved> ProjectAddedRemovedEvent;
        event EventHandler SolutionClosedEvent;
        event EventHandler SolutionOpenedEvent;
    }
}
