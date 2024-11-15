using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace ChickenBot.API.Attributes
{
    /// <summary>
    /// Hides a command from help unless it is executed in an NSFW channel
    /// </summary>
    public class HelpNSFWOnlyAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            if (!help)
            {
                // Allow execution when not in help check
                return Task.FromResult(true);
            }

            // enforce nsfw channel only
            return Task.FromResult(ctx.Channel.IsNSFW);
        }
    }
}
