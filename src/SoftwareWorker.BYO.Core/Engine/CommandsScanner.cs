using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Abstractions.Model.Command;
using System.Reflection;

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
            string assemblyDirectory = AppContext.BaseDirectory;

            //Scan all DLL files in the output directory
            // This catches any assemblies that haven't been loaded yet
            var dllFiles = Directory.GetFiles(assemblyDirectory, "*.dll");

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(dllFile);
                    if (assemblyName.Name != null && !processedAssemblies.Contains(assemblyName.Name))
                    {
                        processedAssemblies.Add(assemblyName.Name);
                        Assembly assembly = Assembly.LoadFrom(dllFile);
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

        /// <summary>
        /// Gets all types implementing BaseCommandHandler from a specific assembly
        /// </summary>
        private static List<Type> GetHandlerTypesFromAssembly(Assembly assembly)
        {
            var handlerTypes = new List<Type>();

            try
            {
                var types = assembly.GetTypes()
                    .Where(t => t.IsClass
                             && !t.IsAbstract
                             && typeof(BaseCommandHandler).IsAssignableFrom(t)
                             && t.GetCustomAttribute<TrunkCommandAttribute>() != null);

                handlerTypes.AddRange(types);
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that have loading issues
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Warning: Error getting types from assembly {assembly.GetName().Name}: {ex.Message}");
            }

            return handlerTypes;
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
