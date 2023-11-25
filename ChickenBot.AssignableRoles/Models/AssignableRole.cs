namespace ChickenBot.AssignableRoles.Models
{
	public class AssignableRole
	{
		public ulong RoleID { get; set; }

		public string RoleName { get; set; } = string.Empty;

		//public override bool Equals(object? obj)
		//{
		//	return obj is not null
		//				&& obj is AssignableRole role
		//				&& (role.RoleID == role.RoleID || role.RoleName.Equals(RoleName, StringComparison.InvariantCultureIgnoreCase));
		//}

		//public override int GetHashCode()
		//{
		//	return HashCode.Combine(RoleID, RoleName.ToLowerInvariant());
		//}
	}
}
