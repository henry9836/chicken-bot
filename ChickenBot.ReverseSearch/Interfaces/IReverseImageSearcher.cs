using ChickenBot.ReverseSearch.Models;

namespace ChickenBot.ReverseSearch.Interfaces
{
	/// <summary>
	/// Represents an image reverse searching API
	/// </summary>
	public interface IReverseImageSearcher
	{
		/// <summary>
		/// Runs an image reverse search
		/// </summary>
		/// <param name="url">The URL for the image to reverse search</param>
		/// <param name="nsfw">value specifying if nsfw results should be included</param>
		/// <returns>Reverse image search result</returns>
		Task<ReverseSearchResult> SearchAsync(string url, bool nsfw);
	}
}
