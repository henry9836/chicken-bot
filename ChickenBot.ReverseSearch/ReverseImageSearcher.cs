using System.Data;
using System.Net.Http.Headers;
using ChickenBot.API.Attributes;
using ChickenBot.ReverseSearch.Fluffle;
using ChickenBot.ReverseSearch.Interfaces;
using ChickenBot.ReverseSearch.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ChickenBot.ReverseSearch
{
	[Transient<IReverseImageSearcher>]
	public class ImageReverseSearcher : IReverseImageSearcher
	{
		private int MaxInputFileSize => m_Configuration.GetSection("ReverseSearch")?.GetValue("MaxFileSize", m_DefaultImageMaxSize) ?? m_DefaultImageMaxSize;

		private readonly IConfiguration m_Configuration;

		private readonly ILogger<ImageReverseSearcher> m_Logger;

		private const int m_DefaultImageMaxSize = 1024 * 1024 * 16; // 16mb

		public ImageReverseSearcher(IConfiguration configuration, ILogger<ImageReverseSearcher> logger)
		{
			m_Configuration = configuration;
			m_Logger = logger;
		}

		/// <summary>
		/// Resizes the input image so that the smaller side is 255px.
		/// </summary>
		/// <remarks>
		/// This is a required pre-processing step for the Ruffle API, to reduce strain on the API.
		/// </remarks>
		/// <param name="sourceImage">Source image data stream</param>
		/// <param name="sourceUrl">Source image url for logging purposes</param>
		/// <returns>Data stream containing the resized image in png format</returns>
		private async Task<MemoryStream?> PreProcessImage(Stream sourceImage, string? sourceUrl = null)
		{
			// It is the callers responsibility to dispose this
			var output = new MemoryStream();

			try
			{
				using var source = await Image.LoadAsync(sourceImage);

				var width = source.Width;
				var height = source.Height;

				int newHeight;
				int newWidth;

				if (width < height)
				{
					var ratio = (float)height / width;
					newWidth = 256;
					newHeight = (int)(256 * ratio);
				}
				else
				{
					var ratio = (float)width / height;
					newHeight = 256;
					newWidth = (int)(256 * ratio);
				}

				source.Mutate((image) => image.Resize(newWidth, newHeight));

				await source.SaveAsPngAsync(output);
				return output;
			}
			catch (InvalidImageContentException)
			{
				output?.Dispose();
				return null; // Provided file is corrupt
			}
			catch (UnknownImageFormatException)
			{
				output?.Dispose();
				return null; // Provided file is not an image
			}
			catch (Exception ex)
			{
				// Unexpected error
				output?.Dispose();
				m_Logger.LogError(ex, "Error while pre-processing image: {image}", sourceUrl);
				return null;
			}
		}

		/// <inheritdoc/>
		public async Task<ReverseSearchResult> SearchAsync(string url, bool nsfw)
		{
			m_Logger.LogInformation("Starting reverse image search for file {url}, IsNSFW: {nsfw}", url, nsfw);
			using var client = new HttpClient();
			client.DefaultRequestHeaders.Add("User-Agent", "Chicken-Bot/v4.0 (By retekt & nitr0glycerin on Discord)");   // Required by the Ruffle API

			m_Logger.LogDebug("Sending request for source image...");
			using var src = await client.GetAsync(url);
			if (src.Content.Headers.ContentLength == null)
			{
				m_Logger.LogInformation("Aborting reverse image search: Unknown file length.");
				return ReverseSearchResult.FromStatus(ELookupResult.UnknownFileSize); // Don't know how large the file is. Don't download it
			}

			if (src.Content.Headers.ContentLength > MaxInputFileSize)
			{
				m_Logger.LogInformation("Aborting reverse image search: File too large.");
				return ReverseSearchResult.FromStatus(ELookupResult.FileTooLarge); // File too large, don't process it
			}

			m_Logger.LogDebug("Pre-processing source image...");

			using var sourceStream = await src.Content.ReadAsStreamAsync(); // Open a byte stream to the resource
			using var resized = await PreProcessImage(sourceStream, url);        // Parse and resize the source image, constraining the smaller side to 255 pixels

			if (resized == null)
			{
				m_Logger.LogInformation("Aborting reverse image search: Image parse failed.");
				return ReverseSearchResult.FromStatus(ELookupResult.InvalidImageType);  // Image parse failed
			}

			resized.Position = 0; // Reset stream position to 0

			using var query = new MultipartFormDataContent();

			using var imageContent = new StreamContent(resized);
			imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");  // Declare mime type as png

			query.Add(imageContent, "file", "file");                       // Attach resized image data
			query.Add(new StringContent("8"), "limit");                    // Specify max results
			query.Add(new StringContent(nsfw.ToString()), "includeNsfw");  // Specify if NSFW results should be considered

			m_Logger.LogDebug("Sending reverse image search request...");

			using var response = await client.PostAsync("https://api.fluffle.xyz/v1/search", query);

			var json = await response.Content.ReadAsStringAsync();

			var result = JsonConvert.DeserializeObject<FluffleResult>(json);

			if (result == null || result.Stats == null)
			{
				m_Logger.LogWarning("Aborting reverse image search: Bad response from API, Code: {code}", result?.Code ?? "Not Provided");
				return ReverseSearchResult.FromStatus(ELookupResult.BadResponse);  // Bad response
			}

			m_Logger.LogDebug("Image reverse search completed");
			return ReverseSearchResult.Success(result);
		}
	}
}
