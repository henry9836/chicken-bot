using System.Collections.Concurrent;
using System.Collections.Immutable;
using ChickenBot.API.Atrributes;
using ChickenBot.Core.Models;
using ChickenBot.VerificationSystem.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.VerificationSystem.Models
{
	/// <summary>
	/// Manages the user verification cache, providing access to check and increment user message counts
	/// </summary>
	[Singleton(typeof(IVerificationCache))]
	public class VerificationCache : IVerificationCache
	{
		/// <summary>
		/// The minimum verification threshold
		/// </summary>
		/// <remarks>
		/// Configurable in VerifyThreshold:Min, defaults to 100 when not set
		/// </remarks>
		public int VerifyThresholdMin => m_Configuration.GetSection("VerifyThreshold").GetValue<int>("Min", 100);

		/// <summary>
		/// The maximum verification threshold
		/// </summary>
		/// <remarks>
		/// Configurable in VerifyThreshold:Max, defaults to 150 when not set
		/// </remarks>
		public int VerifyThresholdMax => m_Configuration.GetSection("VerifyThreshold").GetValue<int>("Max", 150);

		/// <summary>
		/// The number of seconds after the user sends their first message before they can be verified
		/// </summary>
		/// <remarks>
		/// Configurable in VerifyThreshold:Seconds, defaults to 86400 (1 day)
		/// </remarks>
		public int VerifyEligibleSeconds => m_Configuration.GetSection("VerifyThreshold").GetValue<int>("Seconds", 86400);

		private readonly ILogger<VerificationCache> m_Logger;
		private readonly DatabaseContext m_Context;
		private readonly Random m_Random = new Random();
		private readonly IConfiguration m_Configuration;

		private readonly ConcurrentDictionary<ulong, UserInformation> m_Cache = new();

		public VerificationCache(ILogger<VerificationCache> logger, DatabaseContext context, IConfiguration configuration)
		{
			m_Logger = logger;
			m_Context = context;
			m_Configuration = configuration;
		}

		/// <summary>
		/// Adds a user to the cache
		/// </summary>
		/// <param name="userID">The Discord account ID of the user</param>
		/// <param name="information">Verification info for the user</param>
		public void AddUser(ulong userID, UserInformation information)
		{
			m_Cache[userID] = information;
		}

		/// <summary>
		/// Tries to remove a user from the cache
		/// </summary>
		/// <param name="userID">The Discord account ID of the user</param>
		/// <param name="user">The output user information of the removed user</param>
		/// <returns><see langword="true"/> if the user existed in the cache, and was removed.</returns>
		public bool TryRemoveUser(ulong userID, out UserInformation user)
		{
			return m_Cache.Remove(userID, out user);
		}

		/// <summary>
		/// Tries to get a user from the cache, without touching the database
		/// </summary>
		/// <param name="userID">The Discord account ID of the user</param>
		/// <param name="information">Resulting user information</param>
		/// <returns><see langword="true"/> if the user was present in the cache</returns>
		public bool TryGetUser(ulong userID, out UserInformation information)
		{
			return m_Cache.TryGetValue(userID, out information);
		}

		/// <summary>
		/// Flushes the verified user cache to the database, and trims the cache
		/// </summary>
		public async Task FlushCacheAsync()
		{
			// Create a shallow copy of all current user IDs in the cache
			var userIDs = m_Cache.Keys.ToImmutableArray();

			foreach (var userID in userIDs)
			{
				if (!TryGetUser(userID, out var cachedUserInfo))
				{
					continue;
				}

				// Update DB
				await UpdateUserValues(cachedUserInfo.UserID, cachedUserInfo.MessageCount, cachedUserInfo.Threshold, cachedUserInfo.Eligible);

				// Iterate our user tick
				cachedUserInfo.CycleLevel -= 1;

				m_Logger.LogInformation("cachedUser {Cycle}.", cachedUserInfo.CycleLevel);

				if (cachedUserInfo.IsOutOfCycles())
				{
					TryRemoveUser(userID, out _);
					m_Logger.LogInformation("cachedUser out of cycles.");
				}
			}
			m_Logger.LogInformation("Database Upload Done.");
		}

		/// <summary>
		/// Lazy method to increment a user's message count by 1
		/// </summary>
		/// <param name="userID">The Discord account ID of the user</param>
		/// <returns><see langword="true"/> if the user has reached or passed their verification threshold</returns>
		public bool IncrementUserMessages(ulong userID)
		{
			if (TryGetUser(userID, out var info))
			{
				info.MessageCount++;
				return (info.MessageCount >= info.Threshold) && info.Eligible >= DateTime.UtcNow;
			}

			// User is not in the cache, fetch them from the db, or create a new profile

			// Don't bother blocking the caller, just ignore any messages from the user until the cache is populated
			// This either works and takes < ~100ms, or doesn't work at all. So not much point worrying about a single message

			Task.Run(() => PopulateUserCache(userID));

			return false;
		}

		private async Task PopulateUserCache(ulong userID)
		{
			var info = await GetUserFromDatabase(userID)
				?? new UserInformation(userID, 1, DetermineThreshold(), DetermineEligible());

			AddUser(userID, info);
		}

		private int DetermineThreshold(float multiplier = 1)
		{
			return (int)(m_Random.Next(VerifyThresholdMin, VerifyThresholdMax) * multiplier);
		}

		private DateTime DetermineEligible(float multiplier = 1)
		{
			return DateTime.UtcNow.AddSeconds((int)(VerifyEligibleSeconds * multiplier));
		}

		private async Task<UserInformation?> GetUserFromDatabase(ulong authorId)
		{
			using var dbConnection = await m_Context.GetConnectionAsync();

			try
			{
				using var command = dbConnection.CreateCommand();

				command.CommandText = (@"SELECT * FROM `users` WHERE UserID=@ID");
				command.Parameters.AddWithValue("@ID", authorId);

				var reader = await command.ExecuteReaderAsync();

				if (!reader.HasRows)
				{
					return null;
				}

				await reader.ReadAsync();

				var id = reader.GetUInt64("UserID");
				var messageCount = reader.GetUInt32("MessageCount");
				var verificationThreshold = reader.GetInt32("VerificationThreshold");
				var elligible = reader.GetDateTime("Eligible");

				return new UserInformation(id, messageCount, verificationThreshold, elligible);
			}
			catch (Exception ex)
			{
				m_Logger.LogError(ex, "Error while attemping to read user from cache");
				return null;
			}
		}

		private async Task<bool> UpdateUserValues(ulong authorId, uint messageCount, int threshold, DateTime eligible)
		{
			using var dbConnection = await m_Context.GetConnectionAsync();

			try
			{
				using var command = dbConnection.CreateCommand();
				// This will attempt to add a new user into the table if they already exist it will update their message count
				command.CommandText = (@"INSERT INTO `users` (UserID, MessageCount, VerificationThreshold, Eligible) VALUES (@UserID, @count, @threshold, @eligible)"
										+ " ON DUPLICATE KEY UPDATE `MessageCount`=@count, `VerificationThreshold`=@threshold, `Eligible`=@eligible;");

				command.Parameters.AddWithValue("@UserID", authorId);
				command.Parameters.AddWithValue("@count", messageCount);
				command.Parameters.AddWithValue("@threshold", threshold);
				command.Parameters.AddWithValue("@eligible", eligible);
				await command.ExecuteNonQueryAsync();

				return true;
			}
			catch (Exception ex)
			{
				m_Logger.LogError(ex, "Error while attemping to update user verification info: ");
				return false;
			}
		}

		/// <summary>
		/// Modifies a user's verification threshold to immediatley verify them
		/// </summary>
		/// <param name="userID">Discord account ID</param>
		public async Task ForceVerifyUser(ulong userID)
		{
			UserInformation information;
			if (!m_Cache.TryGetValue(userID, out information))
			{
				var info = await GetUserFromDatabase(userID);

				information = info.HasValue ? info.Value : new UserInformation(userID, 0, 0, DateTime.UtcNow);

				AddUser(userID, information);
			}

			information.Threshold = (int)Math.Min(information.MessageCount, int.MaxValue);
			information.Eligible = DateTime.UtcNow;
		}

		/// <summary>
		/// Modifies a user's verification threshold to immediatley de-verify them
		/// </summary>
		/// <param name="userID">Discord account ID</param>
		public async Task<UserInformation> ForceRemoveUserVerification(ulong userID, float multiplier)
		{
			UserInformation information;
			if (!m_Cache.TryGetValue(userID, out information))
			{
				var info = await GetUserFromDatabase(userID);

				information = info.HasValue ? info.Value : new UserInformation(userID, 0, 0, DateTime.UtcNow);

				AddUser(userID, information);
			}

			information.Threshold = (int)information.MessageCount + DetermineThreshold(multiplier);
			information.Eligible = DetermineEligible(multiplier);

			return information;
		}

		/// <summary>
		/// Initializes the verification cache, and runs first-run setup
		/// </summary>
		public async Task Init()
		{
			using var connection = await m_Context.GetConnectionAsync();

			using var command = connection.CreateCommand();
			command.CommandText =
				@"CREATE TABLE IF NOT EXISTS `users` (
						`UserID` BIGINT UNSIGNED NOT NULL,
						`MessageCount` INT NOT NULL,
						`VerificationThreshold` INT NOT NULL,
						`Eligible` DATETIME NOT NULL,
						PRIMARY KEY (`UserID`)
					)";

			var modified = await command.ExecuteNonQueryAsync();

			if (modified > 0)
			{
				m_Logger.LogInformation("Automatically created verification table");
			}
		}
	}
}
