using SpotifyAPI.Web;

namespace ChickenBot.Music.TrackResolve.Models
{
	/// <summary>
	/// Represents a spotify track, for the purpose of weight calculation
	/// </summary>
	public struct SpotifyTrack
	{
		public string Name { get; }

		public IEnumerable<SimpleArtist> Artists { get; }

		public int DurationMs { get; }

		public SpotifyTrack(string name, IEnumerable<SimpleArtist> artists, int durationMs)
		{
			Name = name;
			Artists = artists;
			DurationMs = durationMs;
		}

		public static SpotifyTrack Create(FullTrack track)
		{
			return new SpotifyTrack(track.Name, track.Artists, track.DurationMs);
		}

		public static SpotifyTrack Create(SimpleTrack track)
		{
			return new SpotifyTrack(track.Name, track.Artists, track.DurationMs);
		}
	}
}
