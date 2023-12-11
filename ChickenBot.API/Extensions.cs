using DSharpPlus.CommandsNext;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChickenBot.API
{
	public static class Extensions
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

		public static async Task TryRespondAsync(this CommandContext? ctx, string message)
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
	}
}
