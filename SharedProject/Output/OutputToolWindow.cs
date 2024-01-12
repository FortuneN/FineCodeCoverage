using System;
using System.IO;
using System.Linq;
using System.Reflection;
using FineCodeCoverage.Engine;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using FineCodeCoverage.Core.Utilities;
using System.ComponentModel.Design;

namespace FineCodeCoverage.Output
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("320fd13f-632f-4b16-9527-a1adfe555f6c")]
	internal class OutputToolWindow : ToolWindowPane, IListener<ReportFocusedMessage>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="OutputToolWindow"/> class.
		/// </summary>
		public OutputToolWindow(OutputToolWindowContext context) : base(null)
		{
            Initialize(context);
        }

		public OutputToolWindow()
        {
			Initialize(OutputToolWindowPackage.GetOutputToolWindowContext());
		}

		private void Initialize(OutputToolWindowContext context)
        {
			if (context.ShowToolbar)
			{
				this.ToolBar = new CommandID(PackageGuids.guidOutputToolWindowPackageCmdSet, PackageIds.ToolWindowToolbar);
			}
            //to see if OutputToolWindow can be internal ( and thus IScriptManager )
            Caption = Vsix.Name;
			context.EventAggregator.AddListener(this);

			// This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
			// we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
			// the object returned by the Content property.

			try
			{
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
				Content = new OutputToolWindowControl(context.EventAggregator);
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;
			}
		}

		private Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
		{
			var assemblyName = new AssemblyName(args.Name);

			try
			{
				AppDomain.CurrentDomain.AssemblyResolve -= CurrentDomain_AssemblyResolve;

				// try resolve by name

				try
				{
					var assembly = Assembly.Load(assemblyName.Name);
					if (assembly != null) return assembly;
				}
				catch
				{
					// ignore
				}

				// try resolve by path

				try
				{
					var dllName = $"{assemblyName.Name}.dll";
					var projectDllPath = Path.GetDirectoryName(typeof(FCCEngine).Assembly.Location);
					var dllPath = Directory.GetFiles(projectDllPath, "*.dll", SearchOption.AllDirectories).FirstOrDefault(x => Path.GetFileName(x).Equals(x.Equals(dllName, StringComparison.OrdinalIgnoreCase)));

					if (!string.IsNullOrWhiteSpace(dllPath))
					{
						var assembly = Assembly.LoadFile(dllPath);
						if (assembly != null) return assembly;
					}
				}
				catch
				{
					// ignore
				}
			}
			finally
			{
				AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
			}

			return null;
		}

        public void Handle(ReportFocusedMessage message)
        {
			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				(this.Frame as IVsWindowFrame).Show();
			});
		}
    }
}
