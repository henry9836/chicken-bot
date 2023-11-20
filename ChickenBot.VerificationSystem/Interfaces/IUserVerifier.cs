using DSharpPlus.Entities;

namespace ChickenBot.VerificationSystem.Interfaces
{
	/// <summary>
	/// Manages the Discord Server side of user verification.
	/// </summary>
	public interface IUserVerifier
	{
		/// <summary>
		/// Checks if a user has the verified role
		/// </summary>
		/// <param name="member">Discord member to check</param>
		/// <returns><see langword="true"/> if the user already has the verified role</returns>
		bool CheckUserVerified(DiscordMember member);

		/// <summary>
		/// Grants a user the verified role
		/// </summary>
		/// <param name="member">Discord member to verify</param>
		/// <returns><see langword="true"/> if the user wasn't already verified, and was granted the verified role</returns>
		Task<bool> VerifyUserAsync(DiscordMember member);

		/// <summary>
		/// Removes the verified role from a user
		/// </summary>
		/// <param name="member">Discord member to remove the verified role form</param>
		/// <returns><see langword="true"/> if the user was verified, and no longer is</returns>
		Task<bool> RemoveUserVerificationAsync(DiscordMember member);

		/// <summary>
		/// Sends a message in the verified Discord channel announcing a users verification
		/// </summary>
		/// <param name="member">Discord member to announce verification of</param>
		/// <returns><see langword="true"/> if the verified message could be sent</returns>
		Task<bool> AnnounceUserVerification(DiscordMember member);
	}
}
