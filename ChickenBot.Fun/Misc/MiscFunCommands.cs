using ChickenBot.API.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ChickenBot.Fun.Misc
{
	[Hidden]
	public class MiscFunCommands : BaseCommandModule
	{
		private readonly ReactState m_Reacts;

		public MiscFunCommands(ReactState reacts)
		{
			m_Reacts = reacts;
		}

		[Command("PregnantMan"), RequireBotManager, Hidden]
		public async Task PregnantManCommand(CommandContext ctx, DiscordUser user)
		{
			if (m_Reacts.PregnantManReact.Contains(user.Id))
			{
				await ctx.RespondAsync($"{user.Username} is already pregnant.");
				return;
			}

			m_Reacts.PregnantManReact.Add(user.Id);

			await ctx.RespondAsync($"Made {user.Username} pregnant.");
		}

		[Command("Abort"), RequireBotManager, Hidden]
		public async Task AbortCommand(CommandContext ctx, DiscordUser user)
		{
			if (!m_Reacts.PregnantManReact.Contains(user.Id))
			{
				await ctx.RespondAsync($"{user.Username} isn't pregnant.");
				return;
			}

			m_Reacts.PregnantManReact.Remove(user.Id);

			await ctx.RespondAsync($"Aborted {user.Username}.");
		}
	}
}
