using Autofac;
using Autofac.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ChickenBot
{
	public class AutofacConfig
	{
		public static ContainerBuilder ConfigureContainer(IServiceCollection services)
		{
			var builder = new ContainerBuilder();

			// Register the ILogger interface to use Serilog
			builder.Register<ILogger>((c, p) =>
			{
				var loggerFactory = c.Resolve<ILoggerFactory>();
				return loggerFactory.CreateLogger("MyLogger");
			});

			// Register other dependencies using builder.Register...

			// Build the Autofac container
			builder.Populate(services);

			return builder;
		}
	}
}
