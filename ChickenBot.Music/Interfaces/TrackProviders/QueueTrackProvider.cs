using ChickenBot.API;
using ChickenBot.Music.Models;
using ChickenBot.Music.TrackResolve;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Music.Interfaces.TrackProviders
{
	public class QueueTrackProvider : ITrackProvider
	{
		private readonly Queue<LavalinkTrack> m_Queue = new Queue<LavalinkTrack>();

		private readonly ServerMusicClient m_Parent;

		private readonly ILogger<QueueTrackProvider> m_Logger;

		private readonly LavalinkNodeConnection m_Node;

		private readonly DiscordChannel m_Channel;

		private readonly LavalinkGuildConnection m_Connection;

		private readonly TrackResolver m_Resolver;

		public QueueTrackProvider(ServerMusicClient parent, ILogger<QueueTrackProvider> logger, LavalinkNodeConnection node, DiscordChannel channel, LavalinkGuildConnection connection, TrackResolver resolver)
		{
			m_Parent = parent;
			m_Logger = logger;
			m_Node = node;
			m_Channel = channel;
			m_Connection = connection;
			m_Resolver = resolver;
		}

		public Task<LavalinkTrack?> GetNextTrack()
		{
			if (m_Queue.TryDequeue(out var track))
			{
				return Task.FromResult((LavalinkTrack?)track);
			}
			return Task.FromResult<LavalinkTrack?>(null);
		}

		public async Task HandlePlayRequest(CommandContext? ctx, string query)
		{
			var newTracks = new List<LavalinkTrack>();
			await foreach (var track in m_Resolver.ResolveTracks(query, m_Node, m_Connection))
			{
				m_Queue.Enqueue(track);
				newTracks.Add(track);
			}

			if (newTracks.Count == 0)
			{
				await ctx.TryRespondAsync("Sorry, I couldn't find anything.");
				return;
			}

			var started = await EnsurePlayback();

			if (newTracks.Count == 1)
			{
				if (!started)
				{
					await ctx.TryRespondAsync($"Queued {newTracks[0].Title}");
				}
				return;
			}

			await ctx.TryRespondAsync($"Queued {newTracks.Count} tracks");
		}

		private async Task<bool> EnsurePlayback()
		{
			if (m_Connection.CurrentState.CurrentTrack == null && m_Queue.Count > 0)
			{
				var track = m_Queue.Dequeue();
				await m_Connection.PlayAsync(track);
				return true;
			}
			return false;
		}

		public async Task HandleQueueRequest(CommandContext ctx)
		{
			var currentQueue = m_Queue.ToArray();

			var queue = currentQueue.Take(20).Select(FormatTrackName);
			var currentlyPlaying = FormatTrackName(m_Connection.CurrentState?.CurrentTrack);

			var excess = Math.Max(0, currentQueue.Length - 20);

			var description = string.Join("\n", queue);

			if (excess > 0)
			{
				description += $"\n*+{excess} more tracks in queue*";
			}

			var embed = new DiscordEmbedBuilder()
				.WithTitle("Music Queue")
				.TryAddField("Currently Playing", currentlyPlaying)
				.WithDescription(description)
				.WithRequestedBy(ctx.User);

			await ctx.RespondAsync(embed);
		}

		private string FormatTrackName(LavalinkTrack? track)
		{
			if (track == null)
			{
				return string.Empty;
			}

			if (track.Title.Contains(" - "))
			{
				return $"[{track.Title}](<{track.Uri.AbsoluteUri}>)";
			}
			return $"[{track.Author} - {track.Title}](<{track.Uri.AbsoluteUri}>)";
		}

		public async Task HandleTrackPlaying(LavalinkTrack track)
		{
			await m_Channel.SendMessageAsync($"Now playing: {FormatTrackName(track)}");
		}
	}
}
