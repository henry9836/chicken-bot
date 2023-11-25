namespace ChickenBot.API.Models
{
	public static class AttachmentUtils
	{
		private static readonly string[] m_ImageExtensions = new[] { "png", "jpg", "jpeg", "gif", "tiff", "webp" };

		/// <summary>
		/// Checks if a url is of an image type
		/// </summary>
		/// <param name="url">Url to check</param>
		/// <returns><see langword="true"/> if the url is of an image</returns>
		public static bool IsImage(string url)
		{
			var baseUrl = url.Split('?')[0];
			var ext = Path.GetExtension(baseUrl).Trim('.').ToLowerInvariant();
			return m_ImageExtensions.Contains(ext);
		}
	}
}
