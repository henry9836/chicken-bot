using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Serilog;

namespace ChickenBot
{
	public class Startup
	{
		public static IServiceProvider ConfigureServices(IServiceCollection services)
		{
			// Configure logging with Serilog
			Log.Logger = ChickenMarks.CreateLogger(services);

			services.AddLogging(loggingBuilder =>
				loggingBuilder.AddSerilog());

			// Configure Autofac as the DI container
			var builder = AutofacConfig.ConfigureContainer(services);

			var container = builder.Build();

			// Create the AutofacServiceProvider
			var serviceProvider = new AutofacServiceProvider(container);

			return serviceProvider;
		}
	}
}
