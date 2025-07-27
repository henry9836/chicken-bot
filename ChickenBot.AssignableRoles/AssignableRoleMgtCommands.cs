using ChickenBot.API;
using ChickenBot.API.Attributes;
using ChickenBot.AssignableRoles.Interfaces;
using ChickenBot.AssignableRoles.Models;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace ChickenBot.AssignableRoles
{
	[Group("assignable-roles"), Category("Admin"), RequireBotManagerOrAdmin]
	public class AssignableRoleMgtCommands : BaseCommandModule
	{
		private readonly IAssignableRoles m_Roles;
		private readonly ILogger<AssignRoleCommands> m_Logger;

		public AssignableRoleMgtCommands(IAssignableRoles roles, ILogger<AssignRoleCommands> logger)
		{
			m_Roles = roles;
			m_Logger = logger;
		}

		[GroupCommand, Description("Manages the assignable roles module"), RequireBotManagerOrAdmin]
		public async Task AssignableRolesCommand(CommandContext ctx, [RemainingText] string? _)
		{

			var roles = m_Roles.GetAssignableRoles();
			var assignableRoles = (roles.Length == 0)
						? "No roles to display"
						: roles
							.Select(x => $"`{x.RoleName}` -> <@&{x.RoleID}>")
							.Aggregate((a, b) => $"{a}\n{b}");

			var embed = new DiscordEmbedBuilder()
				.WithTitle("Assignable Roles Module")
				.WithRequestedBy(ctx.User)
				.WithDescription($"### Commands\n" +
									$"`assignable-roles create [role name...]`\n" +
									$"> Searches for a role in the server, and creates an assignable role of the same name\n" +
									$"`assignable-roles add [role] [assignable name]`\n" +
									$"> Creates an assignable role with the specified, for the specified role\n" +
									$"`assignable-roles remove [assignable name]`\n" +
									$"> Removes an assignable role\n" +
									$"### Assignable Roles\n" +
									$"{assignableRoles}");

			await ctx.RespondAsync(embed);
		}

		[Command("create")]
		[RequireBotManagerOrAdmin]
		[Description("A fuzzy-search tool to create self-assignable roles with the same display name as their role name")]
		public async Task AddNewAssignableRole(CommandContext ctx, [RemainingText] string roleName)
		{
			if (ctx.User is not DiscordMember member)
			{
				await ctx.RespondAsync("This command cannot be used in DMs");
				return;
			}

			if (string.IsNullOrWhiteSpace(roleName))
			{
				await ctx.RespondAsync("Invalid role name!");
				return;
			}

			var targetRoles = member.Guild.Roles.Values.Where(x => x.Name.Contains(roleName, StringComparison.InvariantCultureIgnoreCase));

			if (targetRoles.Count() > 1)
			{
				await ctx.RespondAsync("Multiple roles by that name");
				return;
			}

			var targetRole = targetRoles.FirstOrDefault();

			if (targetRole is null)
			{
				await ctx.RespondAsync("Couldn't find a role with that name");
				return;
			}

			// Create the new role
			AssignableRole newRole = new AssignableRole
			{
				RoleName = targetRole.Name,
				RoleID = targetRole.Id
			};

			var roles = m_Roles.GetAssignableRoles();

			// If the role is already in our assignable roles do not add a repeat
			if (roles.Contains(newRole))
			{
				await ctx.RespondAsync("Role already assigned to assignable roles");
				return;
			}

			// Add the new role
			await m_Roles.CreateNewAssignableRole(newRole);

			m_Logger.LogInformation("User {username} created new self-assignable role {role} -> {roleName} ({roleID})", ctx.User.Username, newRole.RoleName, targetRole.Name, targetRole.Id);
			await ctx.RespondAsync($"Created new assignable role {newRole.RoleName}");
		}

		[Command("add")]
		[RequireBotManagerOrAdmin]
		public async Task AddNewAssignableRole(CommandContext ctx, DiscordRole role, [RemainingText] string? roleName)
		{
			if (ctx.User is not DiscordMember member)
			{
				await ctx.RespondAsync("This command cannot be used in DMs");
				return;
			}

			// Create the new role
			var guildRole = await member.Guild.GetRoleAsync(role.Id);

			AssignableRole newRole = new AssignableRole
			{
				RoleName = roleName ?? role.Name,
				RoleID = guildRole.Id
			};

			var roles = m_Roles.GetAssignableRoles();

			// If the role is already in our assignable roles do not add a repeat
			if (roles.Contains(newRole))
			{
				await ctx.RespondAsync("Role already assigned to assignable roles");
				return;
			}

			// Add the new role
			await m_Roles.CreateNewAssignableRole(newRole);

			m_Logger.LogInformation("User {username} created new self-assignable role {role} -> {roleName} ({roleID})", ctx.User.Username, newRole.RoleName, role.Name, role.Id);
			await ctx.RespondAsync($"Created new assignable role {newRole.RoleName}");
		}

		[Command("remove")]
		[RequireBotManagerOrAdmin]
		public async Task RemoveAssignableRole(CommandContext ctx, [RemainingText] string roleName)
		{
			if (ctx.User is not DiscordMember member)
			{
				await ctx.RespondAsync("This command cannot be used in DMs");
				return;
			}

			// Create the new role
			var roles = m_Roles.GetAssignableRoles();

			var roleToRemove = roles.FirstOrDefault(x => x.RoleName.Equals(roleName, StringComparison.InvariantCultureIgnoreCase));

			// If the role is already in our assignable roles do not add a repeat
			if (roleToRemove == null)
			{
				await ctx.RespondAsync("Role isn't part of assignable roles");
				return;
			}

			// Add the new role
			await m_Roles.RemoveAssignableRole(roleToRemove);
			m_Logger.LogInformation("User {username} deleted self-assignable role {role}", ctx.User.Username, roleName);
			await ctx.RespondAsync($"Removed the self-assignable role {roleName}");
		}
	}
}
