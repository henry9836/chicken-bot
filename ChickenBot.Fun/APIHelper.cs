namespace ChickenBot.Fun
{
	public static class APIHelper
	{
		public static async Task<T?> JsonGet<T>(string url, string jsonPath)
		{
			using var client = new HttpClient();
			using var response = await client.GetAsync(url);
			var json = await response.Content.ReadAsStringAsync();
			if (json == null)
			{
				return default;
			}
			return json.JsonRead<T>(jsonPath);
		}
	}
}
