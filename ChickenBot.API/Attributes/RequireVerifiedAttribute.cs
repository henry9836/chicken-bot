using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ChickenBot.API.Attributes
{
	/// <summary>
	/// Requires that a user has the verified role to run a command
	/// </summary>
	public class RequireVerifiedAttribute : CheckBaseAttribute
	{
		public override Task<bool> ExecuteCheckAsync(CommandContext ctx, bool help)
		{
			if (ctx.Member is null)
			{
				return Task.FromResult(false);
			}

			var configuration = ctx.Services.GetRequiredService<IConfiguration>();

			var verifiedRoleID = configuration.GetRequiredSection("Roles").GetValue<ulong>("Verified", 0);

			var isVerified = ctx.Member.Roles.Any(x => x.Id == verifiedRoleID);

			if (!isVerified && !help)
			{
				ctx.RespondAsync("You have to be verified to use this command");
			}
			
			return Task.FromResult(isVerified);
		}
	}
}
