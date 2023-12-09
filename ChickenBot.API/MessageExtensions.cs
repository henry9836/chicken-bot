using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;

namespace ChickenBot.API
{
	public static class MessageExtensions
	{
		public static DiscordEmbedBuilder WithRequestedBy(this DiscordEmbedBuilder builder, DiscordUser user)
		{
			builder.WithFooter($"Requested by {user.Username}");
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
	}
}
