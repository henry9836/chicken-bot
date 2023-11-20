using ChickenBot.API.Atrributes;
using ChickenBot.VerificationSystem.Interfaces;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.VerificationSystem.Models
{
	[Transient(typeof(IUserVerifier))]
	public class UserVerifier : IUserVerifier
	{
		public ulong VerifiedRoleID => m_Configuration.GetSection("Roles:Verified").Get<ulong>();
		public ulong VerifiedChannelID => m_Configuration.GetSection("Channels:Verified").Get<ulong>();

		private readonly IConfiguration m_Configuration;
		private readonly ILogger<UserVerifier> m_Logger;

		public UserVerifier(IConfiguration configuration, ILogger<UserVerifier> logger)
		{
			m_Configuration = configuration;
			m_Logger = logger;
		}

		public bool CheckUserVerified(DiscordMember member)
		{
			return member.Roles.Any(x => x.Id == VerifiedRoleID);
		}

		public async Task<bool> RemoveUserVerificationAsync(DiscordMember member)
		{
			if (!CheckUserVerified(member))
			{
				return false;
			}

			var role = member.Guild.GetRole(VerifiedRoleID);

			if (role == null)
			{
				m_Logger.LogWarning("Couldn't Remove user verification, Verified role '{role}' does not exist in server", VerifiedRoleID);
				return false;
			}

			await member.RevokeRoleAsync(role, $"Deverification requested by moderator");

			return true;
		}

		public async Task<bool> VerifyUserAsync(DiscordMember member)
		{
			if (CheckUserVerified(member))
			{
				return false;
			}

			var role = member.Guild.GetRole(VerifiedRoleID);

			if (role == null)
			{
				m_Logger.LogWarning("Couldn't verify user, Verified role '{role}' does not exist in server", VerifiedRoleID);
				return false;
			}

			await member.GrantRoleAsync(role);
			return true;
		}

		public async Task<bool> AnnounceUserVerification(DiscordMember member)
		{
			var channel = member.Guild.GetChannel(VerifiedChannelID);

			if (channel == null)
			{
				m_Logger.LogWarning("Couldn't announce user verification: verified channel doesn't exist");
				return false;
			}

			var message = new DiscordMessageBuilder()
				.WithContent($"Congrats {member.Mention}! You are now verified! Be-gawk!")
				.WithAllowedMention(new UserMention(member));

			await channel.SendMessageAsync(message);

			return true;
		}
	}
}
