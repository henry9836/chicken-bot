using System.Net;
using ChickenBot.API;
using ChickenBot.Music.TrackResolve.Models;
using DSharpPlus.Lavalink;

namespace ChickenBot.Music.TrackResolve.Resolvers
{
	public class YouTubeResolver
	{
		[TrackResolver]
		public async Task<LavalinkTrack?> ResolveQuery(LavalinkNodeConnection node, string query)
		{
			var results = await node.Rest.GetTracksAsync(query);

			if (results?.Tracks == null)
			{
				return null;
			}

			var ranked = results.Tracks
				.Select(x => (
					track: x,
					weight: CalculateTrackWeight(x, query) // Calculate and cache track weight, as it will be used a lot
				))
				.OrderByDescending(x => x.weight) // Order by track weight, using cached value
				.Select(x => x.track);            // Remove cached weight

			return ranked.FirstOrDefault();
		}

		[TrackResolver("youtube.com/*", "youtu.be/*", "music.youtube.com/*", "music.youtu.be/*")]
		public async Task<IEnumerable<LavalinkTrack>> ResolvePlaylistVideo(LavalinkNodeConnection node, string v, string list)
		{
			if (list.Length < 24)
			{
				// List IDs shorter than this are temporal, and exist only for a specific account
				throw new TrackResolveFailedException("Provided playlist is private");
			}

			var fullUrl = $"https://www.youtube.com/*/watch?v={WebUtility.UrlEncode(v)}&list={WebUtility.UrlEncode(list)}";

			if (!Uri.TryCreate(fullUrl, UriKind.Absolute, out var uri))
			{
				throw new TrackResolveFailedException();
			}

			return await ResolvePlaylist(node, uri);
		}

		[TrackResolver("youtube.com/*", "youtu.be/*", "music.youtube.com/*", "music.youtu.be/*")]
		public async Task<IEnumerable<LavalinkTrack>> ResolvePlaylist(LavalinkNodeConnection node, string list)
		{
			if (list.Length < 24)
			{
				// List IDs shorter than this are temporal, and exist only for a specific account
				throw new TrackResolveFailedException("Provided playlist is private");
			}

			var fullUrl = $"https://www.youtube.com/*/watch?list={WebUtility.UrlEncode(list)}";

			if (!Uri.TryCreate(fullUrl, UriKind.Absolute, out var uri))
			{
				throw new TrackResolveFailedException();
			}

			return await ResolvePlaylist(node, uri);
		}

		[TrackResolver("youtube.com/*", "youtu.be/*", "music.youtube.com/*", "music.youtu.be/*")]
		public async Task<IEnumerable<LavalinkTrack>> ResolvePlaylist(LavalinkNodeConnection node, Uri uri)
		{
			var result = await node.Rest.GetTracksAsync(uri);

			if (result?.Tracks == null)
			{
				throw new TrackResolveFailedException("Playlist contains no elements");
			}

			return result.Tracks;
		}

		[TrackResolver("youtube.com/*", "youtu.be/*", "music.youtube.com/*", "music.youtu.be/*")]
		public async Task<LavalinkTrack?> ResolveTrack(LavalinkNodeConnection node, string v)
		{
			var fullUrl = $"https://www.youtube.com/*/watch?v={WebUtility.UrlEncode(v)}";

			if (!Uri.TryCreate(fullUrl, UriKind.Absolute, out var uri))
			{
				return null;
			}

			var result = await node.Rest.GetTracksAsync(uri);

			if (result?.Tracks == null)
			{
				return null;
			}

			return result.Tracks.FirstOrDefault();
		}

		private float CalculateTrackWeight(LavalinkTrack track, string query)
		{
			var weight = 0f;

			// Rule: Track should not contain the word 'Live' if the query doesn't
			// This is meant to filter out live performances when the query doesn't ask for one
			if (track.Title.ContainsInvariant("live"))
			{
				if (query.ContainsInvariant("live"))
				{
					weight += 10f;
				}
				else
				{
					weight -= 15f;
				}
			}

			// Rule: Prefer topic channels. These channels are auto-created by YouTube when music labels publish music to YouTube Music.
			// The tracks provided by these channels are typically the published lyric video, instead of music video or music visualiser.
			if (track.Author.ContainsInvariant(" - Topic"))
			{
				weight += 15;
			}

			// Rule: Penalize remixes and edits if they are not specified in the query
			if (track.Title.ContainsInvariant("remix") || track.Title.ContainsInvariant("edit"))
			{
				if (query.ContainsInvariant("remix") || query.ContainsInvariant("edit"))
				{
					weight += 10f;
				}
				else
				{
					weight -= 20f;
				}
			}

			return weight;
		}
	}
}
