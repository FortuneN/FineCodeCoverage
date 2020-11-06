using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using FineCodeCoverage;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

public static class Logger
{
	private static IVsOutputWindowPane _pane;
	private static IVsOutputWindow _outputWindow;
	private static IServiceProvider _serviceProvider;
	private static Guid _paneGuid = VSConstants.GUID_BuildOutputWindowPane;

	public static void Initialize(IServiceProvider serviceProvider)
	{
		_serviceProvider = serviceProvider;
	}

	[SuppressMessage("Usage", "VSTHRD104:Offer async methods")]
	public static void Log(params object[] message)
	{
		try
		{
			var messageList = new List<string>(message?.Select(x => x?.ToString()?.Trim(' ', '\r', '\n')).Where(x => !string.IsNullOrWhiteSpace(x)));

			if (!messageList.Any())
			{
				return;
			}

			if (_pane == null)
			{
				ThreadHelper.JoinableTaskFactory.Run(async () =>
				{
					await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
					_outputWindow = (IVsOutputWindow)_serviceProvider.GetService(typeof(SVsOutputWindow));
					_outputWindow?.GetPane(ref _paneGuid, out _pane);
				});
			}

			if (_pane == null)
			{
				return;
			}

			ThreadHelper.JoinableTaskFactory.Run(async () =>
			{
				await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
				_pane.OutputStringThreadSafe($"{Environment.NewLine}{Vsix.Name} : {string.Join(Environment.NewLine, messageList)}{Environment.NewLine}");
			});
		}
		catch (Exception ex)
		{
			Debug.Write(ex);
		}
	}
}