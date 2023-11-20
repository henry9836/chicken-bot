using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChickenBot.API.Atrributes;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Quotes
{
	public class NsfwQuoteCommand : BaseCommandModule
	{
		public ulong NsfwQuotesChannelID => m_Configuration.GetSection("Channels").GetValue("NsfwQuotes", 0ul);

		private readonly ILogger<NsfwQuoteCommand> m_Logger;
		private readonly IConfiguration m_Configuration;
		private readonly string[] m_AllowedTypes = new[] { "png", "jpg", "jpeg" };

		public NsfwQuoteCommand(ILogger<NsfwQuoteCommand> logger, IConfiguration configuration)
		{
			m_Logger = logger;
			m_Configuration = configuration;
		}

		[Command("nsfw-quote"), Description("Quote a funny screenshot from an nsfw channel"), RequireVerified, RequireNsfw]
		public async Task AddQuoteCommand(CommandContext ctx, [RemainingText] string? text)
		{
			string? attachmentUrl = null;

			if (ctx.Message.Attachments.Count > 0)
			{
				// Quote from uploaded attachment
				attachmentUrl = ctx.Message.Attachments[0].Url;
			}
			else if (text != null)
			{
				// Quote from provided image URL
				var providedUrl = text.Split(' ').First();

				// Ensure it is a http or https URL
				if (Uri.TryCreate(providedUrl, UriKind.Absolute, out var parsedUri) &&
					(parsedUri.Scheme == "https" || parsedUri.Scheme == "http"))
				{
					attachmentUrl = parsedUri.AbsoluteUri;
				}

				// Remove link from description
				text = text.Substring(providedUrl.Length).Trim();
			}

			if (attachmentUrl == null)
			{
				await ctx.RespondAsync("Begawk! You need to attach an image to quote!");
				return;
			}

			var extension = Path.GetExtension(attachmentUrl);

			var isAllowed = m_AllowedTypes.Any(x => extension.EndsWith(x, StringComparison.InvariantCultureIgnoreCase));

			if (!isAllowed)
			{
				await ctx.RespondAsync("Begawk! You have to attach an image!");
				return;
			}

			var quotesChannel = ctx.Guild.GetChannel(NsfwQuotesChannelID);

			if (quotesChannel == null)
			{
				await ctx.RespondAsync("Begawk! I can't seem to find the nsfw-quotes channel");
				return;
			}

			var embed = new DiscordEmbedBuilder()
				.WithImageUrl(attachmentUrl)
				.WithTitle($"Quote by {ctx.Message.Author.Username}")
				.WithFooter($"<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>")
				.WithDescription(text)
				.WithColor(DiscordColor.Red);

			m_Logger.LogInformation("Posted nsfw quote from user {user}: {url} '{message}'", ctx.Message.Author.Username, attachmentUrl, text);
		}
	}
}
