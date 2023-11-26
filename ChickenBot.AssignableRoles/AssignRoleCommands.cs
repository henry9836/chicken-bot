using System.Data;
using System.Text;
using ChickenBot.API;
using ChickenBot.API.Atrributes;
using ChickenBot.AssignableRoles.Interfaces;
using ChickenBot.AssignableRoles.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace ChickenBot.AssignableRoles
{
	public class AssignRoleCommands : BaseCommandModule
	{
		private readonly IAssignableRoles m_Roles;
		private readonly ILogger<AssignRoleCommands> m_Logger;

		public AssignRoleCommands(IAssignableRoles roles, ILogger<AssignRoleCommands> logger)
		{
			m_Roles = roles;
			m_Logger = logger;
		}

		private DiscordEmbed CreateRolesEmbed(AssignableRole[] roles, DiscordUser user)
		{
			var roletext = string.Join(", ", roles.Select(x => $"`{x.RoleName}`"));

			return new DiscordEmbedBuilder()
				.WithTitle("Assignable Roles")
				.WithDescription($"Assign a role with `add-role [Role]`, and remove it with `remove-role`")
				.WithRequestedBy(user)
				.AddField("Roles", string.Join(", ", roles.Select(role => $"`{role.RoleName}`")))
				.Build();
		}

		[Command("create-assignable-role")]
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

			if (targetRole == null)
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


		[Command("add-new-assignable-role")]
		[RequireBotManagerOrAdmin]
		public async Task AddNewAssignableRole(CommandContext ctx, DiscordRole role, [RemainingText] string? roleName)
		{
			if (ctx.User is not DiscordMember member)
			{
				await ctx.RespondAsync("This command cannot be used in DMs");
				return;
			}
			
			// Create the new role
			var guildRole = member.Guild.GetRole(role.Id);
			
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
		
		[Command("remove-assignable-role")]
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

		[Command("add-role")]
		public async Task AddRoleCommand(CommandContext ctx)
		{
			var roles = m_Roles.GetAssignableRoles();

			await ctx.RespondAsync(CreateRolesEmbed(roles, ctx.User));
		}

		[Command("role-list"), Description("Returns a list of assignable roles"), Aliases("list-role", "roles", "list-roles")]
		public async Task RoleListCommand(CommandContext ctx)
		{
			if (ctx.User is not DiscordMember member)
			{
				await ctx.RespondAsync("This command cannot be used in DMs");
				return;
			}
			
			var roles = m_Roles.GetAssignableRoles();

			var roleEmbed = new DiscordEmbedBuilder()
				.WithTitle($"Assignable Roles")
				.WithDescription("Self assignable roles, please only use this command in the bot channel")
				.AddField("Roles", string.Join(", ", roles.Select(role => $"`{role.RoleName}`")));

			await ctx.RespondAsync(roleEmbed);
		}
		
		[Command("add-role"), Description("Gives you a self-assignable role")]
		[RequireBotPermissions(Permissions.ManageRoles)]
		public async Task AddRoleCommand(CommandContext ctx, [RemainingText] string role)
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


			var tickEmoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:", includeGuilds: false);

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
		public async Task RemoveRoleCommand(CommandContext ctx, [RemainingText] string role)
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

			var tickEmoji = DiscordEmoji.FromName(ctx.Client, ":white_check_mark:", includeGuilds: false);

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