using ChickenBot.Music.Interfaces;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Music.Commands
{
	[Category("Music")]
	public class MusicCommands : BaseCommandModule
	{
		private readonly IConfiguration m_Configuration;

		private readonly ILogger<MusicCommands> m_Logger;

		private readonly IServiceProvider m_Provider;

		private readonly IMusicClientRegistry m_ClientRegistry;

		public MusicCommands(IConfiguration configuration, ILogger<MusicCommands> logger, IServiceProvider provider, IMusicClientRegistry clientRegistry)
		{
			m_Configuration = configuration;
			m_Logger = logger;
			m_Provider = provider;
			m_ClientRegistry = clientRegistry;
		}

		[Command("Play"), Description("Plays music")]
		public async Task PlayAsync(CommandContext ctx, [RemainingText] string query)
		{
			var client = await m_ClientRegistry.GetOrOpenClient(ctx);
			if (client == null)
			{
				return;
			}

			await client.PlayAsync(ctx, query);
		}

		[Command("Skip"), Description("Skips the current track")]
		public async Task SkipAsync(CommandContext ctx)
		{
			var client = await m_ClientRegistry.GetOrOpenClient(ctx, join: false);
			if (client == null)
			{
				return;
			}

			await client.SkipAsync(ctx);
		}

		[Command("Queue"), Description("Displays the current queue")]
		public async Task QueueCommand(CommandContext ctx)
		{
			var client = await m_ClientRegistry.GetOrOpenClient(ctx, join: false);
			if (client == null)
			{
				return;
			}
			await client.ShowQueue(ctx);
		}
	}
}
