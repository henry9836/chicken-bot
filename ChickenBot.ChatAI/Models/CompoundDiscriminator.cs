using ChickenBot.ChatAI.Interfaces;
using DSharpPlus.Entities;

namespace ChickenBot.ChatAI.Models
{
	public class CompoundDiscriminator : IMessageDiscriminator
	{
		public IMessageDiscriminator[] Discriminators { get; }

		public CompoundDiscriminator(params IMessageDiscriminator[] discriminators)
		{
			Discriminators = discriminators;
		}

		public async Task<bool> Discriminate(DiscordUser user, string message)
		{
			var tasks = new Task<bool>[Discriminators.Length];

			for (int i = 0; i < Discriminators.Length; i++)
			{
				tasks[i] = Task.Run(() => Discriminators[i].Discriminate(user, message));
			}

			for (int i = 0; i <= tasks.Length; i++)
			{
				if (!await tasks[i])
				{
					return false;
				}
			}

			return true;
		}
	}
}
