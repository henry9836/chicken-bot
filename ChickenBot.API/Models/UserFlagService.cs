using System.Collections.Concurrent;
using ChickenBot.API.Interfaces;
using ChickenBot.Core.Models;
using Microsoft.Extensions.Logging;

using Timer = System.Timers.Timer;

namespace ChickenBot.API.Models
{
    public class UserFlagService : IUserFlagProvider
    {
        private readonly DatabaseContext m_Context;
        private readonly ILogger<UserFlagService> m_Logger;
        private readonly ConcurrentDictionary<ulong, UserFlagCache> m_Cache = new();
        private readonly Timer m_Timer;

        public UserFlagService(DatabaseContext context, ILogger<UserFlagService> logger)
        {
            m_Context = context;
            m_Logger = logger;

            m_Timer = new Timer(TimeSpan.FromHours(1));
            m_Timer.Elapsed += (_, _) => PurgeCache();
            m_Timer.Start();
        }

        public async Task ClearFlagValue(ulong userID, string flag)
        {
            try
            {
                await ClearFlagValueInternal(userID, flag);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error clearing flag value for ({user}, {flag})", userID, flag);
            }
        }

        public async Task<string?> GetFlagValue(ulong userID, string flag)
        {
            if (TryGetCache(userID, flag, out var cached))
            {
                return cached.Value;
            }

            try
            {
                return await GetFlagValueInternal(userID, flag);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error getting flag value for ({user}, {flag})", userID, flag);
                return null;
            }
        }

        public async Task SetFlagValue(ulong userID, string flag, string? value)
        {
            try
            {
                await UpdateFlagInternal(userID, flag, value);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error setting flag value for ({user}, {flag}) = {value}", userID, flag, value);
            }
        }

        public async Task<bool> IsFlagSet(ulong userID, string flag)
        {
            if (TryGetCache(userID, flag, out var cached))
            {
                return cached.IsSet;
            }

            try
            {
                return await IsFlagSetInternal(userID, flag);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error setting flag set for ({user}, {flag})", userID, flag);
                return false;
            }
        }

        #region "Database Operations"

        private async Task<bool> ClearFlagValueInternal(ulong userID, string flag)
        {
            using var connection = await m_Context.GetConnectionAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM `UserFlags` WHERE `UserID`=@user AND `Flag` = @flag;";

            command.Parameters.AddWithValue("@user", userID);
            command.Parameters.AddWithValue("@flag", flag);

            var modified = await command.ExecuteNonQueryAsync();

            CacheFlagClear(userID, flag);

            return modified > 0;
        }

        private async Task<string?> GetFlagValueInternal(ulong userID, string flag)
        {
            using var connection = await m_Context.GetConnectionAsync();

            using var command = connection.CreateCommand();
            command.CommandText = "SELECT `Value` FROM `UserFlags` WHERE `UserID`=@user AND `Flag` = @flag;";

            command.Parameters.AddWithValue("@user", userID);
            command.Parameters.AddWithValue("@flag", flag);

            var result = await command.ExecuteReaderAsync();

            if (!await result.ReadAsync())
            {
                CacheFlagClear(userID, flag);
                return null;
            }

            var value = result.GetString(0);
            CacheFlagValue(userID, flag, value);
            return value;
        }

        private async Task<bool> UpdateFlagInternal(ulong userID, string flag, string? value)
        {
            using var connection = await m_Context.GetConnectionAsync();

            using var command = connection.CreateCommand();

            command.CommandText = (@"INSERT INTO `UserFlags` (UserID, Flag, Value, Updated) VALUES (@user, @flag, @value, @now)"
                                   + " ON DUPLICATE KEY UPDATE `Value`=@value, `Updated`=@now;");

            command.Parameters.AddWithValue("@user", userID);
            command.Parameters.AddWithValue("@flag", flag);
            command.Parameters.AddWithValue("@value", value);
            command.Parameters.AddWithValue("@now", DateTime.UtcNow);

            var modified = await command.ExecuteNonQueryAsync();

            CacheFlagValue(userID, flag, value);

            return modified > 0;
        }

        public async Task Init(CancellationToken token)
        {
            using var connection = await m_Context.GetConnectionAsync();

            using var command = connection.CreateCommand();
            command.CommandText =
                @"CREATE TABLE IF NOT EXISTS `UserFlags` (
                    `UserID` BIGINT UNSIGNED NOT NULL,
                    `Flag` VarChar(64) NOT NULL,
                    `Value` VarChar(512) NULL,
                    `Updated` DATETIME NOT NULL,
                    PRIMARY KEY (`UserID`, `Flag`)
                );";

            var modified = await command.ExecuteNonQueryAsync();

            if (modified > 0)
            {
                m_Logger.LogInformation("Automatically created user flags table");
            }
        }

        private async Task<bool> IsFlagSetInternal(ulong userID, string flag)
        {
            using var connection = await m_Context.GetConnectionAsync();

            using var command = connection.CreateCommand();
            command.CommandText = @"SELECT `Value` FROM `UserFlags` WHERE `UserID`=@user AND `Flag` = @flag;";

            command.Parameters.AddWithValue("@user", userID);
            command.Parameters.AddWithValue("@flag", flag);

            var result = await command.ExecuteReaderAsync();

            if (!await result.ReadAsync())
            {
                CacheFlagClear(userID, flag);
                return false;
            }

            var value = result.GetString(1);

            CacheFlagValue(userID, flag, value);

            return true;
        }

        #endregion

        #region "Cache"

        private void CacheFlagClear(ulong userID, string flag)
        {
            UserFlagCache cache;
            if (!m_Cache.TryGetValue(userID, out cache!))
            {
                cache = new UserFlagCache();
                m_Cache[userID] = cache;
            }

            cache.SetFlagCleared(flag);
        }

        private void CacheFlagValue(ulong userID, string flag, string? value)
        {
            UserFlagCache cache;
            if (!m_Cache.TryGetValue(userID, out cache!))
            {
                cache = new UserFlagCache();
                m_Cache[userID] = cache;
            }

            cache.SetFlag(flag, value);
        }

        private bool TryGetCache(ulong userID, string flag, out UserFlag value)
        {
            if (m_Cache.TryGetValue(userID, out var cache))
            {
                if (cache.TryGetCached(flag, out value))
                {
                    return true;
                }
            }

            value = default;
            return false;
        }

        private void PurgeCache()
        {
            if (m_Cache.Count == 0)
            {
                return;
            }

            var timeout = TimeSpan.FromHours(12);
            var currentKeys = m_Cache.Keys.ToArray();
            foreach (var key in currentKeys)
            {
                if (!m_Cache.TryGetValue(key, out var cache))
                {
                    continue;
                }

                if (DateTime.Now - cache.LastAccessed > timeout)
                {
                    m_Cache.Remove(key, out _);
                }
            }
        }
    }

    #endregion
}
