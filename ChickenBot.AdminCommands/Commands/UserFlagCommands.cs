using ChickenBot.API.Attributes;
using ChickenBot.API.Interfaces;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ChickenBot.AdminCommands.Commands
{
    [Group("user-flag"), RequireBotManager, Category("Developer")]
    public class UserFlagCommands : BaseCommandModule
    {
        private readonly IUserFlagProvider m_FlagProvider;

        public UserFlagCommands(IUserFlagProvider flagProvider)
        {
            m_FlagProvider = flagProvider;
        }

        [GroupCommand, RequireBotManager]
        public async Task LeaveMessageCommand(CommandContext ctx, [RemainingText] string? _)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("User Flags")
                .WithDescription("Options:\n" +
                "`user-flag get [user] [flag]`: Gets a user flags value\n" +
                "`user-flag set [user] [flag] [value]`: Sets or creates a user flag\n" +
                "`user-flag clear [user] [flag]`: Clears a user flag");
            await ctx.RespondAsync(embed);
        }

        [Command("get"), RequireBotManager]
        public async Task GetFlagCommand(CommandContext ctx, DiscordUser user, string flag)
        {
            var value = await m_FlagProvider.GetFlagValue(user.Id, flag);

            if (value is null)
            {
                value = await m_FlagProvider.IsFlagSet(user.Id, flag) ? "*Flag Empty*" : "*Not Set*";
            }

            var embed = new DiscordEmbedBuilder()
                 .WithTitle("User Flag")
                 .WithDescription(value)
                 .AddField("User", $"<@{user.Id}>", true)
                 .AddField("Flag", flag, true);

            await ctx.RespondAsync(embed);
        }

        [Command("set"), RequireBotManager]
        public async Task SetFlagCommand(CommandContext ctx, DiscordUser user, string flag, [RemainingText] string? value)
        {
            await m_FlagProvider.SetFlagValue(user.Id, flag, value);
            await ctx.RespondAsync("Flag set.");
        }

        [Command("clear"), RequireBotManager]
        public async Task ClearFlagCommand(CommandContext ctx, DiscordUser user, string flag)
        {
            await m_FlagProvider.ClearFlagValue(user.Id, flag);
            await ctx.RespondAsync("Flag cleared.");
        }
    }
}
