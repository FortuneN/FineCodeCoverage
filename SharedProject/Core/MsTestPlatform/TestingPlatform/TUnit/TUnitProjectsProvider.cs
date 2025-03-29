using System.Collections.Generic;
using System.Threading.Tasks;
using System.ComponentModel.Composition;
using System;
using System.Threading;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.Shell;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    [Export(typeof(ITUnitProjectsProvider))]
    internal class TUnitProjectsProvider : ITUnitProjectsProvider
    {
        private readonly ISolutionProjectsProvider solutionProjectsProvider;
        private readonly ICPSTestProjectService cpsTestProjectService;
        private readonly ITUnitChangeNotifier tUnitChangeNotifier;
        private readonly ITUnitProjectFactory tUnitProjectFactory;
        private readonly ITUnitProjectCache tUnitProjectCache;
        private bool initializedCache;
        private readonly List<IVsHierarchy> addedProjects = new List<IVsHierarchy>();

        public event EventHandler ReadyEvent;

        [ImportingConstructor]
        public TUnitProjectsProvider(
            ISolutionProjectsProvider solutionProjectsProvider,
            ICPSTestProjectService cpsTestProjectService,
            ITUnitChangeNotifier tUnitChangeNotifier,
            ITUnitProjectFactory tUnitProjectFactory,
            ITUnitProjectCache tUnitProjectCache
        )
        {
            tUnitChangeNotifier.ProjectAddedRemovedEvent += TUnitChangeNotifier_ProjectAddedRemovedEvent;
            tUnitChangeNotifier.SolutionClosedEvent += TUnitChangeNotifier_SolutionClosedEvent;
            tUnitChangeNotifier.SolutionOpenedEvent += TUnitChangeNotifier_SolutionOpenedEvent;
            this.solutionProjectsProvider = solutionProjectsProvider;
            this.cpsTestProjectService = cpsTestProjectService;
            this.tUnitChangeNotifier = tUnitChangeNotifier;
            this.tUnitProjectFactory = tUnitProjectFactory;
            this.tUnitProjectCache = tUnitProjectCache;
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                var solutionOpen = await solutionProjectsProvider.IsSolutionOpenAsync();
                if (solutionOpen)
                {
                    OnReady(true);
                }
            });
        }

        public bool Ready { get; private set; }

        private void OnReady(bool ready)
        {
            Ready = ready;
            ReadyEvent?.Invoke(this, EventArgs.Empty);
        }

        private void TUnitChangeNotifier_SolutionOpenedEvent(object sender, EventArgs e)
        {
            OnReady(true);
        }

        private void TUnitChangeNotifier_SolutionClosedEvent(object sender, EventArgs e)
        {
            addedProjects.Clear();
            if (initializedCache)
            {
                tUnitProjectCache.Clear();
                initializedCache = false;
            }
            OnReady(false);
        }

        private void TUnitChangeNotifier_ProjectAddedRemovedEvent(object sender, ProjectAddedRemoved e)
        {
            if (initializedCache)
            {
                var project = e.Project;
                if (e.Added)
                {
                    addedProjects.Add(project);
                }
                else
                {
                    var removed = addedProjects.Remove(project);
                    if(!removed)
                    {
                        tUnitProjectCache.Remove(e.Project);
                    }
                }
            }
        }

        private class CpsProjectAndHierarchy {
            public CpsProjectAndHierarchy(ConfiguredProject cpsProject, IVsHierarchy hierarchy)
            {
                CpsProject = cpsProject;
                Hierarchy = hierarchy;
            }

            public ConfiguredProject CpsProject { get; }
            public IVsHierarchy Hierarchy { get; }
        }

        private async Task<List<CpsProjectAndHierarchy>> GetCpsTestProjectsAndHierarchysAsync(IEnumerable<IVsHierarchy> projects)
        {
            List<CpsProjectAndHierarchy> cpsTestProjectsAndHierarchys = new List<CpsProjectAndHierarchy>();
            foreach(var project in projects)
            {
                var cpsTestProject = await cpsTestProjectService.GetProjectAsync(project);
                if (cpsTestProject != null)
                {
                    cpsTestProjectsAndHierarchys.Add(new CpsProjectAndHierarchy(cpsTestProject, project));
                }
            }
            return cpsTestProjectsAndHierarchys;
        }

        private async Task<List<ITUnitProject>> GetTUnitProjectsAsync(IEnumerable<IVsHierarchy> projects)
        {
            var potentialTUnitProjects = new List<ITUnitProject>();
            var cpsTestProjectAndHierarchys = await GetCpsTestProjectsAndHierarchysAsync(projects);
            foreach (var cpsTestProjectAndHierarchy in cpsTestProjectAndHierarchys)
            {
                var tUnitProject = tUnitProjectFactory.Create(cpsTestProjectAndHierarchy.Hierarchy, cpsTestProjectAndHierarchy.CpsProject);
                potentialTUnitProjects.Add(tUnitProject);
            }
            return potentialTUnitProjects;
        }

        public async Task<List<ITUnitProject>> GetTUnitProjectsAsync(CancellationToken cancellationToken)
        {
            if (!initializedCache)
            {
                var solutionProjects = await solutionProjectsProvider.GetLoadedProjectsAsync(cancellationToken);
                var potentialTUnitProjects = await GetTUnitProjectsAsync(solutionProjects);
                tUnitProjectCache.Initialize(potentialTUnitProjects);
                initializedCache = true;
            }
            else
            {
                var newTUnitProjects = await GetTUnitProjectsAsync(addedProjects);
                foreach(var newTUnitProject in newTUnitProjects)
                {
                    tUnitProjectCache.Add(newTUnitProject);
                }
                addedProjects.Clear();
            }

            return await tUnitProjectCache.GetTUnitProjectsAsync(cancellationToken);
        }
    }
}
