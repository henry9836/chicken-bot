using ChickenBot.Music.TrackResolve.Models;
using DSharpPlus.Lavalink;

namespace ChickenBot.Music
{
	public static class Extensions
	{
		/// <summary>
		/// Runs a basic URI lookup from a trusted uri
		/// </summary>
		/// <param name="node">Node to perform the lookup on</param>
		/// <param name="uri">Trusted uri to the requested resource</param>
		/// <returns>Loaded tracks</returns>
		/// <exception cref="ResolveFailedException"></exception>
		public static async Task<IEnumerable<LavalinkTrack>> BasicResolve(this LavalinkNodeConnection node, Uri uri)
		{
			var result = await node.Rest.GetTracksAsync(uri);
			var firstTrack = result?.Tracks?.FirstOrDefault();

			if (result?.Tracks == null || firstTrack == null)
			{
				throw new ResolveFailedException();
			}

			if (!string.IsNullOrEmpty(result.PlaylistInfo.Name))
			{
				return result.Tracks;
			}

			return new[] { firstTrack };
		}
	}
}
