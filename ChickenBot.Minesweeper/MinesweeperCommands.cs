using ChickenBot.API;
using ChickenBot.API.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Minesweeper
{
    [Category("Fun")]
    public class MinesweeperCommands : BaseCommandModule
    {
        private ulong BotChannelID => m_Configuration.GetSection("Channels")?.GetValue("bot-spam", 0ul) ?? 0ul;
        
        private readonly IConfiguration m_Configuration;
        private readonly ILogger<MinesweeperCommands> m_Logger;
        private readonly Random m_Random;
        
        public MinesweeperCommands(IConfiguration configuration, ILogger<MinesweeperCommands> logger)
        {
            m_Configuration = configuration;
            m_Logger = logger;
            m_Random = new Random();
        }

        [Command("minesweeper"), Description("Starts a game of minesweeper")]
        public async Task MinesweeperCommand(CommandContext ctx)
        {
            if (BotChannelID == 0)
            {
                // Bot spam not set, log a warning and continue anyway
                m_Logger.LogWarning("Bot-Spam channel ID not set!");
            }
            else if (BotChannelID != ctx.Channel.Id)
            {
                // Don't serve the flag command outside of bot spam
                await ctx.RespondAsync("*There area isn't known for land mines, maybe have a look into bot-spawm*");
                return;
            }
            
            await ctx.RespondAsync("Bang!");
            return;
        }
    }
}