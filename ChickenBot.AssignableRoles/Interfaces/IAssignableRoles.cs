using ChickenBot.AssignableRoles.Models;
using DSharpPlus.Entities;

namespace ChickenBot.AssignableRoles.Interfaces
{
	public interface IAssignableRoles
	{
		AssignableRole[] GetAssignableRoles();

		Task<bool> AddUserRole(DiscordMember member, AssignableRole role);

		Task<bool> RemoveUserRole(DiscordMember member, AssignableRole role);
		
		Task<bool> CreateNewAssignableRole(AssignableRole newRole);

		Task<bool> RemoveAssignableRole(AssignableRole roleToRemove);
	}
}
