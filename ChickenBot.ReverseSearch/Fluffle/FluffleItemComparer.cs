namespace ChickenBot.ReverseSearch.Fluffle
{
	public class FluffleItemComparer : IComparer<FluffleItem>
	{

		public int Compare(FluffleItem? x, FluffleItem? y)
		{
			var matchPreference = new[] { "exact", "alternative" };
			var platformPreference = new[] { "e621", "furAffinity", "twitter", "furryNetwork" };

			// Compare via nullability
			if (x == null && y == null)
			{
				return 0;
			}
			else if (x == null && y != null)
			{
				return -1;
			}
			else if (x != null && y == null)
			{
				return 1;
			}

			if (x == null || y == null)
			{
				// Not possible, this is to shut up vs
				throw new NullReferenceException();
			}

			// Compare via match preference
			var xMatch = Array.IndexOf(matchPreference, x.Match);
			var yMatch = Array.IndexOf(matchPreference, y.Match);

			var match = yMatch.CompareTo(xMatch);

			if (match != 0)
			{
				return match;
			}

			// Compare via platform preference
			var xPlatform = Array.IndexOf(platformPreference, x.Platform);
			var yPlatform = Array.IndexOf(platformPreference, y.Platform);

			var platform = yPlatform.CompareTo(xPlatform);

			if (xPlatform != yPlatform)
			{
				return platform;
			}

			return x.Score.CompareTo(y.Score);
		}
	}
}
