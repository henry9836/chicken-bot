using ChickenBot.API.Attributes;
using DSharpPlus;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Minesweeper
{
    [Singleton, ServiceDisabled]
    public class MinesweeperService : IEventHandler<MessageCreatedEventArgs>
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

        public Task HandleEventAsync(DiscordClient sender, MessageCreatedEventArgs eventArgs)
        {
            // Henry: "Implement some logic here :3"
            return Task.CompletedTask;
        }
    }
}