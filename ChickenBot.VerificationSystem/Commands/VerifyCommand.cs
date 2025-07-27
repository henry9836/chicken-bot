using ChickenBot.API.Attributes;
using ChickenBot.API.Interfaces;
using ChickenBot.VerificationSystem.Interfaces;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace ChickenBot.VerificationSystem.Commands
{
    [Group("Verify"), RequireBotManagerOrStaff, Category("Admin")]
    public class VerifyCommand : BaseCommandModule
    {
        private readonly IVerificationCache m_Cache;
        private readonly IUserVerifier m_Verifier;
        private readonly ILogger<VerifyCommand> m_Logger;
        private readonly IUserFlagProvider m_FlagProvider;

        public VerifyCommand(IVerificationCache cache, IUserVerifier verifier, ILogger<VerifyCommand> logger, IUserFlagProvider flagProvider)
        {
            m_Cache = cache;
            m_Verifier = verifier;
            m_Logger = logger;
            m_FlagProvider = flagProvider;
        }

        [GroupCommand, RequirePermissions(false, DiscordPermission.ManageMessages)]
        public async Task VerifyUserCommand(CommandContext ctx)
        {
            if (ctx.Member is null)
            {
                await ctx.RespondAsync("This command cannot be used in DMs");
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Verify User")
                .WithDescription("`Verify user [User Name/ID] {Announce Verification: True/False}`\n" +
                "> Instantly verifies a user\n\n" +
                                 "`Verify allow [User Name/ID]`\n" +
                                 "> Reenables a users ability to be automatically verified\n\n" +
                                 "`Verify deny [User Name/ID]`\n" +
                                 "> Disables a users ability to be automatically verified")

                .WithFooter($"Requested by {ctx.Message.Author?.Username ?? "Unknown Moderator"}");

            await ctx.RespondAsync(embed);
        }

        [Command("allow")]
        public async Task AllowCommand(CommandContext ctx, DiscordMember member)
        {
            await m_FlagProvider.ClearFlagValue(member.Id, "NoVerify");

            await ctx.RespondAsync($"Auto verified enabled for user <@{member.Id}>");
        }

        [Command("deny")]
        public async Task DenyCommand(CommandContext ctx, DiscordMember member)
        {
            await m_FlagProvider.SetFlagValue(member.Id, "NoVerify", null);

            await ctx.RespondAsync($"Auto verified disabled for user <@{member.Id}>");
        }

        [Command("user"), RequirePermissions(false, DiscordPermission.ManageMessages)]   
        public async Task VerifyUserCommand(CommandContext ctx, DiscordMember member)
        {
            await VerifyUserCommand(ctx, member, false);
        }

        [Command("user"), RequirePermissions(false, DiscordPermission.ManageMessages)]
        public async Task VerifyUserCommand(CommandContext ctx, DiscordMember member, bool announce)
        {
            if (ctx.Member is null)
            {
                await ctx.RespondAsync("This command cannot be used in DMs");
                return;
            }

            DiscordEmbedBuilder embed;

            if (m_Verifier.CheckUserVerified(member))
            {
                embed = new DiscordEmbedBuilder()
                    .WithTitle("Couldn't verify user")
                    .WithDescription("User already has the verified role")
                    .WithColor(DiscordColor.Red);

                await ctx.RespondAsync(embed);
                return;
            }

            m_Logger.LogInformation("Moderator {username} ({id}) requested verification on user {target} ({targetID})", ctx.Message.Author?.Username ?? "Unknown Moderator", ctx.Message.Author?.Id, member.Username, member.Id);

            if (!await m_Verifier.VerifyUserAsync(member))
            {
                embed = new DiscordEmbedBuilder()
                    .WithTitle("Couldn't verify user")
                    .WithDescription("Failed to assign the verified role")
                    .WithColor(DiscordColor.Red);
                await ctx.RespondAsync(embed);
                return;
            }

            await m_Cache.ForceVerifyUser(member.Id);

            if (announce)
            {
                await m_Verifier.AnnounceUserVerification(member);
            }

            embed = new DiscordEmbedBuilder()
                .WithTitle("User Verified")
                .WithDescription($"Verified {member.Mention}")
                .WithFooter($"Requested by {ctx.Message.Author?.Username ?? "Unknown Moderator"}");

            await ctx.RespondAsync(embed);
        }
    }
}
