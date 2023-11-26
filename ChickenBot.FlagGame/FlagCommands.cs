using ChickenBot.API;
using ChickenBot.API.Models;
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
			m_Flags = ManifestResourceLoader.LoadResource<CountryFlag[]>("flags.json") 
															?? Array.Empty<CountryFlag>();
			m_GameRegistry.UpdateFlags(m_Flags);
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

			var game = new GameInstance(ctx.Channel.Id, message.Id, flag.Country, async (footer) =>
			{
				embed.WithFooter(footer);
				await message.ModifyAsync(new Optional<DiscordEmbed>(embed.Build()));
			});
			m_GameRegistry.RegisterGame(game);
		}


		
	}
}
