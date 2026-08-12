using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Abstractions.Model.Command;
using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Service;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace SoftwareWorker.BYO.CLI.Core.Engine
{
    /// <summary>
    /// Builds CliCommands structure from handler classes decorated with attributes
    /// </summary>
    public static class CommandsScanner
    {
        /// <summary>
        /// Scans assemblies and builds CliCommands from handlers decorated with TrunkCommandAttribute
        /// </summary>
        /// <returns>CliCommands object populated from reflection</returns>
        public static List<TrunkCommand> BuildFromReflection()
        {
            var commandsDict = new Dictionary<string, TrunkCommandData>();

            // Get all handler types from module assemblies
            var handlerTypes = FindAllHandlerTypes();

            foreach (var handlerType in handlerTypes)
            {
                var commandAttr = handlerType.GetCustomAttribute<TrunkCommandAttribute>()!;
                var subCommandAttr = handlerType.GetCustomAttribute<BranchCommandAttribute>();
                var actionAttr = handlerType.GetCustomAttribute<LeafCommandAttribute>();

                // Get or create command entry
                if (!commandsDict.TryGetValue(commandAttr.Name, out var trunkCommandData))
                {
                    trunkCommandData = new TrunkCommandData
                    {
                        Description = commandAttr.Description,
                        BranchCommands = new Dictionary<string, BranchCommandData>()
                    };
                    commandsDict[commandAttr.Name] = trunkCommandData;
                }

                // Check if this is a 1-level command (no branch attribute)
                if (subCommandAttr == null)
                {
                    // This is a 1-level command: trunk command is directly executable
                    trunkCommandData.Handler = handlerType.FullName;
                    trunkCommandData.Parameters = BuildParameters(handlerType);
                }
                else
                {
                    // Get or create subcommand entry
                    if (!trunkCommandData.BranchCommands.TryGetValue(subCommandAttr.Name, out var subCommandData))
                    {
                        subCommandData = new BranchCommandData
                        {
                            Description = subCommandAttr.Description,
                            LeafCommands = new List<LeafCommand>()
                        };
                        trunkCommandData.BranchCommands[subCommandAttr.Name] = subCommandData;
                    }

                    // If there's an action attribute, this is a three-level command
                    if (actionAttr != null)
                    {
                        var action = new LeafCommand
                        {
                            Name = actionAttr.Name,
                            Description = actionAttr.Description,
                            Handler = handlerType.FullName,
                            Parameters = BuildParameters(handlerType)
                        };
                        subCommandData.LeafCommands.Add(action);
                    }
                    else
                    {
                        // This is a two-level command (legacy behavior)
                        subCommandData.Handler = handlerType.FullName;
                        subCommandData.Parameters = BuildParameters(handlerType);
                    }
                }
            }

            // Convert to CliCommands
            var trunkCommands = commandsDict
                    .OrderBy(kvp => kvp.Key)
                    .Select(kvp => new TrunkCommand
                    {
                        Name = kvp.Key,
                        Description = kvp.Value.Description,
                        Handler = kvp.Value.Handler,
                        Parameters = kvp.Value.Parameters,
                        BranchCommands = kvp.Value.BranchCommands.Any()
                            ? kvp.Value.BranchCommands
                                .OrderBy(sc => sc.Key)
                                .Select(sc => new BranchCommand
                                {
                                    Name = sc.Key,
                                    Description = sc.Value.Description,
                                    Handler = sc.Value.Handler,
                                    Parameters = sc.Value.Parameters,
                                    LeafCommands = sc.Value.LeafCommands.Any()
                                        ? sc.Value.LeafCommands.OrderBy(a => a.Name).ToArray()
                                        : null
                                })
                                .ToArray()
                            : null
                    })
                    .ToList();

            return trunkCommands;
        }

        private class TrunkCommandData
        {
            public string Description { get; set; }
            public string Handler { get; set; }
            public Parameter[] Parameters { get; set; }
            public Dictionary<string, BranchCommandData> BranchCommands { get; set; }
        }

        private class BranchCommandData
        {
            public string Description { get; set; }
            public string Handler { get; set; }
            public Parameter[] Parameters { get; set; }
            public List<LeafCommand> LeafCommands { get; set; }
        }

        /// <summary>
        /// Finds all types that implement ICommandHandler in all application assemblies
        /// </summary>
        private static List<Type> FindAllHandlerTypes()
        {
            var handlerTypes = new List<Type>();
            var processedAssemblies = new HashSet<string>();
            var pluginLoadContexts = new Dictionary<string, PluginAssemblyLoadContext>(StringComparer.OrdinalIgnoreCase);

            // 1) Inspect assemblies already loaded into the current AppDomain first.
            //    This is essential for single-file publishes, where the managed
            //    assemblies (including the built-in handlers) are embedded in the host
            //    and do NOT exist as standalone *.dll files on disk, so the directory
            //    scan below cannot find them.
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.IsDynamic)
                {
                    continue;
                }

                var name = assembly.GetName().Name;
                if (name == null || !processedAssemblies.Add(name))
                {
                    continue;
                }

                handlerTypes.AddRange(GetHandlerTypesFromAssembly(assembly));
            }

            // 2) Scan DLL files from the app output and installed plugin folders to
            //    discover handlers in assemblies that are not yet loaded (e.g. plugins).
            var assemblyDirectories = new List<string> { AppContext.BaseDirectory };

            if (Directory.Exists(SystemConstants.PLUGINS_BINARIES_DIRECTORY))
            {
                assemblyDirectories.AddRange(Directory
                    .GetDirectories(SystemConstants.PLUGINS_BINARIES_DIRECTORY, "*", SearchOption.AllDirectories)
                    .Prepend(SystemConstants.PLUGINS_BINARIES_DIRECTORY));
            }

            var dllFiles = assemblyDirectories
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(Directory.Exists)
                .SelectMany(directory => Directory.GetFiles(directory, "*.dll", SearchOption.TopDirectoryOnly))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    if (IsPluginDependencyAssemblyToSkip(dllFile))
                    {
                        continue;
                    }

                    var assemblyName = AssemblyName.GetAssemblyName(dllFile);
                    if (assemblyName.Name != null && processedAssemblies.Add(assemblyName.Name))
                    {
                        var assembly = LoadAssemblyForScan(dllFile, pluginLoadContexts);
                        var types = GetHandlerTypesFromAssembly(assembly);
                        handlerTypes.AddRange(types);
                    }
                }
                catch (Exception ex)
                {
                    // Log but continue - some assemblies might not load
                    Console.WriteLine($"Warning: Could not load assembly {Path.GetFileName(dllFile)}: {ex.Message}");
                }
            }

            return handlerTypes;
        }

        private static Assembly LoadAssemblyForScan(
            string dllFile,
            IDictionary<string, PluginAssemblyLoadContext> pluginLoadContexts)
        {
            if (!dllFile.StartsWith(SystemConstants.PLUGINS_BINARIES_DIRECTORY, StringComparison.OrdinalIgnoreCase))
            {
                return Assembly.LoadFrom(dllFile);
            }

            var pluginDirectory = Path.GetDirectoryName(dllFile);
            if (string.IsNullOrWhiteSpace(pluginDirectory))
            {
                return Assembly.LoadFrom(dllFile);
            }

            if (!pluginLoadContexts.TryGetValue(pluginDirectory, out var loadContext))
            {
                loadContext = new PluginAssemblyLoadContext(dllFile);
                pluginLoadContexts[pluginDirectory] = loadContext;
            }

            return loadContext.LoadPluginAssembly(dllFile);
        }

        private const string PluginAssemblyPrefix = "BYO.Plugin.";

        private static bool IsPluginDependencyAssemblyToSkip(string dllFile)
        {
            if (!dllFile.StartsWith(SystemConstants.PLUGINS_BINARIES_DIRECTORY, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            var fileName = Path.GetFileNameWithoutExtension(dllFile);

            // Only scan the plugin's own entry assembly for handler types. Every other DLL sitting
            // next to it is a transitive dependency (e.g. Refit, Newtonsoft.Json, Azure/Graph SDKs).
            // Force-loading those directly - instead of letting the plugin's isolated
            // AssemblyLoadContext resolve them lazily on demand - can bind to a different copy/version
            // of a shared dependency than the one the plugin's own code was compiled against, which
            // manifests as an intermittent MissingMethodException/TypeLoadException even when the
            // plugin and CLI target the exact same SoftwareWorker.BYO.SDK version.
            if (!fileName.StartsWith(PluginAssemblyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private sealed class PluginAssemblyLoadContext : AssemblyLoadContext
        {
            private readonly AssemblyDependencyResolver _resolver;
            private readonly string _pluginDirectory;

            public PluginAssemblyLoadContext(string mainAssemblyPath)
                : base($"plugin:{Path.GetFileNameWithoutExtension(mainAssemblyPath)}", isCollectible: false)
            {
                _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
                _pluginDirectory = Path.GetDirectoryName(mainAssemblyPath) ?? string.Empty;
            }

            public Assembly LoadPluginAssembly(string assemblyPath)
            {
                return LoadFromAssemblyPath(assemblyPath);
            }

            protected override Assembly? Load(AssemblyName assemblyName)
            {
                var assemblySimpleName = assemblyName.Name;
                if (string.IsNullOrWhiteSpace(assemblySimpleName))
                {
                    return null;
                }

                if (assemblySimpleName.StartsWith("SoftwareWorker.BYO.", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var resolvedPath = _resolver.ResolveAssemblyToPath(assemblyName);
                if (resolvedPath == null)
                {
                    var candidatePath = Path.Combine(_pluginDirectory, $"{assemblySimpleName}.dll");
                    if (File.Exists(candidatePath))
                    {
                        resolvedPath = candidatePath;
                    }
                }

                if (resolvedPath == null)
                {
                    return null;
                }

                return LoadFromAssemblyPath(resolvedPath);
            }
        }

        /// <summary>
        /// Gets all types implementing BaseCommandHandler from a specific assembly
        /// </summary>
        private static List<Type> GetHandlerTypesFromAssembly(Assembly assembly)
        {
            var handlerTypes = new List<Type>();
            IEnumerable<string>? loaderMessages = null;
            var incompatibleHandlers = new List<string>();

            Type[] assemblyTypes;
            try
            {
                assemblyTypes = assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                // Some types failed to load (e.g. a transitive dependency could not be resolved).
                // Keep the types that did load instead of dropping every handler in this assembly.
                assemblyTypes = ex.Types.Where(t => t != null).ToArray()!;

                loaderMessages = ex.LoaderExceptions
                    .Where(e => e != null)
                    .Select(e => e!.Message)
                    .Distinct();
            }
            catch (Exception ex)
            {
                UserInterfaceService.ShowError($"Warning: Error getting types from assembly {assembly.GetName().Name}: {ex.Message}");
                return handlerTypes;
            }

            try
            {
                var types = assemblyTypes
                    .Where(t => t.IsClass
                             && !t.IsAbstract
                             && typeof(BaseCommandHandler).IsAssignableFrom(t)
                             && t.GetCustomAttribute<TrunkCommandAttribute>() != null)
                    .ToList();

                foreach (var type in types)
                {
                    if (IsHandlerCompatible(type, out var compatibilityError))
                    {
                        handlerTypes.Add(type);
                    }
                    else
                    {
                        incompatibleHandlers.Add($"{type.FullName}: {compatibilityError}");
                    }
                }

                // ReflectionTypeLoadException can occur for non-command types in plugin assemblies.
                // If command handlers were discovered successfully, suppress this noisy warning.
                if (loaderMessages != null && handlerTypes.Count == 0)
                {
                    UserInterfaceService.ShowWarning(
                        $"Warning: Some types in assembly {assembly.GetName().Name} could not be loaded: {string.Join("; ", loaderMessages)}");
                }

                if (incompatibleHandlers.Count > 0)
                {
                    UserInterfaceService.ShowWarning(
                        $"Warning: Ignored incompatible handler types in assembly {assembly.GetName().Name}: {string.Join("; ", incompatibleHandlers)}");
                }
            }
            catch (Exception ex)
            {
                UserInterfaceService.ShowError($"Warning: Error inspecting types from assembly {assembly.GetName().Name}: {ex.Message}");
            }

            return handlerTypes;
        }

        private static bool IsHandlerCompatible(Type handlerType, out string? error)
        {
            try
            {
                var ctor = handlerType.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                {
                    error = "Missing parameterless constructor.";
                    return false;
                }

                RuntimeHelpers.PrepareMethod(ctor.MethodHandle);

                var executeMethod = handlerType.GetMethod(nameof(BaseCommandHandler.ExecuteAsync), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (executeMethod != null && executeMethod.DeclaringType != typeof(BaseCommandHandler))
                {
                    RuntimeHelpers.PrepareMethod(executeMethod.MethodHandle);
                }

                error = null;
                return true;
            }
            catch (Exception ex) when (ex is MissingMethodException || ex is TypeLoadException)
            {
                error = ex.Message;
                return false;
            }
            catch (Exception ex)
            {
                error = ex.Message;
                return false;
            }
        }

        /// <summary>
        /// Builds parameter array from ParameterAttribute decorations
        /// </summary>
        private static Parameter[] BuildParameters(Type handlerType)
        {
            var parameterAttrs = handlerType.GetCustomAttributes<ParameterAttribute>();

            return parameterAttrs
                .Select(attr => new Parameter
                {
                    Name = attr.Name,
                    Description = attr.Description,
                    IsRequired = attr.IsRequired,
                    DefaultValue = attr.DefaultValue
                })
                .ToArray();
        }
    }
}
