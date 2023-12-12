using DSharpPlus.CommandsNext;
using DSharpPlus.Lavalink;

namespace ChickenBot.Music.Interfaces
{
	public interface ITrackProvider
	{
		Task HandlePlayRequest(CommandContext? ctx, string query);

		Task HandleQueueRequest(CommandContext ctx);

		Task<LavalinkTrack?> GetNextTrack();

		Task HandleTrackPlaying(LavalinkTrack track);

		Task HandleShuffleRequest(CommandContext? ctx);

		Task HandleSkip(CommandContext? ctx);
	}
}
