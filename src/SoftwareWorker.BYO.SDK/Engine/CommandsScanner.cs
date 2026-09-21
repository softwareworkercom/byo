using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Abstractions.Model.Command;
using SoftwareWorker.BYO.CLI.Core.Constants;
using SoftwareWorker.BYO.CLI.Core.Service;
using System.Reflection;
using System.Runtime.InteropServices;
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
            var processedAssemblyPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
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

                var assemblyPath = assembly.Location;
                if (string.IsNullOrWhiteSpace(assemblyPath))
                {
                    // Assemblies without a physical path can still contribute handlers; keep them
                    // unique by simple name so we do not re-scan the same loaded assembly again.
                    var name = assembly.GetName().Name;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    if (!processedAssemblyPaths.Add(name))
                    {
                        continue;
                    }
                }
                else
                {
                    var normalizedPath = Path.GetFullPath(assemblyPath);
                    if (!processedAssemblyPaths.Add(normalizedPath))
                    {
                        continue;
                    }
                }

                handlerTypes.AddRange(GetHandlerTypesFromAssembly(assembly));
            }

            // 2) Scan installed plugin folders for assemblies that are not yet loaded.
            //    The current AppDomain already covers the host CLI/BYO assemblies, and recursively
            //    walking AppContext.BaseDirectory pulls in unrelated package/runtime assets
            //    (including native DLLs under runtimes/*/native) that are not command handlers.
            var assemblyDirectories = new List<string>();

            if (Directory.Exists(SystemConstants.PLUGINS_BINARIES_DIRECTORY))
            {
                assemblyDirectories.AddRange(Directory
                    .GetDirectories(SystemConstants.PLUGINS_BINARIES_DIRECTORY, "*", SearchOption.AllDirectories)
                    .Prepend(SystemConstants.PLUGINS_BINARIES_DIRECTORY));
            }

            var dllFiles = assemblyDirectories
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Where(Directory.Exists)
                .SelectMany(directory => Directory.EnumerateFiles(directory, "*.dll", SearchOption.AllDirectories))
                .Select(path => Path.GetFullPath(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    if (!processedAssemblyPaths.Add(dllFile))
                    {
                        continue;
                    }

                    if (IsPluginDependencyAssemblyToSkip(dllFile))
                    {
                        continue;
                    }

                    var assembly = LoadAssemblyForScan(dllFile, pluginLoadContexts);
                    var types = GetHandlerTypesFromAssembly(assembly);
                    handlerTypes.AddRange(types);
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
            // plugin and CLI target the exact same BYO.SDK version.
            if (!fileName.StartsWith(PluginAssemblyPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            return false;
        }

        private sealed class PluginAssemblyLoadContext : AssemblyLoadContext
        {
            private readonly AssemblyDependencyResolver? _resolver;
            private readonly string _pluginDirectory;

            internal PluginAssemblyLoadContext(string mainAssemblyPath)
                : base($"plugin:{Path.GetFileNameWithoutExtension(mainAssemblyPath)}", isCollectible: false)
            {
                _pluginDirectory = Path.GetDirectoryName(mainAssemblyPath) ?? string.Empty;

                if (!string.IsNullOrWhiteSpace(mainAssemblyPath) && File.Exists(mainAssemblyPath))
                {
                    _resolver = new AssemblyDependencyResolver(mainAssemblyPath);
                }
                else
                {
                    _resolver = null;
                }
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

                var alreadyLoaded = FindLoadedAssembly(assemblySimpleName);
                if (alreadyLoaded != null)
                {
                    return alreadyLoaded;
                }

                try
                {
                    var assemblyByIdentity = Assembly.Load(assemblyName);
                    if (assemblyByIdentity != null)
                    {
                        return assemblyByIdentity;
                    }
                }
                catch
                {
                    // Ignore missing identity and continue resolving from the host/plugin directories.
                }

                var hostAssembly = FindHostAssembly(assemblySimpleName);
                if (hostAssembly != null)
                {
                    return hostAssembly;
                }

                var resolvedPath = _resolver?.ResolveAssemblyToPath(assemblyName);
                if (resolvedPath == null)
                {
                    var candidatePath = Path.Combine(_pluginDirectory, $"{assemblySimpleName}.dll");
                    if (File.Exists(candidatePath))
                    {
                        resolvedPath = candidatePath;
                    }
                    else
                    {
                        resolvedPath = TryResolveFromPluginBinaryCache(assemblySimpleName)
                            ?? TryResolveFromPluginPackageCache(assemblySimpleName, ".dll");
                    }
                }

                if (resolvedPath != null)
                {
                    return LoadFromAssemblyPath(resolvedPath);
                }

                return null;
            }

            protected override nint LoadUnmanagedDll(string unmanagedDllName)
            {
                var resolvedPath = _resolver?.ResolveUnmanagedDllToPath(unmanagedDllName)
                    ?? TryResolveNativeFromPluginDirectory(unmanagedDllName)
                    ?? TryResolveFromPluginPackageCache(unmanagedDllName, GetNativeLibraryExtension());

                if (!string.IsNullOrWhiteSpace(resolvedPath))
                {
                    return LoadUnmanagedDllFromPath(resolvedPath);
                }

                return base.LoadUnmanagedDll(unmanagedDllName);
            }

            private static Assembly? FindLoadedAssembly(string assemblySimpleName)
            {
                foreach (var context in GetAssemblyContextsToSearch())
                {
                    foreach (var assembly in context.Assemblies)
                    {
                        if (string.Equals(assembly.GetName().Name, assemblySimpleName, StringComparison.OrdinalIgnoreCase))
                        {
                            return assembly;
                        }
                    }
                }

                foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
                {
                    if (string.Equals(assembly.GetName().Name, assemblySimpleName, StringComparison.OrdinalIgnoreCase))
                    {
                        return assembly;
                    }
                }

                return null;
            }

            private static Assembly? FindHostAssembly(string assemblySimpleName)
            {
                var candidates = new[]
                {
                    typeof(CommandsScanner).Assembly,
                    typeof(BaseCommandHandler).Assembly,
                    AssemblyLoadContext.Default.Assemblies.FirstOrDefault(a => string.Equals(a.GetName().Name, assemblySimpleName, StringComparison.OrdinalIgnoreCase))
                };

                foreach (var candidate in candidates)
                {
                    if (candidate != null && string.Equals(candidate.GetName().Name, assemblySimpleName, StringComparison.OrdinalIgnoreCase))
                    {
                        return candidate;
                    }
                }

                foreach (var assembly in AssemblyLoadContext.Default.Assemblies)
                {
                    if (string.Equals(assembly.GetName().Name, assemblySimpleName, StringComparison.OrdinalIgnoreCase))
                    {
                        return assembly;
                    }
                }

                var baseDirectory = AppContext.BaseDirectory;
                var directPath = Path.Combine(baseDirectory, $"{assemblySimpleName}.dll");
                if (File.Exists(directPath))
                {
                    try
                    {
                        return Assembly.LoadFrom(directPath);
                    }
                    catch
                    {
                        // Ignore if the file is already loaded or cannot be loaded from the host path.
                    }
                }

                var scannerLocation = typeof(CommandsScanner).Assembly.Location;
                if (!string.IsNullOrWhiteSpace(scannerLocation))
                {
                    var siblingPath = Path.Combine(Path.GetDirectoryName(scannerLocation) ?? string.Empty, $"{assemblySimpleName}.dll");
                    if (File.Exists(siblingPath))
                    {
                        try
                        {
                            return Assembly.LoadFrom(siblingPath);
                        }
                        catch
                        {
                            // Ignore duplicate-load conditions and keep falling back to the plugin cache.
                        }
                    }
                }

                return null;
            }

            private static IEnumerable<AssemblyLoadContext> GetAssemblyContextsToSearch()
            {
                if (AssemblyLoadContext.Default != null)
                {
                    yield return AssemblyLoadContext.Default;
                }

                foreach (var context in AssemblyLoadContext.All)
                {
                    if (!ReferenceEquals(context, AssemblyLoadContext.Default))
                    {
                        yield return context;
                    }
                }
            }

            private string? TryResolveFromPluginBinaryCache(string assemblySimpleName)
            {
                if (!Directory.Exists(SystemConstants.PLUGINS_BINARIES_DIRECTORY))
                {
                    return null;
                }

                var candidatePaths = Directory
                    .EnumerateFiles(SystemConstants.PLUGINS_BINARIES_DIRECTORY, $"{assemblySimpleName}.dll", SearchOption.AllDirectories)
                    .Where(path => !string.Equals(Path.GetDirectoryName(path), _pluginDirectory, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                return candidatePaths.FirstOrDefault();
            }

            private string? TryResolveNativeFromPluginDirectory(string libraryName)
            {
                var fileName = libraryName.EndsWith(GetNativeLibraryExtension(), StringComparison.OrdinalIgnoreCase)
                    ? libraryName
                    : libraryName + GetNativeLibraryExtension();

                var candidatePath = Path.Combine(_pluginDirectory, fileName);
                return File.Exists(candidatePath) ? candidatePath : null;
            }

            private string? TryResolveFromPluginPackageCache(string libraryName, string extension)
            {
                if (!Directory.Exists(SystemConstants.PLUGINS_PACKAGES_DIRECTORY))
                {
                    return null;
                }

                var fileName = libraryName.EndsWith(extension, StringComparison.OrdinalIgnoreCase)
                    ? libraryName
                    : libraryName + extension;

                var candidatePaths = Directory
                    .EnumerateFiles(SystemConstants.PLUGINS_PACKAGES_DIRECTORY, fileName, SearchOption.AllDirectories)
                    .Where(path => path.Contains($"{Path.DirectorySeparatorChar}extracted{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(GetPackageAssetPathScore)
                    .ThenBy(path => path, StringComparer.OrdinalIgnoreCase)
                    .ToList();

                return candidatePaths.FirstOrDefault();
            }

            private static int GetPackageAssetPathScore(string path)
            {
                var normalizedPath = path.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
                var score = 0;
                var nativeExtension = GetNativeLibraryExtension();
                var fileName = Path.GetFileName(path);

                if (normalizedPath.Contains($"{Path.DirectorySeparatorChar}runtimes{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    score += 100;
                }

                foreach (var runtimeIdentifier in GetPreferredRuntimeIdentifiers())
                {
                    if (normalizedPath.Contains($"{Path.DirectorySeparatorChar}{runtimeIdentifier}{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                    {
                        score += 50;
                        break;
                    }
                }

                if (normalizedPath.Contains($"{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}net10.0{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    score += 40;
                }
                else if (normalizedPath.Contains($"{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}net9.0{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    score += 35;
                }
                else if (normalizedPath.Contains($"{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}net8.0{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    score += 30;
                }
                else if (normalizedPath.Contains($"{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}netstandard2.1{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    score += 20;
                }
                else if (normalizedPath.Contains($"{Path.DirectorySeparatorChar}lib{Path.DirectorySeparatorChar}netstandard2.0{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase))
                {
                    score += 15;
                }

                if (string.Equals(Path.GetExtension(fileName), nativeExtension, StringComparison.OrdinalIgnoreCase))
                {
                    score += 10;
                }

                return score;
            }

            private static IEnumerable<string> GetPreferredRuntimeIdentifiers()
            {
                var architecture = RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();

                if (OperatingSystem.IsWindows())
                {
                    return [$"win-{architecture}", "win", "any"];
                }

                if (OperatingSystem.IsMacOS())
                {
                    return [$"osx-{architecture}", "osx", "unix", "any"];
                }

                if (OperatingSystem.IsLinux())
                {
                    return [$"linux-{architecture}", "linux", "unix", "any"];
                }

                return ["any"];
            }

            private static string GetNativeLibraryExtension()
            {
                if (OperatingSystem.IsWindows())
                {
                    return ".dll";
                }

                if (OperatingSystem.IsMacOS())
                {
                    return ".dylib";
                }

                return ".so";
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
