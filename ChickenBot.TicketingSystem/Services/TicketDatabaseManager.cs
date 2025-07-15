using System.Timers;
using ChickenBot.TicketingSystem.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Timer = System.Timers.Timer;

namespace ChickenBot.TicketingSystem.Services
{
    public class TicketDatabaseManager : IHostedService
    {
        private readonly IConfiguration m_Configuration;
        private readonly ILogger<TicketDatabaseManager> m_Logger;
        private readonly TicketDatabase m_Database;
        private readonly TicketManager m_Tickets;

        private readonly Timer m_Timer;

        public TicketDatabaseManager(IConfiguration configuration, ILogger<TicketDatabaseManager> logger, TicketDatabase database, TicketManager tickets)
        {
            m_Configuration = configuration;
            m_Logger = logger;
            m_Database = database;
            m_Tickets = tickets;

            m_Timer = new Timer(TimeSpan.FromMinutes(10));
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            m_Logger.LogInformation("Checking schema for ticket table...");
            await m_Database.CheckSchemaAsync();

            m_Timer.Elapsed += TimerElapsed;
            m_Timer.Start();
        }

        private void TimerElapsed(object? sender, ElapsedEventArgs e)
        {
            Task.Run(async () =>
            {
                try
                {
                    await HandleTicketExpires();
                }
                catch (Exception ex)
                {
                    m_Logger.LogError(ex, "Error running ticket expirer process: ");
                }
            });
        }

        private async Task HandleTicketExpires()
        {
            var expireTickets = await m_Database.GetPendingAutoExpireTickets();

            foreach (var ticket in expireTickets)
            {
                await m_Tickets.DoTicketClosure(ticket, true);
                await m_Database.CloseTicket(ticket, 1);
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            m_Timer.Elapsed -= TimerElapsed;
            m_Timer.Stop();
            return Task.CompletedTask;
        }
    }
}
