using ChickenBot.VerificationSystem.Interfaces;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace ChickenBot.VerificationSystem.Commands
{
	[Category("Admin")]
	public class VerifyCommand : BaseCommandModule
	{
		private readonly IVerificationCache m_Cache;
		private readonly IUserVerifier m_Verifier;
		private readonly ILogger<VerifyCommand> m_Logger;

		public VerifyCommand(IVerificationCache cache, IUserVerifier verifier, ILogger<VerifyCommand> logger)
		{
			m_Cache = cache;
			m_Verifier = verifier;
			m_Logger = logger;
		}

		[Command("Verify"), RequirePermissions(Permissions.ManageMessages)]
		public async Task VerifyUserCommand(CommandContext ctx)
		{
			if (ctx.Member is null)
			{
				await ctx.RespondAsync("This command cannot be used in DMs");
				return;
			}

			var embed = new DiscordEmbedBuilder()
				.WithTitle("Verify User")
				.WithDescription("Usage: `Verify [User Name/ID] {Announce Verification: True/False}`")
				.WithFooter($"Requested by {ctx.Message.Author.Username}");

			await ctx.RespondAsync(embed);
		}

		[Command("Verify"), RequirePermissions(Permissions.ManageMessages)]
		public async Task VerifyUserCommand(CommandContext ctx, DiscordMember member)
		{
			await VerifyUserCommand(ctx, member, false);
		}

		[Command("Verify"), RequirePermissions(Permissions.ManageMessages)]
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

			m_Logger.LogInformation("Moderator {username} ({id}) requested verification on user {target} ({targetID})", ctx.Message.Author.Username, ctx.Message.Author.Id, member.Username, member.Id);

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
				.WithFooter($"Requested by {ctx.Message.Author.Username}");

			await ctx.RespondAsync(embed);
		}
	}
}
