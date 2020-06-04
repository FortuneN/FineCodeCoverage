using System;
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
    private static object _syncRoot = new object();

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
    /// <param name="message">The message to output.</param>
    public static void Log(string message)
    {
        if (string.IsNullOrEmpty(message))
            return;

        try
        {
            if (EnsurePane())
            {
                pane.OutputStringThreadSafe(DateTime.Now + ": " + message + Environment.NewLine);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.Write(ex);
        }
    }

    /// <summary>
    /// Logs an exception to the output window and tracks it in Application Insights.
    /// </summary>
    /// <param name="ex">The exception to log.</param>
    public static void Log(Exception ex)
    {
        if (ex != null)
        {
            Log(ex.ToString());

            //if (Telemetry.Enabled)
            //    Telemetry.TrackException(ex);
        }
    }

    /// <summary>
    /// Removes all text from the Output Window pane.
    /// </summary>
    public static void Clear()
    {
        if (pane != null)
        {
            pane.Clear();
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
                IVsOutputWindow output = (IVsOutputWindow)_provider.GetService(typeof(SVsOutputWindow));
                output.DeletePane(ref _guid);
                pane = null;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Write(ex);
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
                    IVsOutputWindow output = (IVsOutputWindow)_provider.GetService(typeof(SVsOutputWindow));
                    output.CreatePane(ref _guid, _name, 1, 1);
                    output.GetPane(ref _guid, out pane);
                }
            }
        }

        return pane != null;
    }
}