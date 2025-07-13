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

        public async Task LoadPlugin(string file)
        {
            Plugins.Add(await LoadDLL(file));
        }

        public async Task LoadLibrary(string file)
        {
            Libraries.Add(await LoadDLL(file));
        }

        private static async Task<Assembly> LoadDLL(string path)
        {
            // Load into memory then load from memory, so we don't lock the file
            var assemblyData = await File.ReadAllBytesAsync(path);
            return Assembly.Load(assemblyData);
        }
    }
}
