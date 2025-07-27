using ChickenBot.API;
using ChickenBot.VerificationSystem.Interfaces;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace ChickenBot.VerificationSystem.Commands
{
    [Category("Admin")]
    public class DeverifyCommands : BaseCommandModule
    {
        private readonly IVerificationCache m_Cache;
        private readonly IUserVerifier m_Verifier;
        private readonly ILogger<VerifyCommand> m_Logger;

        public DeverifyCommands(IVerificationCache cache, IUserVerifier verifier, ILogger<VerifyCommand> logger)
        {
            m_Cache = cache;
            m_Verifier = verifier;
            m_Logger = logger;
        }

        [Command("Deverify"), RequirePermissions(false, DiscordPermission.ManageMessages)]
        public async Task DeverifyUserCommand(CommandContext ctx)
        {
            if (ctx.Member is null)
            {
                await ctx.RespondAsync("This command cannot be used in DMs");
                return;
            }

            var embed = new DiscordEmbedBuilder()
                .WithTitle("Deverify User")
                .WithDescription("Usage: `Deverify [User Name/ID] {Threshold Multiplier}`")
                .WithFooter($"Requested by {ctx.Message.Author?.Username ?? "Unknown Moderator"}");

            await ctx.RespondAsync(embed);
        }

        [Command("Deverify"), RequirePermissions(false, DiscordPermission.ManageMessages)]
        public async Task DeverifyUserCommand(CommandContext ctx, DiscordMember member)
        {
            await DeverifyUserCommand(ctx, member, 3);
        }

        [Command("Deverify"), RequirePermissions(false, DiscordPermission.ManageMessages)]
        public async Task DeverifyUserCommand(CommandContext ctx, DiscordMember member, float multiplier)
        {
            if (ctx.Member is null)
            {
                await ctx.RespondAsync("This command cannot be used in DMs");
                return;
            }

            DiscordEmbedBuilder embed;

            if (!m_Verifier.CheckUserVerified(member))
            {
                embed = new DiscordEmbedBuilder()
                    .WithTitle("Couldn't De-verify user")
                    .WithDescription("User isn't verified")
                    .WithColor(DiscordColor.Red);

                await ctx.RespondAsync(embed);
                return;
            }

            m_Logger.LogInformation("Moderator {username} ({id}) requested de-verification of user {target} ({targetID})", ctx.Message.Author?.Username ?? "Unknown Moderator", ctx.Message.Author?.Id, member.Username, member.Id);

            if (!await m_Verifier.RemoveUserVerificationAsync(member))
            {
                embed = new DiscordEmbedBuilder()
                    .WithTitle("Couldn't de-verify user")
                    .WithDescription("Failed to remove the verified role")
                    .WithColor(DiscordColor.Red);
                await ctx.RespondAsync(embed);
                return;
            }

            var info = await m_Cache.ForceRemoveUserVerification(member.Id, multiplier);

            var newThreshold = info.Threshold - info.MessageCount;

            var eligibleTimestamp = ((DateTimeOffset)info.Eligible).ToUnixTimeSeconds();

            embed = new DiscordEmbedBuilder()
                .WithTitle("User De-verified")
                .WithDescription($"De-verified {member.Mention}")
                .AddField("Message Threshold", $"{newThreshold}", true)
                .AddField("Verify Eligible: ", $"<t:{eligibleTimestamp}:R>", true)
                .WithFooter($"Requested by {ctx.Message.Author?.Username ?? "Unknown Moderator"}");

            await ctx.RespondAsync(embed);
        }

        [Command("SetVerificationMultiplier"), RequirePermissions(false, DiscordPermission.ManageMessages)]
        public async Task SetVerificationRequirements(CommandContext ctx)
        {
            var message = new DiscordEmbedBuilder()
                .WithTitle("Set Verification Requirements")
                .WithDescription("Allows you to change the verification requirements for a user, as a value to multiply their requirements by\nA value of `1` resets their verification progress, and a value of `2` would double it\nUsage: `SetVerificationMultiplier [User] [Multiplier]`")
                .WithRequestedBy(ctx.User)
                .WithColor(DiscordColor.Green);

            await ctx.RespondAsync(message);
        }

        [Command("SetVerificationMultiplier"), RequirePermissions(false, DiscordPermission.ManageMessages)]
        public async Task SetVerificationRequirements(CommandContext ctx, DiscordMember member, float multiplier)
        {
            if (ctx.Member is null)
            {
                await ctx.RespondAsync("This command cannot be used in DMs");
                return;
            }

            var message = new DiscordMessageBuilder();

            if (m_Verifier.CheckUserVerified(member))
            {
                if (!await m_Verifier.RemoveUserVerificationAsync(member))
                {
                    var embed2 = new DiscordEmbedBuilder()
                        .WithTitle("Couldn't de-verify user")
                        .WithDescription("Failed to remove the verified role")
                        .WithColor(DiscordColor.Red);
                    message.AddEmbed(embed2);
                }
            }

            var info = await m_Cache.ForceRemoveUserVerification(member.Id, multiplier);

            var newThreshold = info.Threshold - info.MessageCount;

            var eligibleTimestamp = ((DateTimeOffset)info.Eligible).ToUnixTimeSeconds();

            var embed = new DiscordEmbedBuilder()
                .WithTitle("User Verification requirements updated")
                .WithDescription($"User: {member.Mention}")
                .AddField("Message Threshold", $"{newThreshold}", true)
                .AddField("Verify Eligible: ", $"<t:{eligibleTimestamp}:R>", true)
                .WithFooter($"Requested by {ctx.Message.Author?.Username ?? "Unknown Moderator"}")
                .WithColor(DiscordColor.Red);

            message.AddEmbed(embed);

            await ctx.RespondAsync(message);
        }
    }
}
