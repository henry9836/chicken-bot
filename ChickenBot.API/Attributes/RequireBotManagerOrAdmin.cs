using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace ChickenBot.API.Attributes
{
	public class RequireBotManagerOrAdmin : CheckBaseAttribute
	{
		public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
		{
			var botManager = RequireBotManager.m_BotManagers.Contains(ctx.User.Id);
			var admin = false;

			if (ctx.Member is not null)
			{
				if ((ctx.Member.Permissions & Permissions.Administrator) != 0)
				{
					admin = true;
				}
			}

			return Task.FromResult(admin || botManager);
		}
	}
}
