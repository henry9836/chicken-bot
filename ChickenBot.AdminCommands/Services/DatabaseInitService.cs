using ChickenBot.AdminCommands.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.AdminCommands.Services
{
    public class DatabaseInitService : IHostedService
    {
        private readonly AdminDBClient m_Database;
        private readonly ILogger<DatabaseInitService> m_Logger;

        public DatabaseInitService(AdminDBClient database, ILogger<DatabaseInitService> logger)
        {
            m_Database = database;
            m_Logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            m_Logger.LogInformation("Initializing database for AdminCommands module...");
            try
            {
                await m_Database.CheckSchema();
                m_Logger.LogInformation("Successfully initialized database");
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Error initializing AdminCommands module database: ");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
