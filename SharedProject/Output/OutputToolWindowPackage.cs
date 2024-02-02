using System;
using System.Threading;
using FineCodeCoverage.Options;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using Task = System.Threading.Tasks.Task;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft;
using FineCodeCoverage.Engine;
using EnvDTE80;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.Core.Initialization;
using FineCodeCoverage.Impl;
using System.ComponentModel.Design;

namespace FineCodeCoverage.Output
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [ProvideBindingPath]
	[Guid(PackageGuids.guidOutputToolWindowPackageString)]
	[ProvideMenuResource("Menus.ctmenu", 1)]
	[Export(typeof(OutputToolWindowPackage))]
	[InstalledProductRegistration(Vsix.Name, Vsix.Description, Vsix.Id)]
	[ProvideOptionPage(typeof(AppOptionsPage), Vsix.Name, "General", 0, 0, true)]
    [ProvideProfile(typeof(AppOptionsPage), Vsix.Name, Vsix.Name, 101, 102, true)]
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
	[ProvideToolWindow(typeof(OutputToolWindow), Style = VsDockStyle.Tabbed, DockedHeight = 300, Window = EnvDTE.Constants.vsWindowKindOutput)]
	[ProvideTextMarker("FCCCovered","FCCCovered", CoverageColoursManager.TouchedGuidString, CoverageColoursManager.TextMarkerProviderString)]
    [ProvideTextMarker("FCCUncovered", "FCCUncovered", CoverageColoursManager.NotTouchedGuidString, CoverageColoursManager.TextMarkerProviderString)]
    [ProvideTextMarker("FCCPartiallyCovered", "FCCPartiallyCovered", CoverageColoursManager.PartiallyTouchedGuidString, CoverageColoursManager.TextMarkerProviderString)]
	[ProvideService(typeof(CoverageColoursManager))]
    [ProvideAutoLoad("0FA5E26B-3EAA-4D5E-B689-129B0D2A8690", PackageAutoLoadFlags.SkipWhenUIContextRulesActive)]
    [ProvideUIContextRule("0FA5E26B-3EAA-4D5E-B689-129B0D2A8690", "CoverageWindowLoad", "(TestContainer | TestProjects | WindowStoreTestProjects | CppTestProjects)", new string[] { "TestContainer", "TestProjects", "WindowStoreTestProjects", "CppTestProjects" }, new string[] { "SolutionHasProjectCapability:TestContainer", "SolutionHasProjectFlavor:3AC096D0-A1C2-E12C-1390-A8335801FDAB", "SolutionHasProjectFlavor:BC8A1FFA-BEE3-4634-8014-F334798102B3", "SolutionHasProjectFlavor:8BC9CEB8-8B4A-11D0-8D11-00A0C91BC942" }, 0)]
    public sealed class OutputToolWindowPackage : AsyncPackage
	{
		private static Microsoft.VisualStudio.ComponentModelHost.IComponentModel componentModel;
        private IFCCEngine fccEngine;

		/// <summary>
		/// Initializes a new instance of the <see cref="OutputToolWindowPackage"/> class.
		/// </summary>
		public OutputToolWindowPackage()
		{
			// Inside this method you can place any initialization code that does not require
			// any Visual Studio service because at this point the package object is created but
			// not sited yet inside Visual Studio environment. The place to do all the other
			// initialization is the Initialize method.
		}

		/*
			Hack necessary for debugging in 2022 !
			https://developercommunity.visualstudio.com/t/vsix-tool-window-vs2022-different-instantiation-wh/1663280
		*/
		internal static OutputToolWindowContext GetOutputToolWindowContext()
        {
			return new OutputToolWindowContext
			{
				EventAggregator = componentModel.GetService<IEventAggregator>(),
				ShowToolbar = componentModel.GetService<IAppOptionsProvider>().Get().ShowToolWindowToolbar
			};
		}

		/// <summary>
		/// Initialization of the package; this method is called right after the package is sited, so this is the place
		/// where you can put all the initialization code that rely on services provided by VisualStudio.
		/// </summary>
		/// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
		/// <param name="progress">A provider for progress updates.</param>
		/// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
		protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
		{
			// When initialized asynchronously, the current thread may be a background thread at this point.
			// Do any initialization that requires the UI thread after switching to the UI thread.
			await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);

			var _dte2 = (DTE2)GetGlobalService(typeof(SDTE));			
			var sp = new ServiceProvider(_dte2 as Microsoft.VisualStudio.OLE.Interop.IServiceProvider);
			// you cannot MEF import in the constructor of the package
			componentModel = sp.GetService(typeof(Microsoft.VisualStudio.ComponentModelHost.SComponentModel)) as Microsoft.VisualStudio.ComponentModelHost.IComponentModel;
            Assumes.Present(componentModel);
			fccEngine = componentModel.GetService<IFCCEngine>();
			var eventAggregator = componentModel.GetService<IEventAggregator>();
			await OpenCoberturaCommand.InitializeAsync(this, eventAggregator);
			await OpenHotspotsCommand.InitializeAsync(this, eventAggregator);
            await ClearUICommand.InitializeAsync(this, fccEngine);
            await OutputToolWindowCommand.InitializeAsync(
				this, 
				componentModel.GetService<ILogger>(),
				componentModel.GetService<IShownToolWindowHistory>()
			);
			await componentModel.GetService<IInitializer>().InitializeAsync(cancellationToken);
			var coverageColours = componentModel.GetService<CoverageColoursManager>();
            this.AddService(typeof(CoverageColoursManager),(_,__,___) => Task.FromResult(coverageColours as object),true);
        }

        protected override Task<object> InitializeToolWindowAsync(Type toolWindowType, int id, CancellationToken cancellationToken)
        {
			return Task.FromResult<object>(GetOutputToolWindowContext());
		}
        public override IVsAsyncToolWindowFactory GetAsyncToolWindowFactory(Guid toolWindowType)
		{
			return (toolWindowType == typeof(OutputToolWindow).GUID) ? this : null;
		}

		protected override string GetToolWindowTitle(Type toolWindowType, int id)
		{
			if (toolWindowType == typeof(OutputToolWindow))
			{
				return $"{Vsix.Name} loading";
			}

			return base.GetToolWindowTitle(toolWindowType, id);
		}
	}
}
