using System.Text;
using System.Text.RegularExpressions;
using ChickenBot.API.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using DSharpPlus.Exceptions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChickenBot.API
{
	public static class Extensions
	{
		public static DiscordEmbedBuilder WithRequestedBy(this DiscordEmbedBuilder builder, DiscordUser? user)
		{
			if (user is not null)
			{
				builder.WithFooter($"Requested by {user.Username}");
			}
			return builder;
		}

		/// <summary>
		/// Conditionally adds a field to the embed, so long as the value is not null or empty
		/// </summary>
		public static DiscordEmbedBuilder TryAddField(this DiscordEmbedBuilder builder, string name, string? value, bool inline = false)
		{
			if (!string.IsNullOrWhiteSpace(value))
			{
				builder.AddField(name, value, inline);
			}

			return builder;
		}

		/// <summary>
		/// Tries to react to a message
		/// </summary>
		/// <param name="emojiName">Emoji name</param>
		/// <param name="ctx">The message to react to</param>
		public static async Task TryReactAsync(this CommandContext ctx, string emojiName)
		{
			await TryReactAsync(ctx.Message, ctx.Client, emojiName);
		}

		/// <summary>
		/// Tries to react to a message
		/// </summary>
		/// <param name="message">Message to react to</param>
		/// <param name="emojiName">Emoji name</param>
		/// <param name="client">Discord client</param>
		public static async Task TryReactAsync(this DiscordMessage message, DiscordClient client, string emojiName)
		{
			if (!emojiName.StartsWith(":"))
			{
				emojiName = ':' + emojiName;
			}
			if (!emojiName.EndsWith(":"))
			{
				emojiName = emojiName + ':';
			}

			try
			{
				var emoji = DiscordEmoji.FromName(client, emojiName);

				if (emoji is not null)
				{
					await message.CreateReactionAsync(emoji);
				}
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (NotFoundException)
			{
			}
			catch (BadRequestException)
			{
			}
			catch (ServerErrorException)
			{
			}
			catch (ArgumentException)
			{
			}
		}

		/// <summary>
		/// Tries to remove a reaction on a message, sent by the bot
		/// </summary>
		/// <param name="emojiName">Emoji name</param>
		/// <param name="ctx">The message to react to</param>
		public static async Task TryRemoveReactAsync(this CommandContext ctx, string emojiName)
		{
			await TryRemoveReactAsync(ctx.Message, ctx.Client, emojiName);
		}

		/// <summary>
		/// Tries to remove a reaction on a message, sent by the bot
		/// </summary>
		/// <param name="emojiName">Emoji name</param>
		/// <param name="emojiName">Emoji name</param>
		/// <param name="client">Discord client</param>
		public static async Task TryRemoveReactAsync(this DiscordMessage message, DiscordClient client, string emojiName)
		{
			if (!emojiName.StartsWith(":"))
			{
				emojiName = ':' + emojiName;
			}
			if (!emojiName.EndsWith(":"))
			{
				emojiName = emojiName + ':';
			}

			try
			{
				var emoji = DiscordEmoji.FromName(client, emojiName);

				if (emoji is not null)
				{
					await message.DeleteOwnReactionAsync(emoji);
				}
			}
			catch (UnauthorizedAccessException)
			{
			}
			catch (NotFoundException)
			{
			}
			catch (BadRequestException)
			{
			}
			catch (ServerErrorException)
			{
			}
			catch (ArgumentException)
			{
			}
		}

		/// <summary>
		/// Conditionally adds a field to the embed, so long as the value is not null or empty
		/// </summary>
		public static DiscordEmbedBuilder TryWithThumbnail(this DiscordEmbedBuilder builder, string? url)
		{
			if (!string.IsNullOrWhiteSpace(url))
			{
				builder.WithThumbnail(url);
			}

			return builder;
		}

		public static string Pluralize(this int value)
		{
			if (value == 1)
			{
				return string.Empty;
			}
			return "s";
		}

		public static string Pluralize(this double value)
		{
			if (value == 1)
			{
				return string.Empty;
			}
			return "s";
		}

		public static async Task TryRespondAsync(this CommandContext? ctx, string message)
		{
			if (ctx is not null)
			{
				await ctx.RespondAsync(message);
			}
		}

		public static async Task TryRespondAsync(this CommandContext? ctx, DiscordEmbed message)
		{
			if (ctx is not null)
			{
				await ctx.RespondAsync(message);
			}
		}

		/// <summary>
		/// Gets the specified config value as the specified type, or returns the default value
		/// </summary>
		/// <typeparam name="T">Config parameter type</typeparam>
		/// <param name="configuration">config instance</param>
		/// <param name="key">Key to config value</param>
		/// <param name="defaultValue">Default value if the item is not set</param>
		/// <returns>Config value instance</returns>
		public static T Value<T>(this IConfiguration configuration, string key, T defaultValue)
		{
			var sect = configuration.GetSection(key);
			if (sect == null)
			{
				return defaultValue;
			}

			return sect.Get<T>() ?? defaultValue;
		}

		/// <summary>
		/// Shorthand extension for <seealso cref="ActivatorUtilities.CreateInstance{T}(IServiceProvider, object[])"/>
		/// </summary>
		/// <typeparam name="T">Type to activate</typeparam>
		/// <param name="provider">Service provider to get services from</param>
		/// <param name="parameters">Extra service parameters</param>
		/// <returns>Service instance</returns>
		public static T ActivateType<T>(this IServiceProvider provider, params object[] parameters) where T : class
		{
			return ActivatorUtilities.CreateInstance<T>(provider, parameters: parameters);
		}

		/// <summary>
		/// Shorthand extension for <seealso cref="string.Contains(string, StringComparison)"/>, with <seealso cref="StringComparison.InvariantCultureIgnoreCase"/>
		/// </summary>
		/// <param name="str"></param>
		/// <param name="value"></param>
		/// <returns></returns>
		public static bool ContainsInvariant(this string str, string value)
		{
			return str.Contains(value, StringComparison.InvariantCultureIgnoreCase);
		}

		/// <summary>
		/// Creates a basic regex expression from a pattern.
		/// <para>
		/// '*' matches 1+ instances of any character
		/// </para>
		/// <para>
		/// '+' Matches 1+ instances of Alphanumeric characters, excluding punctuation characters.
		/// </para>
		/// </summary>
		/// <param name="input"></param>
		/// <param name="anchor"></param>
		/// <returns></returns>
		public static string CreateRegexExpression(this string input, bool anchor)
		{
			var escaped = Regex.Escape(input
					.Replace('*', '§')
					.Replace('+', '¶'));

			var modified = escaped
				.Replace("§", ".+")
				.Replace("¶", "[a-zA-Z0-9]+");

			if (anchor)
			{
				return $"\\A{modified}\\z";
			}
			return modified;
		}

		/// <summary>
		/// Extracts a string resource from a URI's path
		/// Useful for extracting resource IDs from
		/// </summary>
		/// <param name="uri">The uri to extract the resource ID from</param>
		/// <param name="pattern">The pattern to extract, where ':id' is the target resource</param>
		/// <param name="targetParam">The target parameter, to extract, as present in the pattern</param>
		/// <returns>The value of <paramref name="targetParam"/> in the uri, or <see langword="null"/> if it couldn't be found</returns>
		/// <remarks>
		/// E.g., using the pattern '/resource/:id', for https://test.com/resource/1234/home, will return 1234
		/// </remarks>
		public static string? ExtractUriResource(this Uri uri, string pattern, string targetParam = ":id")
		{
			var parts = pattern.Split(targetParam, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

			var sb = new StringBuilder();

			if (parts.Length >= 1)
			{
				sb.Append($"(?<=\\A{parts[0].CreateRegexExpression(false)})");
			}

			sb.Append("[a-zA-Z0-9]+"); // ResourceID

			if (parts.Length >= 2)
			{
				sb.Append($"(?={parts[1].CreateRegexExpression(false)})\\z");
			}

			var regex = new Regex(sb.ToString(), RegexOptions.IgnoreCase);

			var match = regex.Match(uri.LocalPath);

			if (!match.Success)
			{
				return null;
			}

			return match.Value;
		}

		/// <summary>
		/// Extracts a string resource from a URI's path
		/// Useful for extracting resource IDs from
		/// </summary>
		/// <param name="uri">The uri to extract the resource ID from</param>
		/// <param name="pattern">The pattern to extract, where ':id' is the target resource</param>
		/// <param name="targetParam">The target parameter, to extract, as present in the pattern</param>
		/// <param name="result">The value of <paramref name="targetParam"/> in the uri, or an empty string if it couldn't be found</param>
		/// <returns>
		/// <see langword="true"/> if the specified resource could be extracted from the URI, <see langword="false"/> otherwise.
		/// </returns>
		/// <remarks>
		/// E.g., using the pattern '/resource/:id', for https://test.com/resource/1234/home, will return 1234
		/// </remarks>
		public static bool TryExtractUriResource(this Uri uri, string pattern, out string result, string targetParam = ":id")
		{
			var value = uri.ExtractUriResource(pattern, targetParam);
			if (value != null)
			{
				result = value;
				return true;
			}

			result = string.Empty;
			return false;
		}

		public static async Task<TemporalMessage> TryCreateTemporalAsync(this CommandContext? ctx, string message)
		{
			if (ctx is not null)
			{
				var msg = await ctx.RespondAsync(message);
				return new TemporalMessage(msg);
			}

			return new TemporalMessage();
		}

		public static async Task<TemporalMessage> TryCreateTemporalAsync(this CommandContext? ctx, DiscordEmbed message)
		{
			if (ctx is not null)
			{
				var msg = await ctx.RespondAsync(message);
				return new TemporalMessage(msg);
			}

			return new TemporalMessage();
		}

		public static async Task<TemporalMessage> TryCreateTemporalAsync(this Optional<CommandContext> ctx, string message)
		{
			if (ctx.HasValue)
			{
				var msg = await ctx.Value.RespondAsync(message);
				return new TemporalMessage(msg);
			}

			return new TemporalMessage();
		}

		public static async Task<TemporalMessage> TryCreateTemporalAsync(this Optional<CommandContext> ctx, DiscordEmbed message)
		{
			if (ctx.HasValue)
			{
				var msg = await ctx.Value.RespondAsync(message);
				return new TemporalMessage(msg);
			}

			return new TemporalMessage();
		}

        public static string FormatTime(this TimeSpan time)
        {
            var years = Math.Floor(time.TotalDays / 365f);

            if (years >= 1)
            {
                return $"{years} year{years.Pluralize()}";
            }
            else if (time.Days >= 1)
            {
                return $"{time.Days} day{time.Days.Pluralize()}";
            }
            else if (time.Hours >= 1)
            {
                return $"{time.Hours} hour{time.Hours.Pluralize()}";
            }
            else if (time.Minutes >= 1)
            {
                return $"{time.Minutes} minute{time.Minutes.Pluralize()}";
            }
            else
            {
                return $"{time.Seconds} second{time.Seconds.Pluralize()}";
            }
        }
    }
}
