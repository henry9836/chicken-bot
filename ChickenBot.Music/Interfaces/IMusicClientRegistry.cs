using ChickenBot.Music.Models;
using DSharpPlus.CommandsNext;

namespace ChickenBot.Music.Interfaces
{
	public interface IMusicClientRegistry
	{
		Task<ServerMusicClient?> GetOrOpenClient(CommandContext ctx, bool join = true);
		void RaiseClientDisconnected(ulong guild);
	}
}
