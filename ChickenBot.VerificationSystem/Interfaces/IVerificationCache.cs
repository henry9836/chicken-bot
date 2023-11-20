using ChickenBot.VerificationSystem.Models;

namespace ChickenBot.VerificationSystem.Interfaces
{
	/// <summary>
	/// Manages the user verification cache, providing access to check and increment user message counts
	/// </summary>
	public interface IVerificationCache
	{
		/// <summary>
		/// Initializes the verification cache, and runs first-run setup
		/// </summary>
		Task Init();

		/// <summary>
		/// Adds a user to the cache
		/// </summary>
		/// <param name="userID">The Discord account ID of the user</param>
		/// <param name="information">Verification info for the user</param>
		void AddUser(ulong userID, UserInformation information);

		/// <summary>
		/// Tries to get a user from the cache, without touching the database
		/// </summary>
		/// <param name="userID">The Discord account ID of the user</param>
		/// <param name="information">Resulting user information</param>
		/// <returns><see langword="true"/> if the user was present in the cache</returns>
		bool TryGetUser(ulong userID, out UserInformation information);

		/// <summary>
		/// Tries to remove a user from the cache
		/// </summary>
		/// <param name="userID">The Discord account ID of the user</param>
		/// <param name="user">The output user information of the removed user</param>
		/// <returns><see langword="true"/> if the user existed in the cache, and was removed.</returns>
		bool TryRemoveUser(ulong userID, out UserInformation user);

		/// <summary>
		/// Flushes the verified user cache to the database, and trims the cache
		/// </summary>
		Task FlushCacheAsync();

		/// <summary>
		/// Lazy method to increment a user's message count by 1
		/// </summary>
		/// <param name="userID">The Discord account ID of the user</param>
		/// <returns><see langword="true"/> if the user has reached or passed their verification threshold</returns>
		bool IncrementUserMessages(ulong userID);

		/// <summary>
		/// Modifies a user's verification threshold to immediatley verify them
		/// </summary>
		/// <param name="userID">Discord account ID</param>
		Task ForceVerifyUser(ulong userID);

		/// <summary>
		/// Modifies a user's verification threshold to immediatley de-verify them
		/// </summary>
		/// <param name="userID">Discord account ID</param>
		/// <param name="multiplier">The multiplier to apply to the new message threshold, and verification eligibility delay</param>
		Task<UserInformation> ForceRemoveUserVerification(ulong userID, float multiplier);
	}
}
