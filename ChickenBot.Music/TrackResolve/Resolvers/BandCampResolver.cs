using DSharpPlus.Lavalink;
using ChickenBot.Music.TrackResolve.Models;

namespace ChickenBot.Music.TrackResolve.Resolvers
{
	public class BandCampResolver
	{
		[TrackResolver("*.bandcamp.com/track/*")]
		public async Task<IEnumerable<LavalinkTrack>> ResolveTrack(LavalinkNodeConnection node, Uri uri)
		{
			return await node.BasicResolve(uri);
		}

		[TrackResolver("*.bandcamp.com/album/*")]
		public async Task<IEnumerable<LavalinkTrack>> ResolveAlbum(LavalinkNodeConnection node, Uri uri)
		{
			return await node.BasicResolve(uri);
		}
	}
}
