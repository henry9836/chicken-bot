using ChickenBot.API;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ChickenBot.Info
{
	public class InfoCommands : BaseCommandModule
	{
		[Command("avatar"), Description("Displays a user's profile picture")]
		public async Task AvatarCommand(CommandContext ctx)
		{
			var embed = new DiscordEmbedBuilder()
				.WithTitle("Avatar command")
				.WithDescription("Usage: `avatar [user]`")
				.WithRequestedBy(ctx.User);

			await ctx.RespondAsync(embed);
		}

		[Command("avatar"), Description("Displays a user's profile picture")]
		public async Task AvatarCommand(CommandContext ctx, DiscordMember member)
		{
			var embed = new DiscordEmbedBuilder()
				.WithTitle($"{member.DisplayName}'s Avatar")
				.WithImageUrl(member.AvatarUrl ?? member.DefaultAvatarUrl)
				.WithRequestedBy(ctx.User);

			await ctx.RespondAsync(embed);
		}

		[Command("info"), Description("Shows info about the Discord server")]
		public async Task ServerInfoCommand(CommandContext ctx)
		{
			if (ctx.Guild is null)
			{
				await ctx.RespondAsync("This command cannot be used in DMs");
				return;
			}

			var embed = new DiscordEmbedBuilder()
				.WithTitle($"{ctx.Guild.Name}")
				.AddField($"Members", $"{ctx.Guild.MemberCount}")
				.AddField($"Owner", $"<@{ctx.Guild.OwnerId}>")
				.AddField($"Created", $"<t:{ctx.Guild.JoinedAt.ToUnixTimeSeconds()}:f>");

			await ctx.RespondAsync(embed);
		}
	}
}