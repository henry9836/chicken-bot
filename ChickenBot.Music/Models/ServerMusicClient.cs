using ChickenBot.API;
using ChickenBot.Music.Interfaces;
using ChickenBot.Music.Interfaces.TrackProviders;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using DSharpPlus.Lavalink.EventArgs;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Music.Models
{
	public class ServerMusicClient
	{
		public bool IsDead { get; private set; } = false;
		public ulong ConnectedChannelID => m_Connection.Channel?.Id ?? 0ul;

		public ITrackProvider TrackProvider { get; }

		public DiscordGuild Guild { get; }

		private readonly LavalinkNodeConnection m_Node;

		private readonly ILogger<ServerMusicClient> m_Logger;

		private readonly LavalinkGuildConnection m_Connection;

		private readonly IMusicClientRegistry m_Registry;

		private readonly IServiceProvider m_Provider;

		private readonly DiscordChannel m_Home;

		public ServerMusicClient(DiscordGuild guild, LavalinkNodeConnection node,
			ILogger<ServerMusicClient> logger, LavalinkGuildConnection connection,
			IMusicClientRegistry registry, IServiceProvider provider, DiscordChannel channel, DiscordClient discord)
		{
			Guild = guild;
			m_Node = node;
			m_Logger = logger;
			m_Connection = connection;
			m_Registry = registry;
			m_Provider = provider;
			m_Home = channel;

			// Load default provider
			TrackProvider = m_Provider.ActivateType<QueueTrackProvider>(this, node, connection, channel);

			m_Connection.DiscordWebSocketClosed += OnDisconnect;
			m_Connection.PlaybackFinished += OnPlaybackFinished;
			m_Connection.PlaybackStarted += OnPlaybackStarted;
			discord.VoiceStateUpdated += OnVoiceUpdate;
		}

		private Task OnVoiceUpdate(DiscordClient sender, DSharpPlus.EventArgs.VoiceStateUpdateEventArgs args)
		{
			if (!m_Connection.IsConnected)
			{
				return Task.CompletedTask;
			}

			if (args.Channel.Id != ConnectedChannelID)
			{
				return Task.CompletedTask;
			}

			var users = args.Channel.Users.Count(x => !x.IsBot);
			if (users == 0)
			{
				_ = Task.Run(async () => await StartChannelTimeout(args.Channel));
			}
			return Task.CompletedTask;
		}

		private async Task StartChannelTimeout(DiscordChannel source)
		{
			await Task.Delay(10000);

			var users = source.Users.Count(x => !x.IsBot);

			if (users != 0 || IsDead)
			{
				return;
			}
			await Destroy();
			await m_Home.SendMessageAsync($"Left {source.Name}, since everyone left.");
		}

		private async Task OnPlaybackStarted(LavalinkGuildConnection sender, TrackStartEventArgs args)
		{
			await TrackProvider.HandleTrackPlaying(args.Track);
		}

		private async Task OnPlaybackFinished(LavalinkGuildConnection sender, TrackFinishEventArgs args)
		{
			await PlayNextTrackAsync();
		}

		private async Task<LavalinkTrack?> PlayNextTrackAsync()
		{
			var next = await TrackProvider.GetNextTrack();

			if (next == null)
			{
				return null;
			}

			//var messageTask = TrackProvider.HandleTrackPlaying(next);

			await m_Connection.PlayAsync(next);
			//await messageTask;

			return next;
		}

		private async Task OnDisconnect(LavalinkGuildConnection sender, WebSocketCloseEventArgs args)
		{
			m_Logger.LogDebug("Client disconnected from voice");
			await Destroy();
		}

		public async Task PlayAsync(CommandContext? ctx, string prompt)
		{
			await TrackProvider.HandlePlayRequest(ctx, prompt);
		}

		public async Task SkipAsync(CommandContext? ctx)
		{
			await m_Connection.StopAsync();
		}

		public async Task ShowQueue(CommandContext ctx)
		{
			await TrackProvider.HandleQueueRequest(ctx);
		}

		public async Task Destroy()
		{
			IsDead = true;
			m_Connection.DiscordWebSocketClosed -= OnDisconnect;
			m_Connection.PlaybackFinished -= OnPlaybackFinished;
			m_Connection.PlaybackStarted -= OnPlaybackStarted;

			await m_Connection.DisconnectAsync(true);
			m_Registry.RaiseClientDisconnected(Guild.Id);
		}
	}
}
