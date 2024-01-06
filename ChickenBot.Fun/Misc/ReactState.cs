using ChickenBot.API.Attributes;

namespace ChickenBot.Fun.Misc
{
	[Singleton]
	public class ReactState
	{
		public List<ulong> PregnantManReact { get; } = new List<ulong>();
	}
}
