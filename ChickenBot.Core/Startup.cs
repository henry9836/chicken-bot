using Autofac.Configuration;
using ChickenBot.API.Interfaces;
using ChickenBot.API.Models;
using ChickenBot.Core.Models;
using ChickenBot.Core.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChickenBot.Core
{
	public static class Startup
	{
		public static void ConfigureServices(IServiceCollection services)
		{
			services.AddSingleton<PluginRegistry>();
			services.AddSingleton<SubcontextBuilder>();

			services.AddTransient<DatabaseContext>();

			// Plugin loading pipeline
			services.AddHostedService<AssemblyLoaderService>();     // Load libraries and plugins into the GAC
			services.AddHostedService<PluginRegistrationService>(); // Register types from plugins into the child container
			services.AddHostedService<DiscordSetupService>();       // Register Discord and Commands Next to the container
			services.AddHostedService<BotService>();                // Build child container and run it
		}

		public static void ConfigureLogging(IServiceCollection services)
		{
			var config = new ConfigurationBuilder();
			config.AddJsonFile("config.json", optional: false, reloadOnChange: true);

			services.AddTransient<IConfigEditor>((_) => new ConfigEditor("config.json"));

			var module = new ConfigurationModule(config.Build());

			services.AddSingleton(module.Configuration);
		}
	}
}
