using FineCodeCoverage.Core.Cobertura;
using FineCodeCoverage.Core.Coverage;
using FineCodeCoverage.Core.Coverlet;
using FineCodeCoverage.Core.Model;
using FineCodeCoverage.Core.MsTestPlatform;
using FineCodeCoverage.Core.OpenCover;
using FineCodeCoverage.Core.ReportGenerator;
using FineCodeCoverage.Core.Utilities;
using FineCodeCoverage.HostedServices;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.IO;

namespace FineCodeCoverage
{
	public class Startup
	{
		public IConfiguration Configuration { get; }
		public ServerSettings ServerSettings { get; }
		
		public Startup(IConfiguration configuration)
		{
			Configuration = configuration;

			ServerSettings = new ServerSettings();
			ServerSettings.AppDataFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), Constants.Code);
			
			Directory.CreateDirectory(ServerSettings.AppDataFolder);
		}

		public void ConfigureServices(IServiceCollection services)
		{
			// dependencies

			services.AddSingleton(ServerSettings);
			services.AddSingleton<ICoverageService, CoverageService>();
			services.AddSingleton<ICoverletService, CoverletService>();
			services.AddSingleton<ICoberturaService, CoberturaService>();
			services.AddSingleton<IOpenCoverService, OpenCoverService>();
			services.AddSingleton<IMsTestPlatformService, MsTestPlatformService>();
			services.AddSingleton<IReportGeneratorService, ReportGeneratorService>();
			
			// controllers

			services
				.AddControllers()
				.AddNewtonsoftJson(o => o.UseMemberCasing());

			// hosted services

			services.AddHostedService<StartupHostedService>();
		}

		public void Configure(IApplicationBuilder app)
		{
			app.UseWebSocketLogger();

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});
		}
	}
}
