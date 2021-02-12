using System;
using System.Linq;
using FineCodeCoverage;
using System.Diagnostics;
using Microsoft.VisualStudio;
using System.Collections.Generic;
using Microsoft.VisualStudio.Shell;
using System.Diagnostics.CodeAnalysis;
using Microsoft.VisualStudio.Shell.Interop;
using System.ComponentModel.Composition;

[Export(typeof(ILogger))]
public class Logger : ILogger
{
    private IVsOutputWindowPane _pane;
    private IVsOutputWindow _outputWindow;
    private readonly IServiceProvider _serviceProvider;
    private Guid _paneGuid = VSConstants.GUID_BuildOutputWindowPane;

    [ImportingConstructor]
    public Logger([Import(typeof(SVsServiceProvider))]
            IServiceProvider serviceProvider)
    {
        this._serviceProvider = serviceProvider;
        staticLogger = this;
    }

    [SuppressMessage("Usage", "VSTHRD102:Implement internal logic asynchronously")]
    private void LogImpl(object[] message, bool withTitle)
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

    private static Logger staticLogger;
    public static void Log(params string[] message)
    {
        (staticLogger as ILogger).Log(message);
    }

    public void Log(params object[] message)
    {
        LogImpl(message, true);
    }

    void ILogger.Log(params string[] message)
    {
        LogImpl(message, true);
    }

    public void Log(IEnumerable<object> message)
    {
        LogImpl(message.ToArray(), true);
    }

    public void Log(IEnumerable<string> message)
    {
        LogImpl(message.ToArray(), true);
    }

    public void LogWithoutTitle(params object[] message)
    {
        LogImpl(message, false);
    }

    public void LogWithoutTitle(params string[] message)
    {
        LogImpl(message, false);
    }

    public void LogWithoutTitle(IEnumerable<object> message)
    {
        LogImpl(message.ToArray(), false);
    }

    public void LogWithoutTitle(IEnumerable<string> message)
    {
        LogImpl(message.ToArray(), false);
    }
}