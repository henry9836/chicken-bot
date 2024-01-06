using ChickenBot.API.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ChickenBot.Fun.AutoReact
{
	public class AutoReactCommands : BaseCommandModule
	{
		private readonly ReactProvider m_Reacts;

		public AutoReactCommands(ReactProvider reacts)
		{
			m_Reacts = reacts;
		}

		[Command("CreateAutoReact"), RequireBotManager, Hidden]
		public async Task CreateAutoReactCommand(CommandContext ctx, string emoji, DiscordUser user, float chance)
		{
			m_Reacts.CreateAuto(new PersistentReact(emoji, chance, user.Id, null));
			await ctx.RespondAsync("Created auto react");
		}

		[Command("RemoveAutoReacts"), RequireBotManager, Hidden]
		public async Task ClearAutoReacts(CommandContext ctx, DiscordUser user)
		{
			var count = m_Reacts.PersistentReacts.RemoveAll(x => x.User == user.Id);
			await m_Reacts.SaveAsync();
			await ctx.RespondAsync($"Cleared {count} auto reacts.");
		}

		[Command("PregnantMan"), Aliases(":pregnant_man:"), Hidden, RequireBotManager]
		public async Task PregnantManCommand(CommandContext ctx, DiscordUser user)
		{
			var auto = new PersistentReact("pregnant_man", 1 / 300f, user.Id, (instance, provider, activate) =>
			{
				var sinceCreated = DateTime.Now - instance.Created;

				if (sinceCreated.TotalMinutes >= 5)
				{
					// First 5 min is up, destroy discriminator to force 1/300 chance
					instance.Discriminator = null;
				}

				return true;
			});

			m_Reacts.CreateAuto(auto);

			await ctx.RespondAsync($"Made {user.GlobalName ?? user.Username} pregnant.");
		}
	}
}
