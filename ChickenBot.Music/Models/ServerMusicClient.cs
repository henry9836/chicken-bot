using System.ComponentModel.DataAnnotations;
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

		public bool IsPaused { get; private set; } = false;

		public ulong ConnectedChannelID => m_Connection.Channel?.Id ?? 0ul;

		public ITrackProvider TrackProvider { get; }

		public DiscordGuild Guild { get; }

		private readonly LavalinkNodeConnection m_Node;

		private readonly ILogger<ServerMusicClient> m_Logger;

		private readonly LavalinkGuildConnection m_Connection;

		private readonly IMusicClientRegistry m_Registry;

		private readonly IServiceProvider m_Provider;

		private readonly DiscordChannel m_Home;

		private DiscordChannel m_VoiceChannel;

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

			m_VoiceChannel = connection.Channel;
		}

		private async Task OnVoiceUpdate(DiscordClient sender, DSharpPlus.EventArgs.VoiceStateUpdateEventArgs args)
		{
			if (IsDead)
			{
				return;
			}

			if (args.Guild.Id != m_VoiceChannel.Guild.Id)
			{
				return;
			}

			if (args.User.IsCurrent)
			{
				if (args.Channel is null)
				{
					// chicken left the channel
					await Destroy();
					return;
				}
				m_Logger.LogDebug("Voice channel for playback was updated.");
				if (args.Channel is not null)
				{
					m_VoiceChannel = args.Channel;
				}
			}

			var users = m_VoiceChannel.Users.Count(x => !x.IsBot);
			if (users == 0)
			{
				m_Logger.LogDebug("All users left the music playback channel, starting disconnect timeout.");
				_ = Task.Run(StartChannelTimeout);
			}
		}

		private async Task StartChannelTimeout()
		{
			await Task.Delay(10000);

			var users = m_VoiceChannel.Users.Count(x => !x.IsBot);

			if (users != 0 || IsDead)
			{
				return;
			}
			m_Logger.LogInformation("No users in music playback channel {channel} for 10 sec, disconnecting", m_VoiceChannel.Name);

			await Destroy();
			await m_Home.SendMessageAsync($"Left {m_VoiceChannel.Name}, since everyone left.");
		}

		private async Task OnPlaybackStarted(LavalinkGuildConnection sender, TrackStartEventArgs args)
		{
			IsPaused = false;
			await TrackProvider.HandleTrackPlaying(args.Track);
		}

		private async Task OnPlaybackFinished(LavalinkGuildConnection sender, TrackFinishEventArgs args)
		{
			if (args.Reason == TrackEndReason.Finished)
			{
				await PlayNextTrackAsync();
			}
		}

		private async Task<LavalinkTrack?> PlayNextTrackAsync()
		{
			if (IsDead)
			{
				return null;
			}

			var next = await TrackProvider.GetNextTrack();

			if (next == null)
			{
				return null;
			}

			if (IsDead)
			{
				return null;
			}

			await m_Connection.PlayAsync(next);

			return next;
		}

		private Task OnDisconnect(LavalinkGuildConnection sender, WebSocketCloseEventArgs args)
		{
			//await Destroy();
			m_Logger.LogDebug("Client disconnected from voice: {reason}", args.Reason);
			return Task.CompletedTask;
		}

		public async Task PlayAsync(CommandContext? ctx, string prompt)
		{
			await TrackProvider.HandlePlayRequest(ctx, prompt);
		}

		public async Task RequestSkipAsync(CommandContext? ctx = null)
		{
			await TrackProvider.HandleSkip(ctx);
		}

		public async Task SkipAsync()
		{
			var next = await PlayNextTrackAsync();
			if (next is null && !IsDead)
			{
				await m_Connection.StopAsync();
			}
		}

		public async Task ShowQueue(CommandContext ctx)
		{
			await TrackProvider.HandleQueueRequest(ctx);
		}

		public async Task Shuffle(CommandContext? ctx = null)
		{
			await TrackProvider.HandleShuffleRequest(ctx);
		}

		public async Task PauseAsync()
		{
			IsPaused = true;
			await m_Connection.PauseAsync();
		}

		public async Task ResumeAsync()
		{
			IsPaused = false;
			await m_Connection.ResumeAsync();
		}

		public async Task<bool> StartPlaying()
		{
			if (IsDead || !m_Connection.IsConnected)
			{
				return false;
			}

			if (m_Connection.CurrentState is null || m_Connection.CurrentState.CurrentTrack is not null)
			{
				return false;
			}

			var nextTrack = await TrackProvider.GetNextTrack();

			if (IsDead || nextTrack is null)
			{
				return false;
			}
			await m_Connection.PlayAsync(nextTrack);
			return true;
		}

		public async Task Destroy()
		{
			m_Logger.LogInformation("Music client destroyed. Last channel: {channel}", m_VoiceChannel.Name);
			IsDead = true;

			m_Registry.RaiseClientDisconnected(Guild.Id);

			m_Connection.DiscordWebSocketClosed -= OnDisconnect;
			m_Connection.PlaybackFinished -= OnPlaybackFinished;
			m_Connection.PlaybackStarted -= OnPlaybackStarted;

			try
			{
				await m_Connection.DisconnectAsync(true);
			}
			catch (NullReferenceException)
			{
				m_Logger.LogWarning("DisconnectAsync threw NullRef exception");
			}
			catch (InvalidOperationException)
			{
			}
		}
	}
}
