using ChickenBot.API.Attributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Minesweeper
{
    [Category("Fun")]
    public class MinesweeperCommands : BaseCommandModule
    {
        private readonly IConfiguration m_Configuration;
        private readonly ILogger<MinesweeperCommands> m_Logger;
        private readonly Random m_Random;

        public MinesweeperCommands(IConfiguration configuration, ILogger<MinesweeperCommands> logger)
        {
            m_Configuration = configuration;
            m_Logger = logger;
            m_Random = new Random();
        }

        [Command("minesweeper"), Description("Starts a game of minesweeper"), RequireBotManager]
        public async Task MinesweeperCommand(CommandContext ctx)
        {
            await ctx.RespondAsync("Bang!");
            return;
        }
    }
}