using DSharpPlus.Entities;

namespace ChickenBot.ChatAI.Interfaces
{
	public interface IMessageDiscriminator
	{
		bool Discriminate(DiscordUser user, string message);
	}
}
