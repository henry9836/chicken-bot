using ChickenBot.FlagGame.Models;
using DSharpPlus;
using DSharpPlus.Entities;
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
		private readonly Random m_Random;

		public FlagService(FlagGameRegistry gameRegistry, ILogger<FlagService> logger, IConfiguration configuration, DiscordClient discord)
		{
			m_GameRegistry = gameRegistry;
			m_Logger = logger;
			m_Configuration = configuration;
			m_Discord = discord;
			m_Random = new Random();
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

				if (!string.IsNullOrEmpty(answer) && answer.Equals(inputAnswer, StringComparison.InvariantCultureIgnoreCase))
				{
					// Correct!
					await SendCongradulatoryMessage(args.Message, answer);
				}

				return;
			}

			var lastGame = m_GameRegistry.GetLastGame(args.Channel.Id);

			if (lastGame == null)
			{
				return;
			}

			var timeSinceSent = DateTime.Now - lastGame.Posted;

			if (timeSinceSent > TimeSpan.FromMinutes(3))
			{
				return;
			}
		}

		private async Task SendCongradulatoryMessage(DiscordMessage message, string answer)
		{
			var responses = new string[]
			{
				$"{message.Author.Mention}, happy squawk That's the flag of {answer}"
			};

			var response = responses[m_Random.Next(0, responses.Length)];

			await message.RespondAsync(response);
		}
	}
}
