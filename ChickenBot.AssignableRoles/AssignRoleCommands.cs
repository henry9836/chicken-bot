using System.Text;
using ChickenBot.API;
using ChickenBot.AssignableRoles.Interfaces;
using ChickenBot.AssignableRoles.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;

namespace ChickenBot.AssignableRoles
{
	public class AssignRoleCommands : BaseCommandModule
	{
		private readonly IAssignableRoles m_Roles;

		public AssignRoleCommands(IAssignableRoles roles)
		{
			m_Roles = roles;
		}

		private DiscordEmbed CreateRolesEmbed(AssignableRole[] roles, DiscordUser user)
		{
			var sb = new StringBuilder();

			foreach (var role in roles)
			{
				sb.AppendLine($"  *  {role.RoleName}");
			}

			return new DiscordEmbedBuilder()
				.WithTitle("Assignable Roles")
				.WithDescription($"Assign a role with `add-role [Role]`, and remove it with `remove-role`\n{sb}")
				.WithRequestedBy(user)
				.Build();
		}

		[Command("add-role")]
		public async Task AddRoleCommand(CommandContext ctx)
		{
			var roles = m_Roles.GetAssignableRoles();

			await ctx.RespondAsync(CreateRolesEmbed(roles, ctx.User));
		}

		[Command("add-role"), Description("Gives you a self-assignable role")]
		[RequireBotPermissions(Permissions.ManageRoles)]
		public async Task AddRoleCommand(CommandContext ctx, string role)
		{
			if (ctx.User is not DiscordMember member)
			{
				await ctx.RespondAsync("This command cannot be used in DMs");
				return;
			}

			var roles = m_Roles.GetAssignableRoles();

			var requestedRole = roles.FirstOrDefault(x => x.RoleName.Equals(role, StringComparison.InvariantCultureIgnoreCase));

			if (requestedRole == null)
			{
				var message = new DiscordMessageBuilder()
					.WithContent("No self-assignable role with that name")
					.WithEmbed(CreateRolesEmbed(roles, ctx.User));
				await ctx.RespondAsync(message);
				return;
			}

			if (member.Roles.Any(x => x.Id == requestedRole.RoleID))
			{
				await ctx.RespondAsync("You already have that role.");
				return;
			}

			if (!await m_Roles.AddUserRole(member, requestedRole))
			{
				await ctx.RespondAsync("I can't seem to find that role right now :(");
				return;
			}

			var tickEmoji = DiscordEmoji.FromName(ctx.Client, "white_checkmark", includeGuilds: false);

			if (tickEmoji != null)
			{
				await ctx.Message.CreateReactionAsync(tickEmoji);
			}
			else
			{
				await ctx.RespondAsync($"Granted you the role {requestedRole.RoleName}");
			}
		}

		[Command("remove-role"), Description("Removes a self-assignable role")]
		[RequireBotPermissions(Permissions.ManageRoles)]
		public async Task RemoveRoleCommand(CommandContext ctx, string role)
		{
			if (ctx.User is not DiscordMember member)
			{
				await ctx.RespondAsync("This command cannot be used in DMs");
				return;
			}

			var roles = m_Roles.GetAssignableRoles();

			var requestedRole = roles.FirstOrDefault(x => x.RoleName.Equals(role, StringComparison.InvariantCultureIgnoreCase));

			if (requestedRole == null)
			{
				var message = new DiscordMessageBuilder()
					.WithContent("No self-assignable role with that name")
					.WithEmbed(CreateRolesEmbed(roles, ctx.User));
				await ctx.RespondAsync(message);
				return;
			}

			if (!member.Roles.Any(x => x.Id == requestedRole.RoleID))
			{
				await ctx.RespondAsync("You don't have that role.");
				return;
			}

			if (!await m_Roles.RemoveUserRole(member, requestedRole))
			{
				await ctx.RespondAsync("I can't seem to find that role right now :(");
				return;
			}

			var tickEmoji = DiscordEmoji.FromName(ctx.Client, "white_checkmark", includeGuilds: false);

			if (tickEmoji != null)
			{
				await ctx.Message.CreateReactionAsync(tickEmoji);
			}
			else
			{
				await ctx.RespondAsync($"Granted you the role {requestedRole.RoleName}");
			}
		}
	}
}