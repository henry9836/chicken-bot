using System.Reflection;
using ChickenBot.API.Models;
using ChickenBot.Core.Attributes;
using ChickenBot.Core.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ChickenBot.Core.Services
{
    /// <summary>
    /// Loads assemblies from the plugins folder into the GAC
    /// </summary>
    [RootService]
    public class AssemblyLoaderService : IHostedService
    {
        public const string PluginsDirectory = "plugins";

        private readonly PluginRegistry m_Registry;

        private readonly ILogger<AssemblyLoaderService> m_Logger;

        public AssemblyLoaderService(PluginRegistry registry, ILogger<AssemblyLoaderService> logger)
        {
            m_Registry = registry;
            m_Logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            AppDomain.CurrentDomain.AssemblyResolve += ResolveAssembly;

            if (!Directory.Exists(PluginsDirectory))
            {
                Directory.CreateDirectory(PluginsDirectory);
            }

            var configFile = Environment.GetEnvironmentVariable("chicken_plugin_config");

            if (configFile == null)
            {
                await LoadPluginsDirectory();
                return;
            }

            if (!File.Exists(configFile))
            {
                m_Logger.LogError("Plugins config file is set, but doesn't exist! Config file: {file}", configFile);
                await LoadPluginsDirectory();
                return;
            }

            var config = INIParser.Parse(await File.ReadAllLinesAsync(configFile));
            var root = config[string.Empty];

            if (root.Bool("load_plugins_dir", defaultValue: true))
            {
                await LoadPluginsDirectory(false);
            }

            var selectionRoot = Environment.CurrentDirectory;

            if (root.TryGetValue("selection_root", out var selRoot))
            {
                selectionRoot = Path.GetFullPath(selRoot);
            }

            foreach (var (pluginName, pluginConfig) in config)
            {
                if (string.IsNullOrWhiteSpace(pluginName))
                {
                    continue;
                }

                await LoadPluginsConfig(pluginConfig, pluginName, selectionRoot);
            }

            m_Logger.LogInformation("Loaded {plugins} plugins and {libraries} libraries", m_Registry.Plugins.Count, m_Registry.Libraries.Count);
        }

        /// <summary>
        /// Implements the default plugin loading from the 'plugins' directory
        /// </summary>
        private async Task LoadPluginsDirectory(bool printCategoryMessages = true)
        {
            if (printCategoryMessages)
            {
                m_Logger.LogInformation("Loading libraries...");
            }

            foreach (var pluginDirectory in Directory.EnumerateDirectories(PluginsDirectory))
            {
                m_Logger.LogInformation("Loading libraries from {path}...", pluginDirectory);

                foreach (var dll in Directory.GetFiles(pluginDirectory, "*.dll"))
                {
                    await m_Registry.LoadLibrary(dll);
                }
            }

            if (printCategoryMessages)
            {
                m_Logger.LogInformation("Loading plugin assemblies...");
            }

            foreach (var dll in Directory.EnumerateFiles(PluginsDirectory, "*.dll"))
            {
                m_Logger.LogInformation("Loading plugin assembly {asm}", dll);

                await m_Registry.LoadPlugin(dll);
            }
        }

        /// <summary>
        /// Loads a plugin and it's dependencies, depending on it's configuration
        /// </summary>
        private async Task LoadPluginsConfig(IDictionary<string, string> config, string pluginName, string selectionRoot)
        {
            var enabled = config.Bool("enabled", false);
            if (!enabled)
            {
                m_Logger.LogDebug("Skipping plugin {plugin}, as it is disabled", pluginName);
                return;
            }

            if (!config.TryGetValue("directory", out var pluginDir))
            {
                m_Logger.LogWarning("Invalid plugin config {config}; missing 'directory' option", pluginName);
                return;
            }

            var loadDependencies = config.Bool("load_dependencies", false);
            var directory = Path.IsPathFullyQualified(pluginDir) ? pluginDir : Path.Combine(selectionRoot, pluginDir);

            m_Logger.LogInformation("Loading plugin: {plugin}...", pluginName);

            var pluginDll = Path.Combine(directory, pluginName + ".dll");

            if (!File.Exists(pluginDll))
            {
                m_Logger.LogWarning("Plugin file not found: {file}", pluginDll);
                return;
            }

            await m_Registry.LoadPlugin(pluginDll);

            if (!loadDependencies)
            {
                return;
            }

            var assemblies = Directory.GetFiles(directory, "*.dll");
            var filtered = FilterLoadAssemblies(assemblies, pluginName).ToArray();

            foreach (var assembly in filtered)
            {
                m_Logger.LogDebug("Loading dependency for plugin {plugin}: {assembly}", pluginName, assembly);
                await m_Registry.LoadLibrary(assembly);
            }
        }

        /// <summary>
        /// Returns assemblies which have not been loaded, and are not already available in default search locations
        /// </summary>
        private IEnumerable<string> FilterLoadAssemblies(IEnumerable<string> files, string pluginName)
        {
            foreach (var file in files)
            {
                var name = Path.GetFileNameWithoutExtension(file);

                if (name.Equals(pluginName, StringComparison.InvariantCultureIgnoreCase))
                {
                    continue;
                }

                try
                {
                    // This will either return an already loaded assembly,
                    // load it from the core ChickenBot project, or the GAC

                    // We want to prefer libraries to be loaded from the core project or GAC, over those included as build outputs
                    Assembly.Load(name);
                    continue;
                }
                catch (Exception)
                {
                    // Failed to load resolve assembly, so it should be loaded
                }

                yield return file;
            }
        }

        /// <summary>
        /// Provides an assembly loader, when the .NET runtime fails to load a library we previously loaded here
        /// </summary>
        /// <remarks>
        /// This allows us to redirect failed assembly load requests to our plugin assembly registry
        /// </remarks>
        private Assembly? ResolveAssembly(object? sender, ResolveEventArgs args)
        {
            var assemblyName = new AssemblyName(args.Name);

            if (assemblyName.Name != null)
            {
                return m_Registry.GetAssembly(assemblyName.Name);
            }

            return null;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;

            return Task.CompletedTask;
        }
    }
}
