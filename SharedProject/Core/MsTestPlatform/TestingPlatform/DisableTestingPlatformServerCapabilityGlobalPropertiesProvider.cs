using FineCodeCoverage.Engine.Model;
using FineCodeCoverage.Engine.MsTestPlatform.TestingPlatform;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Build;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Threading;

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
    [AppliesTo("TestingPlatformServer.ExitOnProcessExitCapability | TestingPlatformServer.UseListTestsOptionForDiscoveryCapability")]
    internal class DisableTestingPlatformServerCapabilityGlobalPropertiesProvider : StaticGlobalPropertiesProviderBase
    {
        private readonly IUseTestingPlatformProtocolFeatureService useTestingPlatformProtocolFeatureService;
        private readonly IAppOptionsProvider appOptionsProvider;
        private readonly ICoverageProjectSettingsManager coverageProjectSettingsManager;
        private CoverageProject coverageProject;
        [ImportingConstructor]
        public DisableTestingPlatformServerCapabilityGlobalPropertiesProvider(
            IUseTestingPlatformProtocolFeatureService useTestingPlatformProtocolFeatureService,
            IProjectService projectService,
            UnconfiguredProject unconfiguredProject,
            IAppOptionsProvider appOptionsProvider,
            ICoverageProjectSettingsManager coverageProjectSettingsManager
        )
          : base((IProjectCommonServices)projectService.Services)
        {
            ThreadHelper.JoinableTaskFactory.Run(async () =>
            {
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
                var hostObject = unconfiguredProject.Services.HostObject;

                var vsHierarchy = (IVsHierarchy)hostObject;
                if (vsHierarchy != null)
                {
                    var success = vsHierarchy.GetGuidProperty((uint)VSConstants.VSITEMID.Root, (int)__VSHPROPID.VSHPROPID_ProjectIDGuid, out Guid projectGuid) == VSConstants.S_OK;

                    if (success)
                    {
                        // todo - ICoverageProjectSettingsManager.GetSettingsAsync parameter 
                        // to change to what it actually needs
                        coverageProject = new CoverageProject(appOptionsProvider, null, coverageProjectSettingsManager, null)
                        {
                            Id = projectGuid,
                            ProjectFile = unconfiguredProject.FullPath
                        };
                    }
                }
            });

            this.useTestingPlatformProtocolFeatureService = useTestingPlatformProtocolFeatureService;
            this.appOptionsProvider = appOptionsProvider;
            this.coverageProjectSettingsManager = coverageProjectSettingsManager;
        }

        // visual studio options states that a restart is required.  If this is true then could cache this value
        private async System.Threading.Tasks.Task<bool> UsingTestingPlatformProtocolAsync()
        {
            var useTestingPlatformProtocolFeature = await useTestingPlatformProtocolFeatureService.GetAsync();
            return useTestingPlatformProtocolFeature.HasValue && useTestingPlatformProtocolFeature.Value;
        }

        private bool AllProjectsDisabled()
        {
            var appOptions = appOptionsProvider.Get();
            return !appOptions.Enabled && appOptions.DisabledNoCoverage;
        }

        private async System.Threading.Tasks.Task<bool> ProjectEnabledAsync()
        {
            if (coverageProject != null)
            {
                var projectSettings = await coverageProjectSettingsManager.GetSettingsAsync(coverageProject);
                return projectSettings.Enabled;
            }
            return true;
        }

        public override async System.Threading.Tasks.Task<IImmutableDictionary<string, string>> GetGlobalPropertiesAsync(CancellationToken cancellationToken)
        {
            /*
                Note that it only matters for ms code coverage but not going to test for that
                Main thing is that FCC does not turn off if user has Enterprise which does support
                the new feature and has turned off FCC.
            */
            if (await UsingTestingPlatformProtocolAsync() && !AllProjectsDisabled() && await ProjectEnabledAsync())
            {
                // https://github.com/microsoft/testfx/blob/main/src/Platform/Microsoft.Testing.Platform.MSBuild/buildMultiTargeting/Microsoft.Testing.Platform.MSBuild.targets
                return Empty.PropertiesMap.Add("DisableTestingPlatformServerCapability", "true");
            }
            return Empty.PropertiesMap;
        }

    }

}
