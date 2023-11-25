using ChickenBot.API.Atrributes;

namespace ChickenBot.FlagGame.Models
{
	[Singleton]
	public class FlagGameRegistry
	{
		private Dictionary<ulong, List<FlagGame>> m_Games = new Dictionary<ulong, List<FlagGame>>();

		public void RegisterGame(FlagGame game)
		{
			if (!m_Games.TryGetValue(game.ChannelID, out var games))
			{
				games = new List<FlagGame>();
				m_Games[game.ChannelID] = games;
			}

			games.Add(game);
		}

		public string? TryGetGame(ulong channelId, ulong messageId)
		{
			if (!m_Games.TryGetValue(channelId, out var games))
			{
				return null;
			}

			var game = games.FirstOrDefault(x => x.MessageID == messageId);

			return game?.Answer;
		}

		public FlagGame? GetLastGame(ulong channelId)
		{
			if (!m_Games.TryGetValue(channelId, out var games))
			{
				return null;
			}
			return games.LastOrDefault();
		}
	}
}
