using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace ChickenBot.API.Attributes
{
    public class RequireBotManagerOrStaff : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            var botManager = RequireBotManager.m_BotManagers.Contains(ctx.User.Id);
            var moderator = false;

            if (ctx.Member is not null)
            {
                if ((ctx.Member.Permissions & Permissions.KickMembers) != 0)
                {
                    moderator = true;
                }
            }

            return Task.FromResult(moderator || botManager);
        }
    }
}