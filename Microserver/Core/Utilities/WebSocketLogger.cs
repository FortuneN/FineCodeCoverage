using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.WebSockets;
using System.Threading.Tasks;

namespace FineCodeCoverage.Core.Utilities
{
	public class WebSocketLogger : ILogger
	{
		public static readonly WebSocketLogger Instance = new WebSocketLogger();

		private static readonly WebSocketState[] RemovalStates = new[]
		{
			WebSocketState.Closed,
			WebSocketState.Aborted,
			WebSocketState.CloseSent,
			WebSocketState.CloseReceived
		};

		private static readonly List<(WebSocket Socket, TaskCompletionSource<object> TaskCompletionSource)> SocketItems = new List<(WebSocket Socket, TaskCompletionSource<object> TaskCompletionSource)>();

		public IDisposable BeginScope<TState>(TState state) => default;

		public bool IsEnabled(LogLevel logLevel) => true;

		public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
		{
			// filter

			if (!IsEnabled(logLevel))
			{
				return;
			}

			// send

			foreach (var item in SocketItems.Where(x => x.Socket.State == WebSocketState.Open))
			{
				try
				{
					// item.Socket.SendAsync()
				}
				catch
				{
					// ignore
				}
			}

			// cleanup

			foreach (var item in SocketItems.Where(x => RemovalStates.Contains(x.Socket.State)).ToArray())
			{
				try
				{
					if (item.TaskCompletionSource.TrySetResult(default))
					{
						SocketItems.Remove(item);
					}
				}
				catch
				{
					// ignore
				}
			}
		}

		public void AddWebSocket(WebSocket socket, TaskCompletionSource<object> taskCompletionSource)
		{
			SocketItems.Add((socket, taskCompletionSource));
		}
	}

	public class WebSocketLoggerProvider : ILoggerProvider
	{
		public static readonly WebSocketLoggerProvider Instance = new WebSocketLoggerProvider();
		public ILogger CreateLogger(string categoryName) => WebSocketLogger.Instance;
		public void Dispose() { }
	}

	public static class WebSocketLoggerExtensions
	{
		public static ILoggingBuilder AddWebSocketLogger(this ILoggingBuilder builder)
		{
			builder.AddProvider(WebSocketLoggerProvider.Instance);
			return builder;
		}

		public static IApplicationBuilder UseWebSocketLogger(this IApplicationBuilder app)
		{
			app.UseWebSockets();

			app.Use(async (context, next) =>
			{
				if (context.Request.Path.Equals("/log", StringComparison.OrdinalIgnoreCase))
				{
					if (context.WebSockets.IsWebSocketRequest)
					{
						using var webSocket = await context.WebSockets.AcceptWebSocketAsync();
						var socketFinishedTcs = new TaskCompletionSource<object>();
						WebSocketLogger.Instance.AddWebSocket(webSocket, socketFinishedTcs);
						await socketFinishedTcs.Task;
					}
					else
					{
						context.Response.StatusCode = 400;
					}
				}
				else
				{
					await next();
				}
			});

			return app;
		}
	}
}