using Autofac.Extensions.DependencyInjection;
using ChickenBot.Core.Models;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ChickenBot
{
	public class Startup
	{
		public static IServiceProvider ConfigureServices(IServiceCollection services)
		{
			// Configure logging with Serilog
			Log.Logger = CreateLogger();

			services.AddLogging(loggingBuilder =>
				loggingBuilder.AddSerilog());

			// Configure Autofac as the DI container
			var builder = AutofacConfig.ConfigureContainer(services);

			var container = builder.Build();

			// Create the AutofacServiceProvider
			var serviceProvider = new AutofacServiceProvider(container);

			return serviceProvider;
		}

		public static ILogger CreateLogger()
		{
			return new Serilog.LoggerConfiguration()
				.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] [{source}] {Message:lj}{NewLine}{Exception}")
				.Enrich.FromLogContext()
				.MinimumLevel.Debug()
				.Enrich.With<LogSourceEnricher>()
				.WriteTo.File(path: Path.Combine("Logs", "_.log"), rollingInterval: RollingInterval.Day)
				.WriteTo.File(path: Path.Combine("Logs", "Errors", "error.log"), rollingInterval: RollingInterval.Day, restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Warning)
				.CreateLogger();
		}
	}
}
