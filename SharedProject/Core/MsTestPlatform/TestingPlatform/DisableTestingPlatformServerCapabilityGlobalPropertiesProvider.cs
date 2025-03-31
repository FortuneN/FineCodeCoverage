using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Options;
using Microsoft.CodeAnalysis;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    // https://github.com/dotnet/project-system
    // https://github.com/microsoft/VSProjectSystem/blob/master/doc/extensibility/IProjectGlobalPropertiesProvider.md
    [Export(typeof(IProjectGlobalPropertiesProvider))]
    // https://github.com/microsoft/testfx/blob/main/src/Platform/Microsoft.Testing.Platform/buildMultiTargeting/Microsoft.Testing.Platform.props
    /*
        	https://github.com/microsoft/VSProjectSystem/blob/master/doc/overview/about_project_capabilities.md
            Classes exported via MEF can declare the project capabilities under which they apply.

            See https://learn.microsoft.com/en-gb/dotnet/api/microsoft.visualstudio.shell.interop.vsprojectcapabilityexpressionmatcher?view=visualstudiosdk-2022
            For expression syntax
    */
    [AppliesTo("TestContainer")]
    internal class DisableTestingPlatformServerCapabilityGlobalPropertiesProvider : StaticGlobalPropertiesProviderBase
    {
        private readonly UnconfiguredProject unconfiguredProject;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ICoverageProjectSettingsManager coverageProjectSettingsManager;

        [ImportingConstructor]
        public DisableTestingPlatformServerCapabilityGlobalPropertiesProvider(
            IProjectService projectService,
            UnconfiguredProject unconfiguredProject,
            IAppOptionsProvider appOptionsProvider,
            ICoverageProjectSettingsManager coverageProjectSettingsManager
        )
          : base((IProjectCommonServices)projectService.Services)
        {
            this.unconfiguredProject = unconfiguredProject;
            this.appOptionsProvider = appOptionsProvider;
            this.coverageProjectSettingsManager = coverageProjectSettingsManager;
        }

        private bool AllProjectsDisabled()
        {
            var appOptions = appOptionsProvider.Get();
            return !appOptions.Enabled && appOptions.DisabledNoCoverage;
        }

        private async Task<bool> IsApplicableAsync()
        {
            try
            {
                var configuredProject = await unconfiguredProject.GetSuggestedConfiguredProjectAsync();
                var references = await configuredProject.Services.PackageReferences.GetUnresolvedReferencesAsync();
                return !references.Any(r => r.UnevaluatedInclude == TUnitConstants.TUnitPackageId);
            }
            catch { }
            return false;
        }

        private async Task<bool> ProjectEnabledAsync()
        {
            var projectGuid = await GetProjectGuidAsync();
            if (!projectGuid.HasValue) return false;

            var coverageProject = GetCoverageProject(projectGuid.Value);
            var projectSettings = await coverageProjectSettingsManager.GetSettingsAsync(coverageProject);
            return projectSettings.Enabled;
        }

        private async Task<Guid?> GetProjectGuidAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var hostObject = unconfiguredProject.Services.HostObject;

            var vsHierarchy = (IVsHierarchy)hostObject;
            if (vsHierarchy != null)
            {
                var success = vsHierarchy.GetGuidProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out Guid projectGuid) == VSConstants.S_OK;

                if (success)
                {
                    return projectGuid;
                }
            }
            return null;
        }

        private CoverageProject GetCoverageProject(Guid projectGuid)
        {
            return new CoverageProject(appOptionsProvider, null, coverageProjectSettingsManager, null)
            {
                Id = projectGuid,
                ProjectFile = unconfiguredProject.FullPath
            };
        }

        public override async Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
        {
            if (await IsApplicableAsync() && !AllProjectsDisabled() && await ProjectEnabledAsync())
            {
                return Empty.PropertiesMap.Add("DisableTestingPlatformServerCapability", "true");
            }
            return Empty.PropertiesMap;
        }
    }
}
