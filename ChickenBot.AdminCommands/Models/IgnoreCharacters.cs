namespace ChickenBot.AdminCommands.Models
{
	public struct IgnoreCharacters
	{
		public void Reset()
		{

		}
		public bool ShouldIgnore(char c)
		{
			return false;
		}
	}
}
