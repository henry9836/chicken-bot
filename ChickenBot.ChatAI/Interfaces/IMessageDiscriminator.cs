using DSharpPlus.Entities;

namespace ChickenBot.ChatAI.Interfaces
{
	public interface IMessageDiscriminator
	{
		Task<bool> Discriminate(DiscordUser user, string message);
	}
}
