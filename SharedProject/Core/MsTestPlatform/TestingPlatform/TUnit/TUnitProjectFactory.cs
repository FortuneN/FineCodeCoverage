using Microsoft.VisualStudio.ProjectSystem;
using Microsoft.VisualStudio.ProjectSystem.Properties;
using Microsoft.VisualStudio.Shell.Interop;
using NuGet.VisualStudio.Contracts;
using System;
using System.Collections.Immutable;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    [Export(typeof(ITUnitProjectFactory))]
    internal class TUnitProjectFactory : ITUnitProjectFactory
    {
        private readonly ITUnitInstalledPackagesService tUnitInstalledPackagesService;
        private readonly ICommandLineParser commandLineParser;

        class TUnitProject : ITUnitProject, IDisposable
        {
            private readonly ITUnitInstalledPackagesService tUnitInstalledPackagesService;
            private readonly ICommandLineParser commandLineParser;
            private IImmutableDictionary<string, IImmutableDictionary<string, string>> packageReferenceItems;
            private bool requiresUpdate = true;
            private bool disposedValue;
            private readonly IProjectProperties commonProperties;
            private readonly IDisposable packageChangeSubscription;
            private const string FCCTestingPlatformCommandLineArgumentsPropertyName = "FCCTestingPlatformCommandLineArguments";
            private const string TestingPlatformCommandLineArgumentsPropertyName = "TestingPlatformCommandLineArguments";


            /*
                in VS2022 there is also
                https://learn.microsoft.com/en-us/visualstudio/extensibility/visualstudio.extensibility/project/project?view=vs-2022
                https://learn.microsoft.com/en-us/visualstudio/extensibility/project-visual-studio-sdk?view=vs-2022  o
            */

            public TUnitProject(
                ITUnitInstalledPackagesService tUnitInstalledPackagesService,
                ICommandLineParser commandLineParser,
                ConfiguredProject configuredProject,
                IVsHierarchy hierarchy
            )
            {
                commonProperties = configuredProject.Services.ProjectPropertiesProvider.GetCommonProperties();
                this.Hierarchy = hierarchy;
                this.tUnitInstalledPackagesService = tUnitInstalledPackagesService;
                this.commandLineParser = commandLineParser;
                this.packageChangeSubscription = this.SubscribeToPackageReferenceChanges(configuredProject);
            }

            /*
                cannot use GetEvaluatedPropertyValueAsync as absence returns empty string
            */
            private async Task<bool?> UseFCCTestingPlatformCommandLineArgumentsPropertyNameAsync()
            {
                var propertyNames = await commonProperties.GetPropertyNamesAsync();
                var hasTestingPlatformCommandLineArgumentsPropertyName = false;
                foreach (var propertyName in propertyNames)
                {
                    if(propertyName == FCCTestingPlatformCommandLineArgumentsPropertyName)
                    {
                        return true;
                    }
                    if(propertyName == TestingPlatformCommandLineArgumentsPropertyName)
                    {
                        hasTestingPlatformCommandLineArgumentsPropertyName = true;
                    }
                }
                if (hasTestingPlatformCommandLineArgumentsPropertyName)
                {
                    return false;
                }
                return null;
            }

            private async Task ParseTestingPlatformCommandLineArgumentsAsync()
            {
                var useFCCTestingPlatformCommandLineArgumentsPropertyName = await UseFCCTestingPlatformCommandLineArgumentsPropertyNameAsync();
                if (!useFCCTestingPlatformCommandLineArgumentsPropertyName.HasValue)
                {
                    CommandLineParseResult = CommandLineParseResult.Empty;
                }
                else
                {
                    var propertyName = useFCCTestingPlatformCommandLineArgumentsPropertyName.Value ? FCCTestingPlatformCommandLineArgumentsPropertyName : TestingPlatformCommandLineArgumentsPropertyName;
                    var testingPlatformCommandLineArguments = await commonProperties.GetEvaluatedPropertyValueAsync(propertyName);

                    CommandLineParseResult = commandLineParser.Parse(testingPlatformCommandLineArguments);
                }
            }

            private IDisposable SubscribeToPackageReferenceChanges(ConfiguredProject configuredProject)
            {
                // there is ActiveConfiguredProjectSubscription but not available in 2019
                var subscriptionService = configuredProject.Services.ProjectSubscription;
                var receivingBlock = new ActionBlock<IProjectVersionedValue<IProjectSubscriptionUpdate>>(ProjectUpdateAsync);
                return subscriptionService.JointRuleSource.SourceBlock.LinkTo(receivingBlock, ruleNames: new string[] { "PackageReference" });
            }

            /*
                Idea was to use Nuget api, but
                IVsPackageInstallerEvents
                These events are only raised for packages.config projects. 
                To get updates for both packages.config and PackageReference use IVsNuGetProjectUpdateEvents instead.

                But IVsNuGetProjectUpdateEvents shipped in version 6.2 - Visual Studio 2022

                --
                Also note that IVSProject4 has PackageReferences but the project is IVSProject !
                and cannot get change event from VSProjectEvents.ReferencesEvents
            */

            /*
                if did not want real-time changes then could have used configuredProject.Services.PackageReferences
                public interface IPackageReference : IReference
                {
                } 
            */

            private Task ProjectUpdateAsync(IProjectVersionedValue<IProjectSubscriptionUpdate> update)
            {
                // if need to switch to the main thread will need CPS IThreadHandling 
                // This runs on a background thread. 
                packageReferenceItems = update.Value.CurrentState["PackageReference"].Items;
                requiresUpdate = true;
                return Task.CompletedTask;
            }
            public bool IsTUnit { get; private set; }
            public bool HasCoverageExtension { get; private set; }
            public IVsHierarchy Hierarchy { get; }

            public CommandLineParseResult CommandLineParseResult { get; private set; } = CommandLineParseResult.Empty;
            public async Task UpdateStateAsync(CancellationToken cancellationToken)
            {
                if (requiresUpdate)
                {
                    var installedPackagesResult = await tUnitInstalledPackagesService.GetTUnitInstalledPackagesAsync(await Hierarchy.GetGuidAsync(), cancellationToken);
                    if (installedPackagesResult.Status != InstalledPackageResultStatus.Successful)
                    {
                        // fallback but not transitive
                        // the data flow block should get data immediately
                        installedPackagesResult = tUnitInstalledPackagesService.GetTUnitInstalledPackages(packageReferenceItems);
                    }

                    IsTUnit = installedPackagesResult.HasTUnit;
                    HasCoverageExtension = installedPackagesResult.HasCoverageExtension;

                    requiresUpdate = false;
                }

                if (IsTUnit)
                {
                    /*
                        alternative is 
                        var projectSnapshotService = configuredProject.Services.ProjectSnapshotService;
                        var receivingBlock = new ActionBlock<IProjectVersionedValue<IProjectSnapshot>>((pvv) =>
                        {
                            var projectInstance = pvv.Value.ProjectInstance;
                            var argsProperty = projectInstance.GetProperty(FCCTestingPlatformCommandLineArgumentsPropertyName);
                            if (argsProperty == null)
                            {
                                argsProperty = projectInstance.GetProperty(TestingPlatformCommandLineArgumentsPropertyName);
                            }
                            if(argsProperty != null)
                            {
                                var value = argsProperty.EvaluatedValue;
                            }

                        });
                        return projectSnapshotService.SourceBlock.LinkTo(receivingBlock);

                    */
                    await ParseTestingPlatformCommandLineArgumentsAsync();
                }
            }

            protected virtual void Dispose(bool disposing)
            {
                if (!disposedValue)
                {
                    if (disposing)
                    {
                        packageChangeSubscription.Dispose();
                    }

                    disposedValue = true;
                }
            }

            public void Dispose()
            {
                // Do not change this code. Put cleanup code in 'Dispose(bool disposing)' method
                Dispose(disposing: true);
                GC.SuppressFinalize(this);
            }
        }


        [ImportingConstructor]
        public TUnitProjectFactory(
            ITUnitInstalledPackagesService tUnitInstalledPackagesService,
            ICommandLineParser commandLineParser
        )
        {
            this.tUnitInstalledPackagesService = tUnitInstalledPackagesService;
            this.commandLineParser = commandLineParser;
        }
        public ITUnitProject Create(IVsHierarchy hierarchy,ConfiguredProject configuredProject)
        {
            return new TUnitProject(tUnitInstalledPackagesService, commandLineParser, configuredProject, hierarchy);
        }
    }
}
