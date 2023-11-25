﻿using ChickenBot.API;
using ChickenBot.FlagGame.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ChickenBot.FlagGame
{
	public class FlagCommands : BaseCommandModule
	{
		private ulong BotChannelID => m_Configuration.GetSection("Channels")?.GetValue("bot-spam", 0ul) ?? 0ul;

		private readonly FlagGameRegistry m_GameRegistry;
		private readonly IConfiguration m_Configuration;
		private readonly ILogger<FlagCommands> m_Logger;
		private readonly CountryFlag[] m_Flags;
		private readonly Random m_Random;

		public FlagCommands(FlagGameRegistry gameRegistry, IConfiguration configuration, ILogger<FlagCommands> logger)
		{
			m_GameRegistry = gameRegistry;
			m_Configuration = configuration;
			m_Logger = logger;
			m_Random = new Random();
			m_Flags = LoadFlags();
			m_Logger.LogInformation("Loaded {count} flags.", m_Flags.Length);
		}

		[Command("flag"), Description("Plays a flag game")]
		public async Task FlagCommand(CommandContext ctx)
		{
			if (BotChannelID == 0)
			{
				// Bot spam not set, log a warning and continue anyway
				m_Logger.LogWarning("Bot-Spam channel ID not set!");
			}
			else if (BotChannelID != ctx.Channel.Id)
			{
				// Don't serve the flag command outside of bot spam
				await ctx.RespondAsync("*bonk!*");
				return;
			}

			// Select flag and send it

			var flag = m_Flags[m_Random.Next(0, m_Flags.Length)];

			var embed = new DiscordEmbedBuilder()
				.WithTitle("What flag is this?")
				.WithDescription("Reply with your best guess!")
				.WithImageUrl(flag.Flag)
				.WithRequestedBy(ctx.User);

			var message = await ctx.RespondAsync(embed);

			// Register the game so the flag service can handle responses for it

			var game = new GameInstance(ctx.Channel.Id, message.Id, flag.Country);
			m_GameRegistry.RegisterGame(game);
		}

		/// <summary>
		/// Loads the list of flags from flags.json, or from the embedded resource if the file does not exist
		/// </summary>
		/// <returns>Array of flags available to the flags game</returns>
		private CountryFlag[] LoadFlags()
		{
			string json;
			if (File.Exists("flags.json"))
			{
				// Load via file
				json = File.ReadAllText("flags.json");
			}
			else
			{
				// Load via manifest stream

				using var stream = typeof(FlagCommands).Assembly.GetManifestResourceStream("ChickenBot.FlagGame.flags.json");

				if (stream == null)
				{
					m_Logger.LogWarning("Failed to load flags from assembly manifest!");
					return Array.Empty<CountryFlag>();
				}

				using var reader = new StreamReader(stream);

				json = reader.ReadToEnd();

			}

			// Parse flags
			return JsonConvert.DeserializeObject<CountryFlag[]>(json)
									?? Array.Empty<CountryFlag>();
		}
	}
}
