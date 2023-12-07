using System.Data;
using ChickenBot.ReverseSearch.Fluffle;

namespace ChickenBot.ReverseSearch.Models
{
	public class ReverseSearchResult
	{
		public ELookupResult Status { get; }

		public FluffleResult? Result { get; }

		public bool HasExactMatch => GetMatches().Any();

		public IEnumerable<FluffleItem> GetMatches()
		{
			if (Result?.Results == null)
			{
				yield break;
			}

			foreach (var item in Result.Results)
			{
				if (item.Match == "exact" || item.Match == "alternative")
				{
					yield return item;
				}
			}
		}

		public IEnumerable<FluffleItem> GetPreferredMatches()
		{
			var comparer = new FluffleItemComparer();
			return GetMatches().OrderByDescending(x => x, comparer);
		}

		public FluffleItem? GetPreferredMatch()
		{
			return GetPreferredMatches().FirstOrDefault();
		}

		public ReverseSearchResult(ELookupResult status, FluffleResult? result)
		{
			Status = status;
			Result = result;
		}

		public static ReverseSearchResult FromStatus(ELookupResult status)
		{
			return new ReverseSearchResult(status, null);
		}

		public static ReverseSearchResult Success(FluffleResult result)
		{
			return new ReverseSearchResult(ELookupResult.Success, result);
		}
	}
}
