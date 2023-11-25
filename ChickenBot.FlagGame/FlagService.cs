using ChickenBot.FlagGame.Models;
using DSharpPlus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.FlagGame
{
	public class FlagService : IHostedService
	{
		private readonly FlagGameRegistry m_GameRegistry;
		private readonly ILogger<FlagService> m_Logger;
		private readonly IConfiguration m_Configuration;
		private readonly DiscordClient m_Discord;

		public FlagService(FlagGameRegistry gameRegistry, ILogger<FlagService> logger, IConfiguration configuration, DiscordClient discord)
		{
			m_GameRegistry = gameRegistry;
			m_Logger = logger;
			m_Configuration = configuration;
			m_Discord = discord;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			m_Discord.MessageCreated += OnMessageCreated;
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			m_Discord.MessageCreated -= OnMessageCreated;
			return Task.CompletedTask;
		}

		private async Task OnMessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs args)
		{
			if (args.Guild == null)
			{
				return;
			}
			var inputAnswer = args.Message.Content.Trim();

			if (inputAnswer.StartsWith('!'))
			{
				return;
			}



			var referencedID = args.Message.ReferencedMessage?.Id ?? 0;



			if (referencedID != 0)
			{
				var answer = m_GameRegistry.TryGetGame(args.Channel.Id, referencedID);


				if (!string.IsNullOrEmpty(answer))


			}





		}
	}
}
