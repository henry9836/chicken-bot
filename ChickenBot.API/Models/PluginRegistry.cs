using System.Reflection;

namespace ChickenBot.API.Models
{
	public class PluginRegistry
	{
		public List<Assembly> Libraries { get; } = new List<Assembly>();
		public List<Assembly> Plugins { get; } = new List<Assembly>();

	}
}
