using System.Data;
using ChickenBot.API;
using ChickenBot.AssignableRoles.Interfaces;
using ChickenBot.AssignableRoles.Models;
using DSharpPlus;
using DSharpPlus.CommandsNext;
using DSharpPlus.CommandsNext.Attributes;
using DSharpPlus.Entities;
using Microsoft.Extensions.Logging;

namespace ChickenBot.AssignableRoles
{
	[Category("Roles")]
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
				.TryAddField("Roles", string.Join(", ", roles.Select(role => $"`{role.RoleName}`")))
				.Build();
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
				.TryAddField("Roles", string.Join(", ", roles.Select(role => $"`{role.RoleName}`")) ?? string.Empty);

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

			if (tickEmoji is not null)
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

			if (tickEmoji is not null)
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