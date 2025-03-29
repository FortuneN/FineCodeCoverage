using Microsoft.VisualStudio.Threading;
using NuGet.VisualStudio.Contracts;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    internal interface INugetProjectServiceProvider
    {
        AsyncLazy<INuGetProjectService> LazyNugetProjectService { get; }
    }
}
