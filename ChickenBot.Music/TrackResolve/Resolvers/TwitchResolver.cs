using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChickenBot.Music.TrackResolve.Models;
using DSharpPlus.Lavalink;

namespace ChickenBot.Music.TrackResolve.Resolvers
{
	public class TwitchResolver
	{
		[TrackResolver("twitch.tv/+")]
		public async Task<IEnumerable<LavalinkTrack>> VimeoResolve(LavalinkNodeConnection node, Uri uri)
		{
			return await node.BasicResolve(uri);
		}
	}
}
