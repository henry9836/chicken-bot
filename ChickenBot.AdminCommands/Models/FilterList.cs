namespace ChickenBot.AdminCommands.Models
{
	public class FilterList
	{
		public List<WordMatcher> Matchers { get; } = new List<WordMatcher>();

		public string BlockMessage { get; set; } = string.Empty;
	}
}
