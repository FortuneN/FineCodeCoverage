using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Microsoft.VisualStudio.Shell.Interop;

/// <summary>
/// A logger made specifically for Visual Studio extensions.
/// </summary>
public static class Logger
{
	private static IVsOutputWindowPane pane;
	private static IServiceProvider _provider;
	private static Guid _guid;
	private static string _name;
	private static readonly object _syncRoot = new object();

	/// <summary>
	/// Initializes the logger.
	/// </summary>
	/// <param name="provider">The service provider or Package instance.</param>
	/// <param name="name">The name to use for the custom Output Window pane.</param>
	public static void Initialize(IServiceProvider provider, string name)
	{
		_provider = provider;
		_name = name;
	}

	///// <summary>
	///// Initializes the logger and Application Insights telemetry client.
	///// </summary>
	///// <param name="provider">The service provider or Package instance.</param>
	///// <param name="name">The name to use for the custom Output Window pane.</param>
	///// <param name="version">The version of the Visual Studio extension.</param>
	///// <param name="telemetryKey">The Applicatoin Insights instrumentation key (usually a GUID).</param>
	//public static void Initialize(IServiceProvider provider, string name, string version, string telemetryKey)
	//{
	//    Initialize(provider, name);
	//    Telemetry.Initialize(provider, version, telemetryKey);
	//}

	/// <summary>
	/// Logs a message to the Output Window.
	/// </summary>
	public static void Log(params object[] message)
	{
		try
		{
			var messageList = new List<string>(message?.Select(x => x?.ToString()?.Trim(' ', '\r', '\n')).Where(x => !string.IsNullOrWhiteSpace(x)));

			if (!messageList.Any())
			{
				return;
			}

			if (!EnsurePane())
			{
				return;
			}

			pane?.OutputStringThreadSafe($"{Environment.NewLine}{DateTime.Now} : {string.Join(Environment.NewLine, messageList)}{Environment.NewLine}");
		}
		catch (Exception ex)
		{
			Debug.Write(ex);
		}
	}

	/// <summary>
	/// Removes all text from the Output Window pane.
	/// </summary>
	public static void Clear()
	{
		if (pane != null)
		{
			pane?.Clear();
		}
	}

	/// <summary>
	/// Deletes the Output Window pane.
	/// </summary>
	public static void DeletePane()
	{
		if (pane != null)
		{
			try
			{
				var output = (IVsOutputWindow)_provider.GetService(typeof(SVsOutputWindow));
				output?.DeletePane(ref _guid);
				pane = null;
			}
			catch (Exception ex)
			{
				Debug.Write(ex);
			}
		}
	}

	private static bool EnsurePane()
	{
		if (pane == null)
		{
			lock (_syncRoot)
			{
				if (pane == null)
				{
					_guid = Guid.NewGuid();
					var output = (IVsOutputWindow)_provider.GetService(typeof(SVsOutputWindow));
					output?.CreatePane(ref _guid, _name, 1, 1);
					output?.GetPane(ref _guid, out pane);
				}
			}
		}

		return pane != null;
	}
}