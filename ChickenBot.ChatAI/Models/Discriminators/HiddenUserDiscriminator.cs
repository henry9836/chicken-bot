using ChickenBot.ChatAI.Interfaces;
using DSharpPlus.Entities;

namespace ChickenBot.ChatAI.Models.Discriminators
{
	public class HiddenUserDiscriminator : IMessageDiscriminator
	{
		private readonly IReadOnlyCollection<ulong> m_HiddenUsers;

		public HiddenUserDiscriminator(IReadOnlyCollection<ulong> hiddenUsers)
		{
			m_HiddenUsers = hiddenUsers;
		}

		public bool Discriminate(DiscordUser user, string message)
		{
			return !m_HiddenUsers.Contains(user.Id);
		}
	}
}
