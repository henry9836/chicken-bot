using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;

namespace ChickenBot.API.Atrributes
{
	public class RequireBotManager : CheckBaseAttribute
	{
		internal static readonly ulong[] m_BotManagers = new ulong[]
		{
			102606498860896256, // Nitro
			764761783965319189  // Tek
		};

		public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
		{
			return Task.FromResult(m_BotManagers.Contains(ctx.User.Id));
		}
	}
}
