using ChickenBot.API.Atrributes;

namespace ChickenBot.FlagGame.Models
{
	[Singleton]
	public class FlagGameRegistry
	{
		public CountryFlag[] Flags { get; private set; } = Array.Empty<CountryFlag>();

		private Dictionary<ulong, List<GameInstance>> m_Games = new Dictionary<ulong, List<GameInstance>>();

		public void UpdateFlags(CountryFlag[] flags)
		{
			Flags = flags;
		}

		public void RegisterGame(GameInstance game)
		{
			if (!m_Games.TryGetValue(game.ChannelID, out var games))
			{
				games = new List<GameInstance>();
				m_Games[game.ChannelID] = games;
			}

			games.Add(game);
		}

		public GameInstance? TryGetGame(ulong channelId, ulong messageId)
		{
			if (!m_Games.TryGetValue(channelId, out var games))
			{
				return null;
			}

			var game = games.FirstOrDefault(x => x.MessageID == messageId);

			return game;
		}

		public GameInstance? GetLastGame(ulong channelId)
		{
			if (!m_Games.TryGetValue(channelId, out var games))
			{
				return null;
			}
			return games.LastOrDefault();
		}

		public bool TryFinalizeGame(GameInstance game)
		{
			if (!m_Games.TryGetValue(game.ChannelID, out var games))
			{
				return false;
			}

			return games.Remove(game);
		}
	}
}
