using Microsoft.VisualStudio.Threading;
using NuGet.VisualStudio.Contracts;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.ComponentModel.Composition;
using System.Collections.Immutable;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{

    [Export(typeof(ITUnitInstalledPackagesService))]
    internal class TUnitInstalledPackagesService : ITUnitInstalledPackagesService
    {
        private readonly AsyncLazy<INuGetProjectService> lazyNugetProjectService;

        [ImportingConstructor]
        public TUnitInstalledPackagesService(
            INugetProjectServiceProvider nugetProjectServiceProvider
        )
        {
            this.lazyNugetProjectService = nugetProjectServiceProvider.LazyNugetProjectService;
        }

        public TUnitInstalledPackageResult GetTUnitInstalledPackages(IImmutableDictionary<string, IImmutableDictionary<string, string>> packageReferenceItems)
        {
            if(packageReferenceItems == null)
            {
                return new TUnitInstalledPackageResult(InstalledPackageResultStatus.Unknown, false, false);
            }

            var hasTUnit = false;
            var hasCoverageExtension = false;
            foreach (var packageReference in packageReferenceItems)
            {
                var id = packageReference.Key;
                if (id == TUnitConstants.TUnitPackageId)
                {
                    hasTUnit = true;
                    continue;
                }
                if (id == TUnitConstants.CodeCoveragePackageId)
                {
                    hasCoverageExtension = true;
                }
                if (hasTUnit && hasCoverageExtension)
                {
                    break;
                }
            }
            return new TUnitInstalledPackageResult(InstalledPackageResultStatus.Successful, hasCoverageExtension, hasTUnit);
        }

        public async Task<TUnitInstalledPackageResult> GetTUnitInstalledPackagesAsync(Guid projectGuid, CancellationToken cancellationToken)
        {
            var nugetProjectService = await lazyNugetProjectService.GetValueAsync();
            var result = await nugetProjectService.GetInstalledPackagesAsync(projectGuid, cancellationToken);
            if (result.Status == InstalledPackageResultStatus.Successful)
            {
                var hasTUnit = false;
                var hasCoverageExtension = false;
                foreach (var package in result.Packages)
                {
                    var id = package.Id;
                    if (id == TUnitConstants.TUnitPackageId)
                    {
                        hasTUnit = true;
                        continue;
                    }
                    if (id == TUnitConstants.CodeCoveragePackageId)
                    {
                        hasCoverageExtension = true;
                    }
                    if (hasTUnit && hasCoverageExtension)
                    {
                        break;
                    }
                }
                return new TUnitInstalledPackageResult(result.Status, hasCoverageExtension, hasTUnit);
            }
            return new TUnitInstalledPackageResult(result.Status, false, false);
        }
    }


}
