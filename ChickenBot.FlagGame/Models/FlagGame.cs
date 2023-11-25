namespace ChickenBot.FlagGame.Models
{
	public class FlagGame
	{
		public ulong ChannelID { get; set; }
		public ulong MessageID { get; set; }
		public string Answer { get; set; }

		public FlagGame(ulong channelID, ulong messageID, string answer)
		{
			ChannelID = channelID;
			MessageID = messageID;
			Answer = answer;
		}
	}
}
