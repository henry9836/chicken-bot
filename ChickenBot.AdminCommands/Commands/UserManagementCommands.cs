using ChickenBot.API;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ChickenBot.AdminCommands.Commands
{
    [Category("Admin")]
    public class UserManagementCommands : BaseCommandModule
    {
        [Command("Ban"), RequireBotPermissions(false, DiscordPermission.BanMembers), RequireUserPermissions(false, DiscordPermission.BanMembers)]
        public async Task BanCommand(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Ban Command")
                .WithDescription("Usage:\n > `!ban [@User/UserID] {Reason}`\nBans a user for the specified reason, without clearing any message history.\n-# Hint: You can use the !PurgePeriod command to purge a user's message history")
                .WithColor(DiscordColor.Red);

            await ctx.RespondAsync(embed);
        }

        [Command("Ban"), RequireBotPermissions(false, DiscordPermission.BanMembers), RequireUserPermissions(false, DiscordPermission.BanMembers)]
        public async Task BanCommand(CommandContext ctx, DiscordUser user, [RemainingText] string reason)
        {
            await BanCommand(ctx, user.Id, reason);
        }

        [Command("Ban"), RequireBotPermissions(false, DiscordPermission.BanMembers), RequireUserPermissions(false, DiscordPermission.BanMembers)]
        public async Task BanCommand(CommandContext ctx, ulong userID, [RemainingText] string reason)
        {
            var guild = ctx.Guild;

            if (guild is null || ctx.Member is null)
            {
                return;
            }

            if (!await CheckModerationAllowed(userID, ctx.Member, ctx))
            {
                return;
            }

            await ctx.Guild.BanMemberAsync(userID, reason: reason, messageDeleteDuration: TimeSpan.Zero);
            await ctx.RespondAsync("User banned.");
        }

        [Command("Kick"), RequireBotPermissions(false, DiscordPermission.KickMembers), RequireUserPermissions(false, DiscordPermission.KickMembers)]
        public async Task KickCommand(CommandContext ctx)
        {
            var embed = new DiscordEmbedBuilder()
                .WithTitle("Kick Command")
                .WithDescription("Usage:\n `!kick [@User/UserID] {Reason}`")
                .WithColor(DiscordColor.Red);

            await ctx.RespondAsync(embed);
        }

        [Command("Kick"), RequireBotPermissions(false, DiscordPermission.KickMembers), RequireUserPermissions(false, DiscordPermission.KickMembers)]
        public async Task KickCommand(CommandContext ctx, ulong userID, [RemainingText] string reason)
        {
            if (ctx.Member is null)
            {
                return;
            }

            DiscordMember member;
            try
            {
                var guild = ctx.Guild;
                member = await guild.GetMemberAsync(userID);
            }
            catch (Exception)
            {
                await ctx.RespondAsync("Couldn't find that user in this server to kick.");
                return;
            }

            await KickCommand(ctx, member, reason);
        }

        [Command("Kick"), RequireBotPermissions(false, DiscordPermission.KickMembers), RequireUserPermissions(false, DiscordPermission.KickMembers)]
        public async Task KickCommand(CommandContext ctx, DiscordMember member, [RemainingText] string reason)
        {
            var guild = ctx.Guild;

            if (guild is null || member is null || ctx.Member is null)
            {
                return;
            }

            if (!await CheckModerationAllowed(member.Id, ctx.Member, ctx))
            {
                return;
            }

            await member.RemoveAsync(reason);
            await ctx.RespondAsync("User Kicked.");
        }

        private async Task<bool> CheckModerationAllowed(ulong targetID, DiscordMember caller, CommandContext ctx)
        {
            if (targetID == caller.Id)
            {
                await ctx.RespondAsync("While that would be funny, I don't think you'd really want that.");
                return false;
            }

            if (targetID == ctx.Client.CurrentUser.Id)
            {
                await ctx.RespondAsync("While that would be funny, I think I'll pass");
                return false;
            }

            var self = await caller.Guild.GetMemberAsync(ctx.Client.CurrentUser.Id);

            DiscordMember? target;
            try
            {
                target = await caller.Guild.GetMemberAsync(targetID);
            }
            catch (Exception)
            {
                target = null;
            }

            var maxUserRole = (target is not null) ? target.Roles.Max(x => x.Position) : -1;
            var maxCallerRole = caller.Roles.Max(x => x.Position);
            var maxBotRole = self.Roles.Max(x => x.Position);

            if (maxCallerRole <= maxUserRole)
            {
                var unimpressed = ctx.TryGetEmoji(":toothless_no:");

                if (unimpressed is not null)
                {
                    await ctx.RespondAsync(unimpressed.ToString());
                }
                else
                {
                    await ctx.RespondAsync("They're above than you!");
                }

                return false;
            }

            if (maxBotRole <= maxUserRole)
            {
                var unimpressed = ctx.TryGetEmoji(":toothless_unimpressed:");

                if (unimpressed is not null)
                {
                    await ctx.RespondAsync(unimpressed.ToString());
                }
                else
                {
                    await ctx.RespondAsync("They're above than me!");
                }
                return false;
            }

            return true;
        }
    }
}
