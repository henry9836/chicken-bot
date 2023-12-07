using Newtonsoft.Json;

namespace ChickenBot.ReverseSearch.Fluffle
{
	public class FluffleItem
	{
		public int ID { get; set; }
		public float Score { get; set; }
		public string Match { get; set; } = string.Empty;
		public string Platform { get; set; } = string.Empty;
		public string Location { get; set; } = string.Empty;
		public bool IsSfw { get; set; }

		public FluffleThumbnail? Thumbnail { get; set; }
		public List<FluffleCredit> Credits { get; set; } = new List<FluffleCredit>();

		[JsonIgnore]
		public bool IsExact => Match.Equals("exact", StringComparison.InvariantCultureIgnoreCase);

		[JsonIgnore]
		public bool IsAlternative => Match.Equals("alternative", StringComparison.InvariantCultureIgnoreCase);
	}
}
