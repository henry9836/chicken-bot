using DSharpPlus;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Minesweeper
{
    public class MinesweeperService : IHostedService
    {
        private readonly ILogger<MinesweeperService> m_Logger;
        private readonly DiscordClient m_Discord;
        private readonly Random m_Random;
        
        public MinesweeperService(ILogger<MinesweeperService> logger, DiscordClient discord)
        {
            m_Logger = logger;
            m_Discord = discord;
            m_Random = new Random();
        }
        
        public Task StartAsync(CancellationToken cancellationToken)
        {
            m_Discord.MessageCreated += OnMessageCreated;
            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            m_Discord.MessageCreated += OnMessageCreated;
            return Task.CompletedTask;
        }

        private async Task OnMessageCreated(DiscordClient sender, DSharpPlus.EventArgs.MessageCreateEventArgs args)
        {
            // Implement some logic here :3
        }
    }
}