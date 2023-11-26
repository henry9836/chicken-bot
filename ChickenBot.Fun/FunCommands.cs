using ChickenBot.API.Atrributes;
using ChickenBot.API.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ChickenBot.Fun
{
	public class FunCommands : BaseCommandModule
	{
		private readonly ILogger<FunCommands> m_Logger;

		private readonly string[] m_Spice;

		public FunCommands(ILogger<FunCommands> logger)
		{
			m_Logger = logger;
			m_Spice = ManifestResourceLoader.LoadResource<string[]>("spice.json") ?? Array.Empty<string>();
		}

		[Command("Love"), Description("<3")]
		public async Task LoveCommand(CommandContext ctx)
		{
			var replies = new[]
			{
				"*bonk*",
				"*cuddles up next to you*",
				"<:chicken_smile:236628343758389249> *stares at you for several seconds, before flapping away*",
				"*gives you a small flower*",
				":heart:",
				"<:chicken_smile:236628343758389249>"
			};

			await ctx.RespondRandom(replies);
		}

		[Command("cluck"), Aliases("bok", "bawk", "squark"), Hidden]
		public async Task RandomNoisesCommand(CommandContext ctx)
		{
			var replies = new[]
			{
				"cluck",
				"bok",
				"*tilts head in confusion*",
				"bawk",
				"*scratches the ground*",
				"*pecks you*",
				"*flaps wings*"
			};

			await ctx.RespondRandom(replies);
		}

		[Command("talk"), Description("Echos a message as the bot"), RequireBotManager]
		public async Task TalkCommand(CommandContext ctx)
		{
			await ctx.Message.DeleteAsync();
		}
		
		[Command("talk"), Description("Echos a message as the bot"), RequireBotManager]
		public async Task TalkCommand(CommandContext ctx, [RemainingText] string message)
		{
			await ctx.Message.DeleteAsync();
			await ctx.Channel.SendMessageAsync(message);
		}
		
		[Command("pet"), Aliases("feed")]
		public async Task PetCommand(CommandContext ctx)
		{
			var replies = new[]
			{
				"<:chicken_smile:236628343758389249>",
				"*cuddles up into you*",
				"*squawks, coughing up a half digested piece of corn. Looking up at you expectingly*"
			};

			await ctx.RespondRandom(replies);
		}

		[Command("ping"), Aliases("echo")]
		public async Task PingCommand(CommandContext ctx)
		{
			var random = new Random();

			if (random.Next(0, 100) >= 95)
			{
				await ctx.RespondAsync("<:toothless_ping:587068355987505155>");
			}
			else
			{
				await ctx.RespondAsync("Pong!");
			}
		}

		[Command("attack"), Aliases("kill")]
		public async Task AttackCommand(CommandContext ctx, DiscordMember member)
		{
			if (member.Id == ctx.User.Id)
			{
				await ctx.RespondAsync("*Trust nobody, not even yourself*");
				return;
			}

			// ~Tek: Not adding in that part that exempts Nitro from being attacked
			//   that's cringe of you Nitro ;/
			
			// ~Nitro: But Volt said it would be funny :3

			// ~Tek: I am still not impressed
			
			// ~Nitro: :toothless_babyboo:

			if (member.Id == 102606498860896256)
			{
				await ctx.RespondAsync($"*pecks and chases* {ctx.Message.Author.Username}`");
				return;
			}

			await ctx.RespondAsync($"*pecks and chases* {member.Username}`");
		}

		[Command("oldspice"), Description("Tells a joke and nothing else :3")]
		public async Task OldspiceCommand(CommandContext ctx)
		{
			var emojis = new[] {"chicken_smile", "grimmel_yaaas", "hiccup_cage", "lightfury_look",
			"lightfury_smug", "lightfury_wow", "night_fowory", "nightlight_bruh", "teethless",
			"toopliss", "toopliss_retarded", "toopliss_think", "toopliss_upsidedown",
			"toothless_blyat", "toothless_bored", "toothless_cool", "toothless_dab",
			"toothless_drunk", "toothless_fingergun", "toothless_fingerguns", "toothless_flirt",
			"toothless_gimmie", "toothless_laugh", "toothless_joy", "toothless_pog",
			"toothless_plead", "toothless_pepe", "toothless_pain", "toothless_omg",
			"toothless_shrug", "toothless_stare", "toothless_smile", "toothless_skeptic",
			"toothless_troll", "toothless_upright", "toothless_upsidedown", "toothless_wdt",
			"toothless_wheeze", "toothless_wink", "toothless_wow"};


			if (m_Spice.Length == 0)
			{
				m_Logger.LogWarning("No spicy");
				await ctx.RespondAsync(":(");
				return;
			}

			var random = new Random();

			var emojiName = $":{emojis[random.Next(0, emojis.Length)]}:";
			var joke = m_Spice[random.Next(m_Spice.Length)];

			try
			{
				var emoji = DiscordEmoji.FromName(ctx.Client, emojiName, includeGuilds: true);
				await ctx.RespondAsync($"{emoji} {joke}");
			}
			catch
			{
				m_Logger.LogWarning("Failed to find emoji {emoji}", emojiName);
				await ctx.RespondAsync(joke);
			}
		}
	}
}