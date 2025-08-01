﻿using System.Data;
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
            var roleText = string.Join(", ", roles.Select(x => $"`{x.RoleName}`"));

            return new DiscordEmbedBuilder()
                .WithTitle("Assignable Roles")
                .WithDescription($"Assign a role with `add-role [Role]`, and remove it with `remove-role`")
                .WithRequestedBy(user)
                .TryAddField("Roles", string.Join(", ", roles.Select(role => $"`{role.RoleName}`")))
                .Build();
        }

        [Command("add-role"), RequireBotSpam]
        public async Task AddRoleCommand(CommandContext ctx)
        {
            var roles = m_Roles.GetAssignableRoles();

            await ctx.RespondAsync(CreateRolesEmbed(roles, ctx.User));
        }

        [Command("role-list"), Description("Returns a list of assignable roles"), Aliases("list-role", "roles", "list-roles"), RequireBotSpam]
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

        [Command("add-role"), Description("Gives you a self-assignable role"), RequireBotSpam]
        [RequireBotPermissions(false, DiscordPermission.ManageRoles)]
        public async Task AddRoleCommand(CommandContext ctx, [RemainingText] string role)
        {
            if (ctx.User is not DiscordMember member)
            {
                await ctx.RespondAsync("This command cannot be used in DMs");
                return;
            }

            var roles = m_Roles.GetAssignableRoles();

            var matchingRoles = roles.Where(x => x.RoleName.Contains(role, StringComparison.InvariantCultureIgnoreCase)).ToArray();

            AssignableRole targetRole;

            if (matchingRoles.Length == 0)
            {
                var message = new DiscordMessageBuilder()
                .WithContent("No self-assignable role with that name")
                .AddEmbed(CreateRolesEmbed(roles, ctx.User));
                await ctx.RespondAsync(message);
                return;
            }
            else if (matchingRoles.Length > 1)
            {
                var exactRole = roles.Where(x => x.RoleName.Equals(role, StringComparison.InvariantCultureIgnoreCase)).FirstOrDefault();

                if (exactRole == null)
                {
                    var message = new DiscordMessageBuilder()
                        .WithContent($"Multiple matching role names: {string.Join(", ", matchingRoles.Select(x => x.RoleName))}")
                        .AddEmbed(CreateRolesEmbed(roles, ctx.User));
                    await ctx.RespondAsync(message);
                    return;
                }
                targetRole = exactRole;
            }
            else
            {
                targetRole = matchingRoles.First();
            }

            if (member.Roles.Any(x => x.Id == targetRole.RoleID))
            {
                await ctx.RespondAsync("You already have that role.");
                return;
            }

            if (!await m_Roles.AddUserRole(member, targetRole))
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
                await ctx.RespondAsync($"Granted you the role {targetRole.RoleName}");
            }
        }

        [Command("remove-role"), Description("Removes a self-assignable role"), RequireBotSpam]
        [RequireBotPermissions(false, DiscordPermission.ManageRoles)]
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
                    .AddEmbed(CreateRolesEmbed(roles, ctx.User));
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