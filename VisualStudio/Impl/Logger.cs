using System;
using System.Linq;
using FineCodeCoverage;
using System.Diagnostics;
using Microsoft.VisualStudio;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics.CodeAnalysis;
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

	[SuppressMessage("Usage", "VSTHRD102:Implement internal logic asynchronously")]
	private static void LogImpl(object[] message, bool withTitle)
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

				var logs = string.Join(Environment.NewLine, messageList);

				if (withTitle)
				{
					_pane.OutputStringThreadSafe($"{Environment.NewLine}{Vsix.Name} : {logs}{Environment.NewLine}");
				}
				else
				{
					_pane.OutputStringThreadSafe($"{logs}{Environment.NewLine}");
				}
			});
		}
		catch (Exception ex)
		{
			Debug.Write(ex);
		}
	}

	public static void Log(params object[] message)
	{
		LogImpl(message, true);
	}

	public static void Log(params string[] message)
	{
		LogImpl(message, true);
	}

	public static void Log(IEnumerable<object> message)
	{
		LogImpl(message.ToArray(), true);
	}

	public static void Log(IEnumerable<string> message)
	{
		LogImpl(message.ToArray(), true);
	}

	public static void LogWithoutTitle(params object[] message)
	{
		LogImpl(message, false);
	}

	public static void LogWithoutTitle(params string[] message)
	{
		LogImpl(message, false);
	}

	public static void LogWithoutTitle(IEnumerable<object> message)
	{
		LogImpl(message.ToArray(), false);
	}

	public static void LogWithoutTitle(IEnumerable<string> message)
	{
		LogImpl(message.ToArray(), false);
	}
}