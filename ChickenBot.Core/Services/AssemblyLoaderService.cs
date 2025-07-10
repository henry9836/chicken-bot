using System.Reflection;
using ChickenBot.API.Models;
using ChickenBot.Core.Attributes;
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

            m_Logger.LogInformation("Loading libraries...");

            foreach (var pluginDirectory in Directory.EnumerateDirectories(PluginsDirectory))
            {
                m_Logger.LogInformation("Loading libraries from {path}...", pluginDirectory);

                foreach (var dll in Directory.GetFiles(pluginDirectory, "*.dll"))
                {
                    m_Registry.Libraries.Add(await LoadDLL(dll));
                }
            }

            m_Logger.LogInformation("Loading plugin assemblies...");

            foreach (var dll in Directory.EnumerateFiles(PluginsDirectory, "*.dll"))
            {
                m_Logger.LogInformation("Loading plugin assembly {asm}", dll);

                m_Registry.Plugins.Add(await LoadDLL(dll));
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

        private static async Task<Assembly> LoadDLL(string path)
        {
            // Load into memory then load from memory, so we don't lock the file
            var assemblyData = await File.ReadAllBytesAsync(path);
            return Assembly.Load(assemblyData);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            AppDomain.CurrentDomain.AssemblyResolve -= ResolveAssembly;

            return Task.CompletedTask;
        }
    }
}
