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

		public Task<bool> Discriminate(DiscordUser user, string message)
		{
			return Task.FromResult(!m_HiddenUsers.Contains(user.Id));
		}
	}
}
