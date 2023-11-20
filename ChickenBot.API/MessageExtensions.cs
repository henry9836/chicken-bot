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
	}
}
