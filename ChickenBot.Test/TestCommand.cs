using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.EventArgs;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Test
{
	public class TestCommand : BaseCommandModule
	{
		[Command("ping")]
		public async Task PingCommand(CommandContext ctx)
		{
			await ctx.RespondAsync("Hello World!");
		}
	}

	public class TestNormalChat : IHostedService
	{
		public TestNormalChat(DiscordClient client, ILogger<TestNormalChat> logger)
		{
			m_Client = client;
			m_Logger = logger;
		}
		public Task StartAsync(CancellationToken cancellationToken)
		{
			m_Client.MessageCreated += M_ClientOnMessageCreated;
			return Task.CompletedTask;
		}

		private Task M_ClientOnMessageCreated(DiscordClient sender, MessageCreateEventArgs args)
		{
			m_Logger.LogInformation("Message Received: {Text}", args.Message.Content);
			return Task.CompletedTask;
		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			return Task.CompletedTask;
		}

		private readonly DiscordClient m_Client;
		private readonly ILogger<TestNormalChat> m_Logger;
	}
}
