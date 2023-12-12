using ChickenBot.API;
using ChickenBot.Music.TrackResolve.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Lavalink;
using Microsoft.Extensions.Logging;
using SpotifyAPI.Web;

namespace ChickenBot.Music.TrackResolve.Resolvers
{
	public class SpotifyResolver
	{
		private readonly SpotifyClient m_Spotify;

		private readonly ILogger<SpotifyResolver> m_Logger;

		private const float MinimumAcceptableWeight = 0f;

		public SpotifyResolver(SpotifyClient spotify, ILogger<SpotifyResolver> logger)
		{
			m_Spotify = spotify;
			m_Logger = logger;
		}

		//https://open.spotify.com/track/3j2SaDGyzZhiifh5g5tmNw?si=27984ad093d84c45
		[TrackResolver("open.spotify.com/track/*")]
		public async Task<LavalinkTrack?> GetSpotifyTrack(LavalinkNodeConnection node, Uri uri)
		{
			if (!uri.TryExtractUriResource("/track/:id", out var trackID))
			{
				return null;
			}

			var spotifyTrack = await m_Spotify.Tracks.Get(trackID);

			if (spotifyTrack == null)
			{
				return null;
			}

			// Resolve to YouTube or Soundcloud track
			return await ResolveTrack(node, SpotifyTrack.Create(spotifyTrack));
		}

		[TrackResolver("open.spotify.com/album/*")]
		public async IAsyncEnumerable<LavalinkTrack> GetSpotifyAlbum(LavalinkNodeConnection node, Uri uri, Optional<CommandContext> ctx)
		{

			if (!uri.TryExtractUriResource("/album/:id", out var albumID))
			{
				throw new TrackResolveFailedException();
			}

			await using var _ = await ctx.TryCreateTemporalAsync("Hatching eggs, this might take a while...");

			var album = await m_Spotify.Albums.Get(albumID);

			if (album == null)
			{
				throw new TrackResolveFailedException();
			}

			var count = 0;

			await foreach (var result in ResolveTracks(album.Tracks, node))
			{
				yield return result;

				count++;

				// Limit results to 50
				if (count >= 50)
				{
					yield break;
				}
			}
		}

		[TrackResolver("open.spotify.com/playlist/*")]
		public async IAsyncEnumerable<LavalinkTrack> GetSpotifyPlaylist(LavalinkNodeConnection node, Uri uri, Optional<CommandContext> ctx)
		{
			if (!uri.TryExtractUriResource("/playlist/:id", out var playlistID))
			{
				throw new TrackResolveFailedException();
			}

			await using var _ = await ctx.TryCreateTemporalAsync("Hatching eggs, this might take a while...");

			var playlist = await m_Spotify.Playlists.Get(playlistID);

			if (playlist?.Tracks == null)
			{
				throw new TrackResolveFailedException();
			}

			var count = 0;

			await foreach (var spotifyTrack in m_Spotify.Paginate(playlist.Tracks))
			{
				SpotifyTrack realTrack;

				if (spotifyTrack.Track is FullTrack full)
				{
					realTrack = SpotifyTrack.Create(full);
				}
				else if (spotifyTrack.Track is SimpleTrack simple)
				{
					realTrack = SpotifyTrack.Create(simple);
				}
				else
				{
					continue;
				}

				LavalinkTrack? track;
				try
				{
					track = await ResolveTrack(node, realTrack);
				}
				catch (Exception)
				{
					continue;
				}
				if (track == null)
				{
					continue;
				}

				yield return track;
				count++;

				if (count >= 100)
				{
					yield break;
				}
			}
		}

		private async IAsyncEnumerable<LavalinkTrack> ResolveTracks(Paging<SimpleTrack> tracks, LavalinkNodeConnection node)
		{
			await foreach (var spotifyTrack in m_Spotify.Paginate(tracks))
			{
				LavalinkTrack? track;
				try
				{
					track = await ResolveTrack(node, SpotifyTrack.Create(spotifyTrack));
				}
				catch (Exception)
				{
					continue;
				}
				if (track == null)
				{
					continue;
				}

				yield return track;
			}
		}

		private async Task<LavalinkTrack?> ResolveTrack(LavalinkNodeConnection node, SpotifyTrack spotifyTrack)
		{
			// Get initial search results

			var searchTerm = $"{spotifyTrack.Artists.First().Name} - {spotifyTrack.Name}";

			var youtubeLookup = node.Rest.GetTracksAsync(searchTerm, LavalinkSearchType.Youtube);
			var soundcloudLookup = node.Rest.GetTracksAsync(searchTerm, LavalinkSearchType.SoundCloud);

			var youtubeTracks = await youtubeLookup;
			var soundcloudTracks = await soundcloudLookup;

			var combinedTracks = new List<(LavalinkTrack track, bool fromYoutube)>();

			if (youtubeTracks?.Tracks != null)
			{
				combinedTracks.AddRange(youtubeTracks.Tracks.Select(x => (x, true)));
			}

			if (soundcloudTracks?.Tracks != null)
			{
				combinedTracks.AddRange(soundcloudTracks.Tracks.Select(x => (x, false)));

			}

			// Weigh results to find the best match
			var bestTrack = combinedTracks
				.Select(x => (                     // Calculate weights for each result track
					x.track,
					weight: CalculateResultWeight(x.track, spotifyTrack, x.fromYoutube)
				))
				.OrderByDescending(x => x.weight)  // Order results by weight, highest first
				.FirstOrDefault();                 // Select the first result, being the highest weighted result

			if (bestTrack.weight < MinimumAcceptableWeight)
			{
				return null;
			}
			return bestTrack.track;
		}

		/// <summary>
		/// Calculates the weighting for a <seealso cref="LavalinkTrack"/> in comparison to a Spotify <seealso cref="FullTrack"/>
		/// </summary>
		/// <param name="track">The lavalink track to calculate a weight for</param>
		/// <param name="spotifyTrack">The Spotify track to compare <paramref name="track"/> against, for the purpose of weight calculation</param>
		/// <param name="youtube">A value specifying if <paramref name="track"/> originates from the YouTube platform.
		/// When <see langword="false"/>, it is assumed the track originated from SoundCloud </param>
		/// <returns>A weight value representing the similarity of <paramref name="track"/> to <paramref name="spotifyTrack"/>. A higher value indicates a greater similarity</returns>
		private float CalculateResultWeight(LavalinkTrack track, SpotifyTrack spotifyTrack, bool youtube)
		{
			var weight = 0f;

			// Rule: Prefer topic channels, and prefer soundcloud
			if (youtube)
			{
				if (track.Author.Contains(" - Topic"))
				{
					weight += 10;
				}
			}
			else
			{
				weight += 10f;
			}

			// Rule: Track title contains spotify title
			if (track.Title.Contains(track.Title))
			{
				weight += 10f;
			}

			// Rule: Prefer tracks where the title perfectly match the spotify title
			if (track.Title.Equals(spotifyTrack.Name, StringComparison.InvariantCultureIgnoreCase))
			{
				weight += 5;
			}

			// Rule: prefer tracks where the most artists are represented
			var artistWeight = 10f;
			foreach (var artist in spotifyTrack.Artists)
			{
				if (track.Title.ContainsInvariant(artist.Name) || track.Author.ContainsInvariant(artist.Name))
				{
					weight += artistWeight;
					artistWeight /= 2f;
				}
			}

			// Rule: Track remix/edit status matches
			var spotifyIsRemix = spotifyTrack.Name.ContainsInvariant("remix") || spotifyTrack.Name.ContainsInvariant("edit");
			var trackIsRemix = track.Title.ContainsInvariant("remix") || track.Title.ContainsInvariant("edit");

			if (spotifyIsRemix == trackIsRemix)
			{
				weight += 10f;
			}
			else
			{
				weight -= 10f;
			}

			// Rule: Prefer tracks of similar duration, and penalize tracks with a significantly different duration
			var durationGap = track.Length - TimeSpan.FromMilliseconds(spotifyTrack.DurationMs);

			var durationDifference = TimeSpan.FromSeconds(Math.Abs(durationGap.TotalSeconds));

			var maxDurationWeight = 15f;                          // The maximum weight to give to a track that has a perfect matching duration
			var maxDurationPenalty = 10f;                         // The maximum penalty to give to a track whose duration lies well out of range

			var durationWeightCutOff = TimeSpan.FromSeconds(5);   // The maximum duration period to promote tracks within
			var durationPenaltyCutOff = TimeSpan.FromSeconds(20); // The duration point where tracks receive the maximum weight penalty

			if (durationDifference <= durationWeightCutOff)
			{
				// Promote track
				var similarity = 1f - (float)(durationDifference.TotalSeconds / durationWeightCutOff.TotalSeconds);

				var weightedSimilarity = similarity * maxDurationWeight;

				weight += weightedSimilarity;
			}
			else
			{
				// Penalize track
				var beyond = durationDifference - durationWeightCutOff;

				var value = (beyond.TotalSeconds / durationPenaltyCutOff.TotalSeconds);

				var weightedPenalty = (float)Math.Min(maxDurationPenalty, value * maxDurationPenalty);

				weight -= weightedPenalty;
			}

			return weight;
		}
	}
}
