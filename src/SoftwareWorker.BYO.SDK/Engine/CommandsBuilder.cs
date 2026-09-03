using SoftwareWorker.BYO.CLI.Abstractions.Attributes;
using SoftwareWorker.BYO.CLI.Abstractions.Model.Command;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.Core.Model.Enums;
using System.CommandLine;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;


namespace SoftwareWorker.BYO.CLI.Core.Engine
{
    public class CommandsBuilder
    {
        private const string ScheduleOptionName = "schedule";
        private const string ExportOptionName = "export";
        private const string AsyncOptionName = "async";
        private static readonly Regex ScheduleIntervalRegex = new(
            @"^(?<value>\d+)\s*(?<unit>mo|[smhdw])$",
            RegexOptions.IgnoreCase | RegexOptions.Compiled);

        public static void LoadCommands(RootCommand rootCommand, List<TrunkCommand> trunkCommands)
        {
            foreach (var trunkCommand in trunkCommands)
            {
                var systemCommand = new Command(trunkCommand.Name, trunkCommand.Description);
                var hasTrunkHandler = !string.IsNullOrWhiteSpace(trunkCommand.Handler);
                var hasBranchCommands = trunkCommand.BranchCommands != null && trunkCommand.BranchCommands.Length > 0;

                // Trunk command can be directly executable even when it also has subcommands.
                if (hasTrunkHandler)
                {
                    var optionsMap = AddParametersToCommand(systemCommand, trunkCommand.Parameters);
                    var scheduleOption = AddScheduleOption(systemCommand);
                    var exportOption = AddExportOption(systemCommand);
                    var asyncOption = AddAsyncOption(systemCommand);
                    ConfigureDynamicParameterHandling(systemCommand, trunkCommand.Handler);
                    SetAction(systemCommand, trunkCommand.Handler, optionsMap, scheduleOption, exportOption, asyncOption);
                }

                if (hasBranchCommands)
                {
                    // Has subcommands - 2-level or 3-level
                    foreach (var subCmd in trunkCommand.BranchCommands.Cast<BranchCommand>())
                    {
                        // Check if this subcommand has actions (three-level) or is a direct handler (two-level)
                        if (subCmd.LeafCommands != null && subCmd.LeafCommands.Length > 0)
                        {
                            // Three-level command: add a subcommand with nested action commands
                            var subCommand = new Command(subCmd.Name, subCmd.Description);

                            foreach (var action in subCmd.LeafCommands.Cast<LeafCommand>())
                            {
                                var actionCommand = CreateActionCommand(action);
                                subCommand.Add(actionCommand);
                            }

                            systemCommand.Add(subCommand);
                        }
                        else
                        {
                            // Two-level command (legacy): add subcommand directly as executable
                            var subCommand = CreateSubCommand(subCmd);
                            systemCommand.Add(subCommand);
                        }
                    }
                }

                rootCommand.Add(systemCommand);
            }
        }

        private static Command CreateActionCommand(LeafCommand action)
        {
            var actionCommand = new Command(action.Name, action.Description);
            var optionsMap = AddParametersToCommand(actionCommand, action.Parameters);
            var scheduleOption = AddScheduleOption(actionCommand);
            var exportOption = AddExportOption(actionCommand);
            var asyncOption = AddAsyncOption(actionCommand);
            ConfigureDynamicParameterHandling(actionCommand, action.Handler);
            SetAction(actionCommand, action.Handler, optionsMap, scheduleOption, exportOption, asyncOption);
            return actionCommand;
        }

        private static Command CreateSubCommand(BranchCommand subCmd)
        {
            var subCommand = new Command(subCmd.Name, subCmd.Description);
            var optionsMap = AddParametersToCommand(subCommand, subCmd.Parameters);
            var scheduleOption = AddScheduleOption(subCommand);
            var exportOption = AddExportOption(subCommand);
            var asyncOption = AddAsyncOption(subCommand);
            ConfigureDynamicParameterHandling(subCommand, subCmd.Handler);
            SetAction(subCommand, subCmd.Handler, optionsMap, scheduleOption, exportOption, asyncOption);
            return subCommand;
        }

        private static void ConfigureDynamicParameterHandling(Command command, string handlerName)
        {
            var handlerType = FindTypeInReferencedAssemblies(handlerName);
            if (handlerType?.GetCustomAttribute<AllowDynamicParametersAttribute>() != null)
            {
                command.TreatUnmatchedTokensAsErrors = false;
            }
        }

        private static Dictionary<string, Option<string>> AddParametersToCommand(Command command, Parameter[] parameters)
        {
            var optionsMap = new Dictionary<string, Option<string>>();

            if (parameters == null) return optionsMap;

            foreach (var param in parameters)
            {
                // All parameters are optional at the command line level.
                // Missing parameters will be prompted for interactively by the handler.
                var option = new Option<string>($"--{param.Name}")
                {
                    Description = param.Description,
                    Required = false
                };
                optionsMap[param.Name] = option;
                command.Add(option);
            }

            return optionsMap;
        }

        private static Option<string> AddScheduleOption(Command command)
        {
            var scheduleOption = new Option<string>($"--{ScheduleOptionName}")
            {
                Description = "Repeat command on a schedule. Supports hh:mm:ss or short format like 30s, 5m, 1h, 1d, 1w, 1mo.",
                Required = false
            };
            command.Add(scheduleOption);
            return scheduleOption;
        }

        private static Option<ExportEnum?> AddExportOption(Command command)
        {
            var exportOption = new Option<ExportEnum?>($"--{ExportOptionName}")
            {
                Description = "Optional export format. Supported values: csv, json, excel.",
                Required = false
            };
            command.Add(exportOption);
            return exportOption;
        }

        private static Option<bool> AddAsyncOption(Command command)
        {
            var asyncOption = new Option<bool>($"--{AsyncOptionName}")
            {
                Description = "Run command asynchronously in the background and return immediately.",
                Required = false
            };

            command.Add(asyncOption);
            return asyncOption;
        }

        private static void SetAction(
            Command command,
            string handlerName,
            Dictionary<string, Option<string>> optionsMap,
            Option<string> scheduleOption,
            Option<ExportEnum?> exportOption,
            Option<bool> asyncOption)
        {
            command.SetAction(async (ParseResult parseResult) =>
            {
                var handlerType = FindTypeInReferencedAssemblies(handlerName);
                if (handlerType == null || !typeof(BaseCommandHandler).IsAssignableFrom(handlerType))
                {
                    throw new InvalidOperationException($"Handler class '{handlerName}' not found or does not implement BaseCommandHandler.");
                }

                var rawTokens = Environment.GetCommandLineArgs().Skip(1).ToList();
                var optionsDict = ExtractOptionValues(parseResult, optionsMap);
                var dynamicParameters = ExtractDynamicParameters(rawTokens, optionsMap);
                var scheduleValue = parseResult.CommandResult.GetValue(scheduleOption);
                var exportValue = parseResult.CommandResult.GetValue(exportOption);
                var runAsync = parseResult.CommandResult.GetValue(asyncOption);
                var contextValue = InferContextValue(rawTokens);

                if (runAsync)
                {
                    var backgroundTokens = RemoveOptionFromTokens(rawTokens, [AsyncOptionName, "async"]);
                    if (!TryStartBackgroundProcess(backgroundTokens, out var backgroundProcessId, out var launchError))
                    {
                        UserInterfaceService.ShowError(launchError ?? "Unable to start the command in background mode.");
                        return;
                    }

                    UserInterfaceService.ShowGrey($"Started in background (PID {backgroundProcessId}).");
                    return;
                }

                if (exportValue.HasValue)
                {
                    optionsDict[ExportOptionName] = exportValue.Value.ToString();
                }

                // Ensure all parameters are populated (prompts interactively by default)
                var (updatedOptions, validationError) = BaseCommandHandler.EnsureParameters(handlerType, optionsDict);
                if (validationError != null)
                {
                    UserInterfaceService.ShowError(validationError);
                    return;
                }
                optionsDict = updatedOptions;

                var executeOnce = async () =>
                {
                    var handlerInstance = (BaseCommandHandler)Activator.CreateInstance(handlerType)!;
                    handlerInstance.BindParameters(optionsDict);
                    handlerInstance.SetDynamicParameters(dynamicParameters);
                    handlerInstance.SetExport(exportValue);
                    handlerInstance.SetContext(contextValue);

                    var isSuccessful = true;
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    try
                    {
                        await handlerInstance.ExecuteAsync();
                    }
                    catch (Exception ex) when (ex is MissingMethodException || ex is TypeLoadException || ex is MissingFieldException)
                    {
                        isSuccessful = false;
                        UserInterfaceService.ShowError(
                            $"Error: {ex.GetType().Name}: {ex.Message}\n" +
                            "This usually means an installed plugin was built against a different version of SoftwareWorker.BYO.SDK than the one currently installed. " +
                            "Try updating or reinstalling the plugin (byo plugin install <plugin>) to a version compatible with the current CLI.");
                    }
                    catch (Exception ex)
                    {
                        isSuccessful = false;
                        UserInterfaceService.ShowError(FormatExceptionMessage(ex));
                    }

                    stopwatch.Stop();

                    UserInterfaceService.WriteLine();
                    if (isSuccessful)
                    {
                        UserInterfaceService.ShowGreen($"Completed in {stopwatch.ElapsedMilliseconds} ms.");
                    }
                    else
                    {
                        UserInterfaceService.ShowError($"Failed after {stopwatch.ElapsedMilliseconds} ms.");
                    }
                };

                if (string.IsNullOrWhiteSpace(scheduleValue))
                {
                    await executeOnce();
                    return;
                }

                if (!TryParseScheduleInterval(scheduleValue, out var scheduleInterval))
                {
                    UserInterfaceService.ShowError("Invalid --schedule value. Use hh:mm:ss or short format like 30s, 5m, 1h, 1d, 1w, 1mo.");
                    return;
                }

                using var cancellation = new CancellationTokenSource();
                ConsoleCancelEventHandler cancelHandler = (_, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    cancellation.Cancel();
                };

                Console.CancelKeyPress += cancelHandler;
                try
                {
                    UserInterfaceService.ShowGrey($"Running on schedule every {scheduleInterval:c}. Press Ctrl+C to stop.");
                    await executeOnce();

                    using var timer = new PeriodicTimer(scheduleInterval);
                    while (await timer.WaitForNextTickAsync(cancellation.Token))
                    {
                        await executeOnce();
                    }
                }
                catch (OperationCanceledException)
                {
                    UserInterfaceService.ShowGrey("Schedule stopped.");
                }
                finally
                {
                    Console.CancelKeyPress -= cancelHandler;
                }
            });
        }

        private static string? InferContextValue(IReadOnlyList<string> rawTokens)
        {
            var commandSegments = rawTokens
                .TakeWhile(token => !token.StartsWith("--", StringComparison.Ordinal))
                .Select(NormalizeContextSegment)
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .ToList();

            if (commandSegments.Count == 0)
            {
                return null;
            }

            var toolName = NormalizeContextSegment(GetToolCommandName());
            commandSegments.Insert(0, string.IsNullOrWhiteSpace(toolName) ? "byo" : toolName);

            return string.Join("-", commandSegments);
        }

        private static string NormalizeContextSegment(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalized = Regex.Replace(value.Trim().ToLowerInvariant(), "[^a-z0-9-]+", "-");
            return Regex.Replace(normalized, "-{2,}", "-").Trim('-');
        }

        private static bool TryParseScheduleInterval(string input, out TimeSpan interval)
        {
            interval = TimeSpan.Zero;
            if (string.IsNullOrWhiteSpace(input))
            {
                return false;
            }

            if (TimeSpan.TryParse(input, out interval) && interval > TimeSpan.Zero)
            {
                return true;
            }

            var match = ScheduleIntervalRegex.Match(input.Trim());
            if (!match.Success || !int.TryParse(match.Groups["value"].Value, out var value) || value <= 0)
            {
                return false;
            }

            interval = match.Groups["unit"].Value.ToLowerInvariant() switch
            {
                "s" => TimeSpan.FromSeconds(value),
                "m" => TimeSpan.FromMinutes(value),
                "h" => TimeSpan.FromHours(value),
                "d" => TimeSpan.FromDays(value),
                "w" => TimeSpan.FromDays(value * 7),
                "mo" => TimeSpan.FromDays(value * 30),
                _ => TimeSpan.Zero
            };

            return interval > TimeSpan.Zero;
        }

        private static Dictionary<string, object> ExtractOptionValues(
            ParseResult parseResult,
            Dictionary<string, Option<string>> optionsMap)
        {
            var optionsDict = new Dictionary<string, object>();

            foreach (var (key, option) in optionsMap)
            {
                string? optionValue = parseResult.CommandResult.GetValue(option);
                if (!string.IsNullOrEmpty(optionValue))
                {
                    optionsDict[key] = optionValue;
                }
            }

            return optionsDict;
        }

        private static Dictionary<string, string> ExtractDynamicParameters(
            IEnumerable<string> rawTokens,
            Dictionary<string, Option<string>> optionsMap)
        {
            var dynamicParameters = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var knownOptions = optionsMap.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
            knownOptions.Add(ScheduleOptionName);
            knownOptions.Add(ExportOptionName);
            knownOptions.Add(AsyncOptionName);
            var tokens = rawTokens.ToList();

            for (var i = 0; i < tokens.Count; i++)
            {
                var token = tokens[i];
                if (!token.StartsWith("--", StringComparison.Ordinal))
                {
                    continue;
                }

                var optionText = token[2..].Trim();
                if (string.IsNullOrWhiteSpace(optionText))
                {
                    continue;
                }

                string key;
                string value;

                var eqIndex = optionText.IndexOf('=');
                if (eqIndex >= 0)
                {
                    key = optionText[..eqIndex].Trim();
                    value = optionText[(eqIndex + 1)..].Trim();
                }
                else
                {
                    key = optionText;
                    value = string.Empty;

                    if (i + 2 < tokens.Count &&
                        !tokens[i + 1].StartsWith("--", StringComparison.Ordinal) &&
                        !tokens[i + 2].StartsWith("--", StringComparison.Ordinal) &&
                        LooksLikeTokenSegment(tokens[i + 1]))
                    {
                        key = $"{optionText}:{tokens[i + 1].Trim()}";
                        value = tokens[i + 2];
                        i += 2;
                    }
                    else if (i + 1 < tokens.Count && !tokens[i + 1].StartsWith("--", StringComparison.Ordinal))
                    {
                        value = tokens[i + 1];
                        i++;
                    }
                }

                // skip declared options
                if (knownOptions.Contains(key))
                {
                    continue;
                }

                // skip declared options passed as --name:value
                var colonIndex = key.IndexOf(':');
                if (colonIndex > 0 && knownOptions.Contains(key[..colonIndex]))
                {
                    continue;
                }

                dynamicParameters[key] = value;
            }

            return dynamicParameters;
        }

        private static bool LooksLikeTokenSegment(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            var trimmed = value.Trim();
            return !trimmed.Contains(' ') && !trimmed.Contains('=');
        }

        static BaseCommandHandler GetHandlerInstance(string handlerName)
        {
            var (instance, _) = GetHandlerInstanceAndType(handlerName);
            return instance;
        }

        static (BaseCommandHandler Instance, Type HandlerType) GetHandlerInstanceAndType(string handlerName)
        {
            Type handlerType = FindTypeInReferencedAssemblies(handlerName);
            if (handlerType == null || !typeof(BaseCommandHandler).IsAssignableFrom(handlerType))
            {
                throw new InvalidOperationException($"Handler class '{handlerName}' not found or does not implement BaseCommandHandler.");
            }

            var instance = (BaseCommandHandler)Activator.CreateInstance(handlerType);
            return (instance, handlerType);
        }

        static Type FindTypeInReferencedAssemblies(string className)
        {
            var processedAssemblies = new HashSet<string>();

            // Search in loaded assemblies first
            var type = SearchLoadedAssemblies(className, processedAssemblies);
            if (type != null) return type;

            // Then search in DLL files
            return SearchAssemblyFiles(className, processedAssemblies);
        }

        private static Type SearchLoadedAssemblies(string className, HashSet<string> processedAssemblies)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (!ShouldProcessAssembly(assembly, processedAssemblies))
                    continue;

                var type = TryGetTypeFromAssembly(assembly, className);
                if (type != null) return type;
            }
            return null;
        }

        private static Type SearchAssemblyFiles(string className, HashSet<string> processedAssemblies)
        {
            var assemblyDirectory = AppContext.BaseDirectory;
            var dllFiles = Directory.GetFiles(assemblyDirectory, "*.dll");

            foreach (var dllFile in dllFiles)
            {
                try
                {
                    var assemblyName = AssemblyName.GetAssemblyName(dllFile);
                    if (assemblyName.Name == null || processedAssemblies.Contains(assemblyName.Name))
                        continue;

                    processedAssemblies.Add(assemblyName.Name);
                    var assembly = Assembly.LoadFrom(dllFile);
                    var type = TryGetTypeFromAssembly(assembly, className);
                    if (type != null) return type;
                }
                catch
                {
                    // Skip assemblies with loading issues
                }
            }
            return null;
        }

        private static bool ShouldProcessAssembly(Assembly assembly, HashSet<string> processedAssemblies)
        {
            try
            {
                var assemblyName = assembly.GetName().Name;
                if (assemblyName == null || processedAssemblies.Contains(assemblyName))
                    return false;

                if (assemblyName.StartsWith("System") ||
                    assemblyName.StartsWith("Microsoft") ||
                    assemblyName.StartsWith("netstandard"))
                    return false;

                processedAssemblies.Add(assemblyName);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static Type TryGetTypeFromAssembly(Assembly assembly, string className)
        {
            try
            {
                return assembly.GetType(className, false, true);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Scans all referenced assemblies and returns all types that implement BaseCommandHandler
        /// </summary>
        /// <returns>List of types implementing BaseCommandHandler</returns>
        public static List<Type> GetAllCommandHandlers()
        {
            var handlerTypes = new List<Type>();

            // Check the Core assembly for built-in handlers
            var coreAssembly = Assembly.GetExecutingAssembly();
            handlerTypes.AddRange(GetHandlerTypesFromAssembly(coreAssembly));
            return handlerTypes;
        }

        /// <summary>
        /// Gets all types implementing BaseCommandHandler from a specific assembly
        /// </summary>
        /// <param name="assembly">The assembly to scan</param>
        /// <returns>List of types implementing BaseCommandHandler</returns>
        private static List<Type> GetHandlerTypesFromAssembly(Assembly assembly)
        {
            var handlerTypes = new List<Type>();

            try
            {
                var types = assembly.GetTypes()
                    .Where(t => typeof(BaseCommandHandler).IsAssignableFrom(t)
                             && !t.IsInterface
                             && !t.IsAbstract)
                    .ToList();

                handlerTypes.AddRange(types);
            }
            catch (ReflectionTypeLoadException)
            {
                // Skip assemblies that have loading issues
            }

            return handlerTypes;
        }

        public static string GetCurrentVersion()
        {
            return Assembly
                .GetExecutingAssembly()
                .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                ?.InformationalVersion;
        }

        public static string GetToolCommandName()
        {
            var assembly = Assembly.GetEntryAssembly();
            var metadata = assembly?.GetCustomAttributes<AssemblyMetadataAttribute>()
                .FirstOrDefault(a => a.Key == "ToolCommandName");
            return metadata?.Value;
        }

        private static string BuildReplayCommand(IEnumerable<string> rawTokens, IReadOnlyDictionary<string, object> options)
        {
            var knownOptions = new HashSet<string>(options.Keys, StringComparer.OrdinalIgnoreCase)
            {
                ScheduleOptionName,
                ExportOptionName,
                AsyncOptionName
            };

            var commandTokens = new List<string>();
            var tokenList = rawTokens.ToList();

            for (var index = 0; index < tokenList.Count; index++)
            {
                var token = tokenList[index];
                if (!token.StartsWith("--", StringComparison.Ordinal))
                {
                    commandTokens.Add(token);
                    continue;
                }

                var optionToken = token[2..];
                var eqIndex = optionToken.IndexOf('=');
                var optionName = (eqIndex >= 0 ? optionToken[..eqIndex] : optionToken).Trim();

                if (!knownOptions.Contains(optionName))
                {
                    continue;
                }

                if (eqIndex < 0 &&
                    index + 1 < tokenList.Count &&
                    !tokenList[index + 1].StartsWith("--", StringComparison.Ordinal))
                {
                    index++;
                }
            }

            if (commandTokens.Count == 0)
            {
                return string.Empty;
            }

            var toolName = GetToolCommandName();
            var replayTokens = new List<string> { string.IsNullOrWhiteSpace(toolName) ? "byo" : toolName };
            replayTokens.AddRange(commandTokens);

            foreach (var (key, rawValue) in options)
            {
                var value = rawValue?.ToString();
                if (string.IsNullOrWhiteSpace(value))
                {
                    continue;
                }

                replayTokens.Add($"--{key}");
                replayTokens.Add(FormatCommandArgument(value));
            }

            return string.Join(" ", replayTokens);
        }

        private static List<string> RemoveOptionFromTokens(
            IReadOnlyList<string> tokens,
            IReadOnlyCollection<string> optionNames)
        {
            var normalizedOptions = new HashSet<string>(optionNames, StringComparer.OrdinalIgnoreCase);
            var filteredTokens = new List<string>();

            for (var index = 0; index < tokens.Count; index++)
            {
                var token = tokens[index];
                if (!token.StartsWith("--", StringComparison.Ordinal))
                {
                    filteredTokens.Add(token);
                    continue;
                }

                var optionToken = token[2..];
                var eqIndex = optionToken.IndexOf('=');
                var optionName = (eqIndex >= 0 ? optionToken[..eqIndex] : optionToken).Trim();

                if (!normalizedOptions.Contains(optionName))
                {
                    filteredTokens.Add(token);
                    continue;
                }

                if (eqIndex < 0 &&
                    index + 1 < tokens.Count &&
                    !tokens[index + 1].StartsWith("--", StringComparison.Ordinal) &&
                    bool.TryParse(tokens[index + 1], out _))
                {
                    index++;
                }
            }

            return filteredTokens;
        }

        private static bool TryStartBackgroundProcess(
            IReadOnlyList<string> commandTokens,
            out int processId,
            out string? error)
        {
            processId = 0;
            error = null;

            if (commandTokens.Count == 0)
            {
                error = "Unable to start background command: no command tokens were provided.";
                return false;
            }

            var commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length == 0)
            {
                error = "Unable to determine command entry point for background execution.";
                return false;
            }

            var entryPoint = commandLineArgs[0];
            var processPath = Environment.ProcessPath;
            var executable = string.IsNullOrWhiteSpace(processPath) ? entryPoint : processPath;

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = Environment.CurrentDirectory
            };

            if (entryPoint.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.ArgumentList.Add(entryPoint);
            }

            foreach (var token in commandTokens)
            {
                startInfo.ArgumentList.Add(token);
            }

            try
            {
                var process = Process.Start(startInfo);
                if (process == null)
                {
                    error = "Process start returned no process instance.";
                    return false;
                }

                processId = process.Id;
                return true;
            }
            catch (Exception ex)
            {
                error = $"Unable to start background process: {ex.Message}";
                return false;
            }
        }

        private static string FormatCommandArgument(string value)
        {
            var sanitized = value
                .Replace("\r\n", " ", StringComparison.Ordinal)
                .Replace("\r", " ", StringComparison.Ordinal)
                .Replace("\n", " ", StringComparison.Ordinal);

            return $"'{sanitized.Replace("'", "''", StringComparison.Ordinal)}'";
        }

        private static string FormatExceptionMessage(Exception ex)
        {
            var message = ex.Message?.Trim();
            var typeName = ex.GetType().Name;

            if (string.IsNullOrWhiteSpace(message))
            {
                return $"{typeName}: no additional error details were provided.";
            }

            if (message.StartsWith(typeName, StringComparison.Ordinal))
            {
                return message;
            }

            return $"{typeName}: {message}";
        }

        /// <summary>
        /// Generates all possible command combinations with and without optional parameters
        /// Enhanced to handle DefaultValue with pipe-separated options
        /// </summary>
        private static List<string> GenerateAllCommandCombinations(List<TrunkCommand> trunkCommands)
        {
            if (trunkCommands == null)
                return new List<string>();

            var combinations = new List<string>();

            foreach (var trunkCommand in trunkCommands)
            {
                // Check if this is a 1-level command (no subcommands)
                if (trunkCommand.BranchCommands == null || trunkCommand.BranchCommands.Length == 0)
                {
                    // 1-level command
                    combinations.AddRange(GenerateTrunkCommandCombinations(trunkCommand));
                }
                else
                {
                    // Has subcommands - 2-level or 3-level
                    foreach (var subCommand in trunkCommand.BranchCommands.Cast<BranchCommand>())
                    {
                        // Check if this is a three-level command (has actions)
                        if (subCommand.LeafCommands != null && subCommand.LeafCommands.Length > 0)
                        {
                            foreach (var action in subCommand.LeafCommands.Cast<LeafCommand>())
                            {
                                combinations.AddRange(GenerateActionCombinations(trunkCommand.Name, subCommand.Name, action));
                            }
                        }
                        else
                        {
                            // Two-level command (legacy)
                            combinations.AddRange(GenerateCommandCombinations(trunkCommand.Name, subCommand));
                        }
                    }
                }
            }

            return combinations.OrderBy(c => c).ToList();
        }

        private static IEnumerable<string> GenerateActionCombinations(string commandName, string subCommandName, LeafCommand action)
        {
            var toolName = GetToolCommandName();
            var parameters = (action.Parameters as Parameter[]) ?? Array.Empty<Parameter>();

            if (!parameters.Any())
            {
                yield return $"{toolName} {commandName} {subCommandName} {action.Name}";
                yield break;
            }

            var (required, optional, pipeOptions) = CategorizeParameters(parameters);

            if (pipeOptions.Any())
            {
                foreach (var combination in GenerateCombinationsWithPipeOptions(toolName, commandName, subCommandName, action.Name, required, optional, pipeOptions))
                {
                    yield return combination;
                }
            }
            else
            {
                foreach (var combination in GenerateSimpleCombinations(toolName, commandName, subCommandName, action.Name, required, optional))
                {
                    yield return combination;
                }
            }
        }

        private static IEnumerable<string> GenerateCommandCombinations(string commandName, BranchCommand subCommand)
        {
            var toolName = GetToolCommandName();
            var parameters = (subCommand.Parameters as Parameter[]) ?? Array.Empty<Parameter>();

            if (!parameters.Any())
            {
                yield return $"{toolName} {commandName} {subCommand.Name}";
                yield break;
            }

            var (required, optional, pipeOptions) = CategorizeParameters(parameters);

            if (pipeOptions.Any())
            {
                foreach (var combination in GenerateCombinationsWithPipeOptions(toolName, commandName, subCommand.Name, required, optional, pipeOptions))
                {
                    yield return combination;
                }
            }
            else
            {
                foreach (var combination in GenerateSimpleCombinations(toolName, commandName, subCommand.Name, required, optional))
                {
                    yield return combination;
                }
            }
        }

        private static IEnumerable<string> GenerateTrunkCommandCombinations(TrunkCommand trunkCommand)
        {
            var toolName = GetToolCommandName();
            var parameters = (trunkCommand.Parameters as Parameter[]) ?? Array.Empty<Parameter>();

            if (!parameters.Any())
            {
                yield return $"{toolName} {trunkCommand.Name}";
                yield break;
            }

            var (required, optional, pipeOptions) = CategorizeParameters(parameters);

            if (pipeOptions.Any())
            {
                foreach (var combination in GenerateCombinationsWithPipeOptions(toolName, trunkCommand.Name, required, optional, pipeOptions))
                {
                    yield return combination;
                }
            }
            else
            {
                foreach (var combination in GenerateSimpleCombinations(toolName, trunkCommand.Name, required, optional))
                {
                    yield return combination;
                }
            }
        }

        private static (List<Parameter> required, List<Parameter> optional, List<Parameter> pipeOptions)
            CategorizeParameters(Parameter[] parameters)
        {
            var required = parameters.Where(p => p.IsRequired).ToList();
            var optional = parameters.Where(p => !p.IsRequired).ToList();
            var pipeOptions = parameters
                .Where(p => p.DefaultValue?.ToString()?.Contains("|") == true)
                .ToList();

            return (required, optional, pipeOptions);
        }

        private static IEnumerable<string> GenerateCombinationsWithPipeOptions(
            string toolName,
            string commandName,
            string subCommandName,
            List<Parameter> requiredParams,
            List<Parameter> optionalParams,
            List<Parameter> pipeParams)
        {
            var pipeOptionCombinations = GeneratePipeOptionCombinations(pipeParams);

            foreach (var pipeOptions in pipeOptionCombinations)
            {
                var relevantOptionals = optionalParams.Where(p => !pipeOptions.ContainsKey(p.Name));
                var optionalCombinations = GeneratePowerSet(relevantOptionals);

                foreach (var optionalCombination in optionalCombinations)
                {
                    var paramString = BuildParameterString(requiredParams, pipeOptions, optionalCombination);
                    yield return $"{toolName} {commandName} {subCommandName}{paramString}";
                }
            }
        }

        private static IEnumerable<string> GenerateCombinationsWithPipeOptions(
            string toolName,
            string commandName,
            List<Parameter> requiredParams,
            List<Parameter> optionalParams,
            List<Parameter> pipeParams)
        {
            var pipeOptionCombinations = GeneratePipeOptionCombinations(pipeParams);

            foreach (var pipeOptions in pipeOptionCombinations)
            {
                var relevantOptionals = optionalParams.Where(p => !pipeOptions.ContainsKey(p.Name));
                var optionalCombinations = GeneratePowerSet(relevantOptionals);

                foreach (var optionalCombination in optionalCombinations)
                {
                    var paramString = BuildParameterString(requiredParams, pipeOptions, optionalCombination);
                    yield return $"{toolName} {commandName}{paramString}";
                }
            }
        }

        private static IEnumerable<string> GenerateCombinationsWithPipeOptions(
            string toolName,
            string commandName,
            string subCommandName,
            string actionName,
            List<Parameter> requiredParams,
            List<Parameter> optionalParams,
            List<Parameter> pipeParams)
        {
            var pipeOptionCombinations = GeneratePipeOptionCombinations(pipeParams);

            foreach (var pipeOptions in pipeOptionCombinations)
            {
                var relevantOptionals = optionalParams.Where(p => !pipeOptions.ContainsKey(p.Name));
                var optionalCombinations = GeneratePowerSet(relevantOptionals);

                foreach (var optionalCombination in optionalCombinations)
                {
                    var paramString = BuildParameterString(requiredParams, pipeOptions, optionalCombination);
                    yield return $"{toolName} {commandName} {subCommandName} {actionName}{paramString}";
                }
            }
        }

        private static IEnumerable<string> GenerateSimpleCombinations(
            string toolName,
            string commandName,
            string subCommandName,
            List<Parameter> requiredParams,
            List<Parameter> optionalParams)
        {
            foreach (var optionalCombination in GeneratePowerSet(optionalParams))
            {
                var allParams = requiredParams.Concat(optionalCombination).ToList();
                var paramString = allParams.Any()
                    ? " " + string.Join(" ", allParams.Select(p => $"--{p.Name} <{p.Name}>"))
                    : "";

                yield return $"{toolName} {commandName} {subCommandName}{paramString}";
            }
        }

        private static IEnumerable<string> GenerateSimpleCombinations(
            string toolName,
            string commandName,
            List<Parameter> requiredParams,
            List<Parameter> optionalParams)
        {
            foreach (var optionalCombination in GeneratePowerSet(optionalParams))
            {
                var allParams = requiredParams.Concat(optionalCombination).ToList();
                var paramString = allParams.Any()
                    ? " " + string.Join(" ", allParams.Select(p => $"--{p.Name} <{p.Name}>"))
                    : "";

                yield return $"{toolName} {commandName}{paramString}";
            }
        }

        private static IEnumerable<string> GenerateSimpleCombinations(
            string toolName,
            string commandName,
            string subCommandName,
            string actionName,
            List<Parameter> requiredParams,
            List<Parameter> optionalParams)
        {
            foreach (var optionalCombination in GeneratePowerSet(optionalParams))
            {
                var allParams = requiredParams.Concat(optionalCombination).ToList();
                var paramString = allParams.Any()
                    ? " " + string.Join(" ", allParams.Select(p => $"--{p.Name} <{p.Name}>"))
                    : "";

                yield return $"{toolName} {commandName} {subCommandName} {actionName}{paramString}";
            }
        }

        private static string BuildParameterString(
            List<Parameter> requiredParams,
            Dictionary<string, string> pipeOptions,
            IEnumerable<Parameter> optionalParams)
        {
            var allParams = new List<string>();

            // Add required parameters
            foreach (var param in requiredParams)
            {
                var value = pipeOptions.ContainsKey(param.Name)
                    ? pipeOptions[param.Name]
                    : $"<{param.Name}>";
                allParams.Add($"--{param.Name} {value}");
            }

            // Add pipe options (non-required)
            foreach (var (key, value) in pipeOptions.Where(po => !requiredParams.Any(rp => rp.Name == po.Key)))
            {
                allParams.Add($"--{key} {value}");
            }

            // Add optional parameters
            foreach (var param in optionalParams)
            {
                allParams.Add($"--{param.Name} <{param.Name}>");
            }

            return allParams.Any() ? " " + string.Join(" ", allParams) : "";
        }

        /// <summary>
        /// Generates all combinations of parameters that have pipe-separated default values
        /// </summary>
        /// <param name="parametersWithPipeOptions">Parameters that have DefaultValue with pipe-separated options</param>
        /// <returns>All possible combinations of parameter-value pairs</returns>
        private static IEnumerable<Dictionary<string, string>> GeneratePipeOptionCombinations(List<Parameter> parametersWithPipeOptions)
        {
            if (!parametersWithPipeOptions.Any())
            {
                yield return new Dictionary<string, string>();
                yield break;
            }

            var parameterOptions = parametersWithPipeOptions.ToDictionary(
                p => p.Name,
                p => p.DefaultValue.ToString().Split('|', StringSplitOptions.RemoveEmptyEntries).Select(opt => opt.Trim()).ToArray()
            );

            var combinations = GenerateCartesianProduct(parameterOptions);

            foreach (var combination in combinations)
            {
                yield return combination;
            }
        }

        /// <summary>
        /// Generates cartesian product of parameter options
        /// </summary>
        private static IEnumerable<Dictionary<string, string>> GenerateCartesianProduct(Dictionary<string, string[]> parameterOptions)
        {
            if (!parameterOptions.Any())
            {
                yield return new Dictionary<string, string>();
                yield break;
            }

            var keys = parameterOptions.Keys.ToArray();
            var values = parameterOptions.Values.ToArray();
            var indices = new int[keys.Length];

            do
            {
                yield return CreateCombination(keys, values, indices);
            } while (IncrementIndices(indices, values));
        }

        private static Dictionary<string, string> CreateCombination(string[] keys, string[][] values, int[] indices)
        {
            var combination = new Dictionary<string, string>();
            for (int i = 0; i < keys.Length; i++)
            {
                combination[keys[i]] = values[i][indices[i]];
            }
            return combination;
        }

        private static bool IncrementIndices(int[] indices, string[][] arrays)
        {
            for (int i = indices.Length - 1; i >= 0; i--)
            {
                indices[i]++;
                if (indices[i] < arrays[i].Length)
                    return true;

                indices[i] = 0;
            }
            return false;
        }

        /// <summary>
        /// Generates the power set (all possible subsets) of a list
        /// </summary>
        private static IEnumerable<IEnumerable<T>> GeneratePowerSet<T>(IEnumerable<T> items)
        {
            var itemsList = items.ToList();
            var powerSetSize = 1 << itemsList.Count; // 2^n

            for (int i = 0; i < powerSetSize; i++)
            {
                yield return GenerateSubset(itemsList, i);
            }
        }

        private static IEnumerable<T> GenerateSubset<T>(List<T> items, int mask)
        {
            for (int j = 0; j < items.Count; j++)
            {
                if ((mask & (1 << j)) != 0)
                {
                    yield return items[j];
                }
            }
        }
    }
}
