using ChickenBot.API.Atrributes;
using ChickenBot.API.Interfaces;
using ChickenBot.AssignableRoles.Interfaces;
using DSharpPlus.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ChickenBot.AssignableRoles.Models
{
	[Transient(typeof(IAssignableRoles))]
	public class AssignableRolesProvider : IAssignableRoles
	{
		private readonly ILogger<AssignableRolesProvider> m_Logger;
		private readonly IConfiguration m_Configuration;
		private readonly IConfigEditor m_ConfigEditor;

		public AssignableRolesProvider(ILogger<AssignableRolesProvider> logger, IConfiguration configuration, IConfigEditor editor)
		{
			m_Logger = logger;
			m_Configuration = configuration;
			m_ConfigEditor = editor;
		}

		public AssignableRole[] GetAssignableRoles()
		{
			return m_Configuration.GetSection("AssignableRoles")?.Get<AssignableRole[]>()
				?? Array.Empty<AssignableRole>();
		}

		public async Task<bool> CreateNewAssignableRole(AssignableRole newRole)
		{
			return await m_ConfigEditor.AppendValueAsync("AssignableRoles", newRole);
		}

		public async Task<bool> RemoveAssignableRole(AssignableRole roleToRemove)
		{
			var roles = GetAssignableRoles();

			var newRoles = roles
				.Where(x => !x.RoleName.Equals(roleToRemove.RoleName, StringComparison.InvariantCultureIgnoreCase))
				.ToArray();

			return await m_ConfigEditor.UpdateValueAsync("AssignableRoles", newRoles);
		}
        
		public async Task<bool> AddUserRole(DiscordMember member, AssignableRole role)
		{
			var guildRole = member.Guild.GetRole(role.RoleID);

			if (guildRole == null)
			{
				return false;
			}

			await member.GrantRoleAsync(guildRole, "User-assignable role");

			m_Logger.LogInformation("User {user} self-assign themself the role {role}", member.Username, role.RoleName);
			return true;
		}

		public async Task<bool> RemoveUserRole(DiscordMember member, AssignableRole role)
		{
			var guildRole = member.Guild.GetRole(role.RoleID);

			if (guildRole == null)
			{
				return false;
			}

			m_Logger.LogInformation("User {user} removed the self-assigned role {role}", member.Username, role.RoleName);

			await member.RevokeRoleAsync(guildRole, "User-assignable role");
			return true;
		}
	}
}
