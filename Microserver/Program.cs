using FineCodeCoverage.Core.Utilities;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

namespace FineCodeCoverage
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateHostBuilder(args).Build().Run();
		}

		public static IHostBuilder CreateHostBuilder(string[] args)
		{
			//TODO: Input
			// - Port
			// - LogLevel

			return Host
			.CreateDefaultBuilder(args)
			.ConfigureLogging(builder =>
			{
				builder.AddWebSocketLogger();
			})
			.ConfigureWebHostDefaults(builder =>
			{
				builder.UseStartup<Startup>();
			});
		}
	}
}
