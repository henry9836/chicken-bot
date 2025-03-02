using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace ChickenBot.API.Attributes
{
    /// <summary>
    /// Disables a command or command group
    /// </summary>
    public class DisabledCommandAttribute : CheckBaseAttribute
    {
        public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
        {
            return Task.FromResult(false);
        }
    }
}
