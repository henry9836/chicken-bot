using ChickenBot.TicketingSystem.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.TicketingSystem.Services
{
    public class TicketDatabaseManager : IHostedService
    {
        private readonly IConfiguration m_Configuration;
        private readonly ILogger<TicketDatabaseManager> m_Logger;
        private readonly TicketDatabase m_Database;

        public TicketDatabaseManager(IConfiguration configuration, ILogger<TicketDatabaseManager> logger, TicketDatabase database)
        {
            m_Configuration = configuration;
            m_Logger = logger;
            m_Database = database;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            m_Logger.LogInformation("Checking schema for ticket table...");
            await m_Database.CheckSchemaAsync();
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
