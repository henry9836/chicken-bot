namespace ChickenBot.FlagGame.Models
{
	public class FlagGame
	{
		public ulong ChannelID { get; }
		public ulong MessageID { get; }
		public string Answer { get; }
		public DateTime Posted { get; }

		public FlagGame(ulong channelID, ulong messageID, string answer)
		{
			ChannelID = channelID;
			MessageID = messageID;
			Answer = answer;
			Posted = DateTime.Now;
		}
	}
}
