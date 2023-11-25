using System.Text;

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

		/// <summary>
		/// Scans a message for URLs, to then extract and remove the first valid URL
		/// </summary>
		/// <param name="content">Message string content to scan</param>
		/// <param name="requireFile">When <see langword="true"/>, only urls with a file extension are extracted, leaving non-file URLs</param>
		/// <returns>The first valid url, or null</returns>
		public static string? ExtractAttachment(ref string content, bool requireFile = true)
		{
			var attachments = ExtractAttachments(ref content, requireFile);
			return attachments.FirstOrDefault();
		}

		/// <summary>
		/// Scans a message for URLs, to then extract and remove from the message
		/// </summary>
		/// <param name="content">Message string content to scan and remove URLs from</param>
		/// <param name="requireFile">When <see langword="true"/>, only urls with a file extension are extracted, leaving non-file URLs</param>
		/// <returns>List of URLs provided in the message</returns>
		public static List<string> ExtractAttachments(ref string content, bool requireFile = true, int limit = -1)
		{
			var attachments = new List<string>();

			var doLimit = limit != -1;
			var sb = new StringBuilder();

			foreach (var word in content.Split(' '))
			{
				if (Uri.TryCreate(word, UriKind.Absolute, out var uri) // If the word is a valid URI
					&& IsWebURI(uri)                                   // If the URI has a scheme of http or https
					&& (requireFile ? IsFileURI(uri) : true)           // And If it matches IsFileURI (So long as requireFile is true)
					&& (doLimit ? limit-- >= 0 : true))                // And If limit >= 0, and decrement limit (So long as doLimit is true)
				{
					attachments.Add(uri.AbsolutePath);
					continue;
				}

				sb.Append(word);
				sb.Append(" ");
			}

			if (attachments.Count > 0)
			{
				content = sb.ToString().Trim();
			}

			return attachments;
		}

		/// <summary>
		/// Checks if a url is a file url
		/// </summary>
		/// <returns>
		/// <see langword="true"/> if the url is a valid uri, and is a file uri
		/// </returns>
		public static bool IsFileURI(string uri)
		{
			if (Uri.TryCreate(uri, UriKind.Absolute, out var url))
			{
				return IsFileURI(url);
			}
			return false;
		}

		/// <summary>
		/// Checks if a url is a file url
		/// </summary>
		public static bool IsFileURI(Uri uri)
		{
			var file = uri.LocalPath.Split('/').Last();
			return file.Contains('.'); // Contains *a* file extension
		}

		/// <summary>
		/// Verifies the specified uri has a scheme of http or https
		/// </summary>
		public static bool IsWebURI(Uri uri)
		{
			return uri.Scheme == "https" || uri.Scheme == "http";
		}
	}
}
