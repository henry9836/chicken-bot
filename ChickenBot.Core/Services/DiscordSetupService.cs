using ChickenBot.API.Models;
using ChickenBot.Core.Attributes;
using ChickenBot.Core.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Core.Services
{
	[RootService]
	public class DiscordSetupService : IHostedService
	{
		private readonly SubcontextBuilder m_Subcontext;

		private readonly PluginRegistry m_Registry;

		private readonly ILogger<DiscordSetupService> m_Logger;

		private readonly IConfiguration m_Configuration;

		public DiscordSetupService(SubcontextBuilder subcontext, PluginRegistry registry, ILogger<DiscordSetupService> logger, IConfiguration configuration)
		{
			m_Subcontext = subcontext;
			m_Registry = registry;
			m_Logger = logger;
			m_Configuration = configuration;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			m_Subcontext.ChildServices.AddSingleton(DiscordClientFactory);
			m_Subcontext.ChildServices.AddSingleton(CommandsNextFactory);
			return Task.CompletedTask;
		}

		private static DiscordClient DiscordClientFactory(IServiceProvider provider)
		{
			var configuration = provider.GetService<IConfiguration>();
			var loggerFactory = provider.GetService<ILoggerFactory>();

			if (configuration == null)
			{
				throw new InvalidOperationException("Failed to fetch IConfiguration from container");
			}

			var token = configuration["Token"];

			var disordConfig = new DiscordConfiguration()
			{
				Token = token,
				TokenType = TokenType.Bot,
				Intents = DiscordIntents.All,
				LoggerFactory = loggerFactory
			};

			return new DiscordClient(disordConfig);
		}

		private static CommandsNextExtension CommandsNextFactory(IServiceProvider provider)
		{
			var discord = provider.GetRequiredService<DiscordClient>();
			var configuration = provider.GetRequiredService<IConfiguration>();

			var existing = discord.GetCommandsNext();

			if (existing != null)
			{
				return existing;
			}

			var commandsNextConfig = new CommandsNextConfiguration()
			{
				Services = provider,
				CaseSensitive = false,
				EnableMentionPrefix = true,
				StringPrefixes = configuration.GetSection("Prefixes")?.Get<string[]>() ?? new[] { "!" }
			};

			return discord.UseCommandsNext(commandsNextConfig);
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}
	}
}
