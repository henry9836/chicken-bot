using ChickenBot.API.Attributes;
using ChickenBot.API.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Quotes
{
	public class QuoteCommand : BaseCommandModule
	{
		private ulong QuotesChannelID => m_Configuration.GetSection("Channels").GetValue("Quotes", 0ul);
		private DiscordChannel? m_QuotesChannel;

		private readonly ILogger<QuoteCommand> m_Logger;
		private readonly IConfiguration m_Configuration;

		public QuoteCommand(ILogger<QuoteCommand> logger, IConfiguration configuration)
		{
			m_Logger = logger;
			m_Configuration = configuration;
		}

		[Command("Quote"), Description("Quote a funny screenshot of chat"), RequireVerified]
		public async Task AddQuoteCommand(CommandContext ctx, [RemainingText] string? text)
		{
			if (ctx.Channel.IsNSFW)
			{
				await ctx.RespondAsync("Bonk! If quoting a nsfw item please use nsfw-quote instead");
				return;
			}
			
			string? attachmentUrl = null;

			if (ctx.Message.Attachments.Count > 0)
			{
				// Quote from uploaded attachment
				attachmentUrl = ctx.Message.Attachments[0].Url;
			}
			else if (text != null)
			{
				attachmentUrl = AttachmentUtils.ExtractAttachment(ref text);
			}

			if (attachmentUrl == null)
			{
				await ctx.RespondAsync("Begawk! You need to attach an image to quote!");
				return;
			}

			var isAllowed = AttachmentUtils.IsImage(attachmentUrl);

			if (!isAllowed)
			{
				await ctx.RespondAsync("Begawk! You have to attach an image!");
				return;
			}

			if (m_QuotesChannel is null)
			{
				m_QuotesChannel = ctx.Guild.GetChannel(QuotesChannelID);
				if (m_QuotesChannel is null)
				{
					await ctx.RespondAsync("Begawk! I can't seem to find the quotes channel");
					return;
				}
			}

			var embed = new DiscordEmbedBuilder()
				.WithImageUrl(attachmentUrl)
				.WithTitle($"Quote by {ctx.Message.Author.Username}")
				.WithTimestamp(DateTime.Now)
				.WithDescription($"{text}\n<t:{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}:R>".Trim())
				.WithColor(new DiscordColor("#19b017"));

			await m_QuotesChannel.SendMessageAsync(embed);

			m_Logger.LogInformation("Posted quote from user {user}: {url} '{text}'", ctx.Message.Author.Username, attachmentUrl, text);
		}
	}
}
