using System;
using System.ComponentModel.Design;
using EnvDTE80;
using FineCodeCoverage.Core.MsTestPlatform.TestingPlatform;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;

namespace FineCodeCoverage.Output
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class CollectTUnitCommand
    {
        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = PackageIds.cmdidCollectTUnitCommand;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid CommandSet = PackageGuids.guidOutputToolWindowPackageCmdSet;

        private readonly MenuCommand command;
        private readonly ITUnitCoverage tUnitCoverage;

        public static CollectTUnitCommand Instance
        {
            get;
            private set;
        }


        public static async Task InitializeAsync(AsyncPackage package, ITUnitCoverage tUnitCoverage)
        {
            // Switch to the main thread - the call to AddCommand in the constructor requires
            // the UI thread.
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            OleMenuCommandService commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            var dte = ServiceProvider.GlobalProvider.GetService(typeof(SDTE)) as DTE2;
            Instance = new CollectTUnitCommand(commandService, tUnitCoverage);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ClearUICommand"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="commandService">Command service to add command to, not null.</param>
        private CollectTUnitCommand(OleMenuCommandService commandService, ITUnitCoverage tUnitCoverage)
        {
            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            this.command = new MenuCommand(this.Execute, menuCommandID);
            this.command.Enabled = tUnitCoverage.Ready;
            tUnitCoverage.CollectingChangedEvent += (_, collecting) => this.command.Visible = !collecting;
            tUnitCoverage.ReadyEvent += (_, __) =>
            {
                this.command.Enabled = tUnitCoverage.Ready;
            };
            commandService.AddCommand(command);
            this.tUnitCoverage = tUnitCoverage;
        }

        /// <summary>
        /// This function is the callback used to execute the command when the menu item is clicked.
        /// See the constructor to see how the menu item is associated with this function using
        /// OleMenuCommandService service and MenuCommand class.
        /// </summary>
        /// <param name="sender">Event sender.</param>
        /// <param name="e">Event args.</param>
        private void Execute(object sender, EventArgs e)
        {
            tUnitCoverage.CollectCoverage();
        }
    }
}
