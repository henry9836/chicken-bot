namespace ChickenBot.ReverseSearch.Fluffle
{
	public class FluffleResult
	{
		public string? Code { get; set; }
		public FluffleStatus? Stats { get; set; }
		public List<FluffleItem> Results { get; set; } = new List<FluffleItem>();
	}
}
