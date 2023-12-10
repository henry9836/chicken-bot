using ChickenBot.API.Attributes;
using ChickenBot.Core.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Logging;

namespace ChickenBot.AdminCommands
{
	[Category("Developer")]
	public class RestartBotCommand : BaseCommandModule
	{
		private readonly ILogger<RestartBotCommand> m_Logger;
		private readonly ServiceLifetime m_Lifetime;

		public RestartBotCommand(ILogger<RestartBotCommand> logger, ServiceLifetime lifetime)
		{
			m_Logger = logger;
			m_Lifetime = lifetime;
		}

		[Command("restart"), RequireBotManager]
		public async Task RestartCommand(CommandContext ctx)
		{
			await ctx.RespondAsync("Restarting the bot...");

			m_Logger.LogInformation("Bot manager {user} as initiated an automatic restart of the bot", ctx.User.Username);

			m_Lifetime.Shutdown();
		}
	}
}
