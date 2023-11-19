using System.Collections.Concurrent;
using System.Data.Common;
using System.Timers;
using ChickenBot.Core.Models;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ChickenBot.VerificationSystem
{
    public struct UserInformation
    {
        public UserInformation(ulong userId, uint messageCount, uint threshold)
        {
            m_UserID = userId;
            m_MessageCount = messageCount;
            m_Threshold = threshold;
        }
        
        public ulong m_UserID;
        public uint m_MessageCount;
        public uint m_Threshold;
    }
    
    public class VerificationMonitor : IHostedService
    {
        private readonly DatabaseContext m_Database;
        private readonly DiscordClient m_DiscordClient;
        private readonly ILogger<VerificationMonitor> m_Logger;
        private readonly ConcurrentDictionary<ulong, UserInformation> m_UserCache;
        private System.Timers.Timer SyncDatabaseTimer;
        
        public VerificationMonitor(DatabaseContext database, DiscordClient client, ILogger<VerificationMonitor> logger)
        {
            m_Database = database;
            m_DiscordClient = client;
            m_Logger = logger;
            m_UserCache = new ConcurrentDictionary<ulong, UserInformation>();
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            m_DiscordClient.MessageCreated += OnMessageCreated;
            
            m_Logger.LogInformation("Verification Plugin Ready.");
            
            // Create Database Sync Timer
            SyncDatabaseTimer = new System.Timers.Timer(30000);
            SyncDatabaseTimer.Enabled = true;
            SyncDatabaseTimer.AutoReset = true;
            SyncDatabaseTimer.Elapsed += SyncDatabaseTimerOnElapsed;
            
            return Task.CompletedTask;
        }

        private async void SyncDatabaseTimerOnElapsed(object? sender, ElapsedEventArgs e)
        {
            m_Logger.LogInformation("Attempting Database Upload...");
            using var dbConnection = await m_Database.GetConnectionAsync();
            {
                foreach (var cachedUser in m_UserCache)
                {
                    await UpdateUserValues(cachedUser.Value.m_UserID, cachedUser.Value.m_MessageCount, cachedUser.Value.m_Threshold, dbConnection);
                }
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            m_Logger.LogInformation("Verification Plugin Stopped.");
            return Task.CompletedTask;
        }
        
        private Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs args)
        {
            ulong authorId = args.Author.Id;
            
            // Run in threadpool
            Task.Run(() => ProcessMessageAuthorId(authorId));
            
            return Task.CompletedTask;
        }

        public async Task<UserInformation?> AttemptToGetUserInfoFromDatabase(ulong authorId, MySqlConnection dbConnection)
        {
            try
            {
                using (var sqlCheckCommand = dbConnection.CreateCommand())
                {
                    // Check if we exist on the database
                    sqlCheckCommand.CommandText = (@"SELECT * FROM `users` WHERE UserID=@ID LIMIT 1");
                    sqlCheckCommand.Parameters.AddWithValue("@ID", authorId);
                    var existResult = await sqlCheckCommand.ExecuteReaderAsync();
                    await existResult.ReadAsync();
                    var id = existResult.GetUInt64("UserID");
                    var messageCount = existResult.GetUInt32("MessageCount");
                    var verificationThreshold = existResult.GetUInt32("VerificationThreshold");
                    
                    UserInformation userInfo = new UserInformation(id, messageCount, verificationThreshold);
                    
                    return (userInfo);
                }
            }
            catch (System.Exception exception)
            {
                m_Logger.LogError(exception.Message);
                m_Logger.LogError(exception.StackTrace);
                return null;
            }
        }
        
        public async Task<bool> UpdateUserValues(ulong authorId, uint messageCount, uint threshold, MySqlConnection dbConnection)
        {
            try
            {
                using (var sqlCheckCommand = dbConnection.CreateCommand())
                {
                    // This will attempt to add a new user into the table if they already exist it will update their message count
                    sqlCheckCommand.CommandText = (@"INSERT INTO `users` (UserID, MessageCount, VerificationThreshold) VALUES (@UserID, @count, @threshold) ON DUPLICATE KEY UPDATE `MessageCount`=@count;");
                    sqlCheckCommand.Parameters.AddWithValue("@UserID", authorId);
                    sqlCheckCommand.Parameters.AddWithValue("@count", messageCount);
                    sqlCheckCommand.Parameters.AddWithValue("@threshold", threshold);
                    var existResult = await sqlCheckCommand.ExecuteNonQueryAsync();
                    return (existResult != 0);
                }
            }
            catch
            {
                return false;
            }
        }
        
        public async Task ProcessMessageAuthorId(ulong authorId)
        {
            // If our user is not in the cache, download/create them
            if (!m_UserCache.ContainsKey(authorId))
            {
                m_Logger.LogInformation("We do not have {authorId} in our dict...", authorId);
                
                using var dbConnection = await m_Database.GetConnectionAsync(); // Opens a new connection to the MySQL Server

                // We can now use this connection 
                if (!await dbConnection.PingAsync())
                {
                    m_Logger.LogError("Could not connect to database!");
                    return;
                }
                
                // Attempt download from database
                var existingUser = await AttemptToGetUserInfoFromDatabase(authorId, dbConnection);
                
                // Add this user to the cache
                if (existingUser.HasValue)
                {
                    m_UserCache.AddOrUpdate(authorId, new UserInformation(existingUser.Value.m_UserID, existingUser.Value.m_MessageCount, existingUser.Value.m_Threshold), (key, existingValue) => new UserInformation(existingValue.m_UserID, existingValue.m_MessageCount + 1, existingValue.m_Threshold));
                    m_Logger.LogInformation("Adding {authorId} from db to dict...", authorId);
                }
                // This user does not exist, create a new entry and add it to cache
                else
                {
                    m_UserCache.AddOrUpdate(authorId, new UserInformation(authorId, 1, 100), (key, existingValue) => new UserInformation(existingValue.m_UserID, existingValue.m_MessageCount + 1, existingValue.m_Threshold));
                    m_Logger.LogInformation("Creating {authorId} in dict...", authorId);
                }
            }
            else
            {
                // Get our user and add one to the message
                UserInformation user = m_UserCache.GetOrAdd(authorId, new UserInformation(authorId, 0, 100));
                user.m_MessageCount += 1;
                
                m_Logger.LogInformation("Got {authorId} in dict", authorId);
                m_Logger.LogInformation("count now {m_MessageCount}", user.m_MessageCount);
                
                // Update Dict
                m_UserCache.AddOrUpdate(authorId, new UserInformation(user.m_UserID, user.m_MessageCount, user.m_Threshold), (key, existingValue) => new UserInformation(user.m_UserID, user.m_MessageCount, user.m_Threshold));
            }
        }
    }
}