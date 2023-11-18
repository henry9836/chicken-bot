using ChickenBot.Core.Models;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MySqlConnector;

namespace ChickenBot.VerificationSystem
{
    public class VerificationMonitor : IHostedService
    {
        private readonly DatabaseContext m_Database;
        private readonly DiscordClient m_DiscordClient;
        private readonly ILogger<VerificationMonitor> m_Logger;
        
        public VerificationMonitor(DatabaseContext database, DiscordClient client, ILogger<VerificationMonitor> logger)
        {
            m_Database = database;
            m_DiscordClient = client;
            m_Logger = logger;
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            m_DiscordClient.MessageCreated += OnMessageCreated;
            
            m_Logger.LogInformation("Verification Plugin Ready.");
            return Task.CompletedTask;
        }
        
        public Task StopAsync(CancellationToken cancellationToken)
        {
            m_Logger.LogInformation("Verification Plugin Stopped.");
            return Task.CompletedTask;
        }
        
        private Task OnMessageCreated(DiscordClient sender, MessageCreateEventArgs args)
        {
            m_Logger.LogInformation("VMessage Received: {Text}", args.Message.Content);

            ulong authorId = args.Author.Id;
            ProcessMessageAuthorId(authorId);
            
            return Task.CompletedTask;
        }
        
        public async Task ProcessMessageAuthorId(ulong authorId)
        {
            using var dbConnection = await m_Database.GetConnectionAsync(); // Opens a new connection to the MySQL Server

            // We can now use this connection 
            if (!await dbConnection.PingAsync())
            {
                m_Logger.LogError("Could not connect to database!");
                return;
            }
            
            m_Logger.LogInformation("Connected TO DATABASE BBY!");
            using (var sqlCheckCommand = dbConnection.CreateCommand())
            {
                // Check if we exist on the database
                sqlCheckCommand.CommandText = (@"SELECT 1 FROM users WHERE UserId=@ID");
                sqlCheckCommand.Parameters.AddWithValue("@ID", authorId);
                var existResult = await sqlCheckCommand.ExecuteReaderAsync();
                
                // If we do not then add to the table, set count to one and set a random threshold 100 + config, min/max
                if (!existResult.HasRows)
                {
                    using (var sqlCreateUserCommand = dbConnection.CreateCommand())
                    {
                        sqlCreateUserCommand.CommandText = (@"INSERT INTO users (UserID, MessageCount, VerificationThreshold) VALUES (@ID, @MessageCount, @VerificationThreshold)");
                        sqlCreateUserCommand.Parameters.AddWithValue("@ID", authorId);
                        sqlCreateUserCommand.Parameters.AddWithValue("@MessageCount", 1);
                        //TODO: UPDATE THIS LATER TO USE RANDOM NUMBER AND NOT 5 LOL
                        sqlCreateUserCommand.Parameters.AddWithValue("@VerificationThreshold", 5);
                        
                        m_Logger.LogInformation("Executing SQL query: {Query}", sqlCreateUserCommand.CommandText);

                        var newUserResult = await sqlCreateUserCommand.ExecuteNonQueryAsync();
                        if (newUserResult == 0)
                        {
                            m_Logger.LogError("Could not insert new member into database!");
                        }
                    }
                    return;
                }
            
                // If we do then iterate our timer by one and alter, if we are beyond our threshold add the verification role
                else
                {
                    
                }
            }
        }
    }
}