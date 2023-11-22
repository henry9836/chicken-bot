using System.Reflection;

namespace ChickenBot.API.Models
{
	public class PluginRegistry
	{
		public List<Assembly> Libraries { get; } = new List<Assembly>();
		public List<Assembly> Plugins { get; } = new List<Assembly>();

		public Assembly? GetAssembly(string name)
		{
			for (int i = 0; i < Plugins.Count; i++)
			{
				var plugin = Plugins[i];

				var pluginName = plugin.GetName();

				if (pluginName.Name != null && name.Equals(pluginName.Name, StringComparison.InvariantCultureIgnoreCase))
				{
					return plugin;
				}
			}

			for (int i = 0; i < Libraries.Count; i++)
			{
				var library = Libraries[i];

				var libraryName = library.GetName();

				if (libraryName.Name != null && name.Equals(libraryName.Name, StringComparison.InvariantCultureIgnoreCase))
				{
					return library;
				}
			}

			return null;
		}
	}
}
