using ChickenBot.FlagGame.Models;
using DSharpPlus;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.FlagGame
{
	/// <summary>
	/// Handles answers to the flag game, serves responses, and finalizes games
	/// </summary>
	public class FlagService : IHostedService
	{
		private readonly FlagGameRegistry m_GameRegistry;
		private readonly ILogger<FlagService> m_Logger;
		private readonly DiscordClient m_Discord;
		private readonly Random m_Random;

		public FlagService(FlagGameRegistry gameRegistry, ILogger<FlagService> logger, DiscordClient discord)
		{
			m_GameRegistry = gameRegistry;
			m_Logger = logger;
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
			var inputAnswer = args.Message.Content.Trim();

			if (inputAnswer.StartsWith('!'))
			{
				// Don't evaluate commands
				return;
			}

			var referencedID = args.Message.ReferencedMessage?.Id ?? 0;

			if (referencedID != 0)
			{
				// Get game via referenced message
				var game = m_GameRegistry.TryGetGame(args.Channel.Id, referencedID);

				if (game == null)
				{
					// Not a game
					return;
				}

				if (game.Answer.Equals(inputAnswer, StringComparison.InvariantCultureIgnoreCase))
				{
					// Correct!
					m_Logger.LogInformation("User {user} guessed the flag correctly: {flag}", args.Author.Username, game.Answer);
					await SendCongratulatoryMessage(args.Message, game.Answer);
				} else
				{
					// Failiure
					m_Logger.LogInformation("User {user} guessed the flag incorrectly: {flag}", args.Author.Username, game.Answer);
					await SendFailiureMessage(args.Message, game.Answer);
				}

				m_GameRegistry.TryFinalizeGame(game);
				return;
			}

			// No message referenced, try get game via channel context

			var lastGame = m_GameRegistry.GetLastGame(args.Channel.Id);

			if (lastGame == null)
			{
				return;
			}

			var timeSinceSent = DateTime.Now - lastGame.Posted;

			if (timeSinceSent > TimeSpan.FromMinutes(2))
			{
				// Game timed out
				m_GameRegistry.TryFinalizeGame(lastGame);
				return;
			}

			// Evaluate answer, ignore incorrect answers
			if (lastGame.Answer.Equals(inputAnswer, StringComparison.InvariantCultureIgnoreCase))
			{
				m_Logger.LogInformation("Implicit user {user} guessed the flag correctly: {flag}", args.Author.Username, lastGame.Answer);
				await SendCongratulatoryMessage(args.Message, lastGame.Answer);
				m_GameRegistry.TryFinalizeGame(lastGame);
			}
		}

		/// <summary>
		/// Congradulate users on getting the flag correct
		/// </summary>
		private async Task SendCongratulatoryMessage(DiscordMessage message, string answer)
		{
			var responses = new string[]
			{
				$"{message.Author.Mention}, happy squawk That's the flag of {answer}",
				$"*does a small dance*, that's right {message.Author.Mention}, that's the flag of {answer}"
			};

			var response = responses[m_Random.Next(0, responses.Length)];

			await message.RespondAsync(response);
		}

		/// <summary>
		/// This user sucks, laugh at them
		/// </summary>
		private async Task SendFailiureMessage(DiscordMessage message, string answer)
		{
			var responses = new string[]
			{
				$"{message.Author.Mention}, pecks your foot dejectedly That's the flag of {answer}",
				$"*stares at you disappointedly*, that's the flag of {answer}."
			};

			var response = responses[m_Random.Next(0, responses.Length)];

			await message.RespondAsync(response);
		}
	}
}
