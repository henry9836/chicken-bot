using DSharpPlus.Entities;

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
	}
}
