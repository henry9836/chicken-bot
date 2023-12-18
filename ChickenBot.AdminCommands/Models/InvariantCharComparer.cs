using System.Diagnostics.CodeAnalysis;

namespace ChickenBot.AdminCommands.Models
{
	public class InvariantCharComparer : IEqualityComparer<char>
	{
		public bool Equals(char x, char y)
		{
			return char.ToUpperInvariant(x) == char.ToUpperInvariant(y);
		}

		public int GetHashCode([DisallowNull] char obj)
		{
			return char.ToUpperInvariant(obj).GetHashCode();
		}
	}
}
