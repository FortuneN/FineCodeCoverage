using FineCodeCoverage.Core.Utilities;
using Microsoft.ServiceHub.Framework;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.ServiceBroker;
using Microsoft.VisualStudio.Threading;
using NuGet.VisualStudio.Contracts;
using System;
using System.ComponentModel.Composition;

namespace FineCodeCoverage.Core.MsTestPlatform.TestingPlatform
{
    [Export(typeof(INugetProjectServiceProvider))]
    internal class NugetProjectServiceProvider : INugetProjectServiceProvider
    {
        public AsyncLazy<INuGetProjectService> LazyNugetProjectService { get; }

        [ImportingConstructor]
        public NugetProjectServiceProvider(
            [Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider
        )
        {
            LazyNugetProjectService = new AsyncLazy<INuGetProjectService>(async () =>
            {
                var brokeredServiceContainer = serviceProvider.GetService<SVsBrokeredServiceContainer, IBrokeredServiceContainer>();
                IServiceBroker serviceBroker = brokeredServiceContainer.GetFullAccessServiceBroker();
#pragma warning disable ISB001 // Dispose of proxies
                INuGetProjectService nugetProjectService = await serviceBroker.GetProxyAsync<INuGetProjectService>(NuGetServices.NuGetProjectServiceV1);
#pragma warning restore ISB001 // Dispose of proxies
                return nugetProjectService;
            }, ThreadHelper.JoinableTaskFactory);
        }
    }
}
