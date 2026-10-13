using SoftwareWorker.BYO.SDK.Abstractions.Attributes;
using SoftwareWorker.BYO.SDK.Abstractions.Model.Command;
using SoftwareWorker.BYO.SDK.Services;
using System.CommandLine;
using System.CommandLine.Completions;
using System.Diagnostics;
using System.Reflection;
using System.Text.RegularExpressions;
using SoftwareWorker.BYO.CLI.Helpers;
using SoftwareWorker.BYO.SDK;


namespace SoftwareWorker.BYO.CLI.Engine
{
    public class CommandsBuilder
    {
        private const string ScheduleOptionName = "schedule";
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
                    var asyncOption = AddAsyncOption(systemCommand);
                    ConfigureDynamicParameterHandling(systemCommand, trunkCommand.Handler);
                    SetAction(systemCommand, trunkCommand.Handler, optionsMap, scheduleOption, asyncOption);
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
            var asyncOption = AddAsyncOption(actionCommand);
            ConfigureDynamicParameterHandling(actionCommand, action.Handler);
            SetAction(actionCommand, action.Handler, optionsMap, scheduleOption, asyncOption);
            return actionCommand;
        }

        private static Command CreateSubCommand(BranchCommand subCmd)
        {
            var subCommand = new Command(subCmd.Name, subCmd.Description);
            var optionsMap = AddParametersToCommand(subCommand, subCmd.Parameters);
            var scheduleOption = AddScheduleOption(subCommand);
            var asyncOption = AddAsyncOption(subCommand);
            ConfigureDynamicParameterHandling(subCommand, subCmd.Handler);
            SetAction(subCommand, subCmd.Handler, optionsMap, scheduleOption, asyncOption);
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

                // Pipe-separated default values ("a|b|c") are the allowed choices, so offer them as completions.
                var choices = GetEnumeratedValues(param.DefaultValue);
                if (choices.Length > 0)
                {
                    option.CompletionSources.Add(_ => choices.Select(choice => new CompletionItem(choice)));
                }

                optionsMap[param.Name] = option;
                command.Add(option);
            }

            return optionsMap;
        }

        private static string[] GetEnumeratedValues(object? defaultValue)
        {
            var value = defaultValue?.ToString();
            if (string.IsNullOrWhiteSpace(value) || !value.Contains('|'))
            {
                return [];
            }

            return value.Split('|', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
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
                var runAsync = parseResult.CommandResult.GetValue(asyncOption);

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

                // Non-interactive runs must supply every required parameter; interactive runs leave missing values to the handler
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

                    var isSuccessful = true;
                    Stopwatch stopwatch = Stopwatch.StartNew();

                    try
                    {
                        await handlerInstance.ExecuteAsync();
                    }
                    catch (Exception ex) when (PluginCompatibilityHelper.IsCompatibilityException(ex))
                    {
                        isSuccessful = false;
                        UserInterfaceService.ShowError(PluginCompatibilityHelper.BuildErrorMessage(ex, handlerType.Assembly));
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

            if (Environment.GetCommandLineArgs().Length == 0)
            {
                error = "Unable to determine command entry point for background execution.";
                return false;
            }

            var startInfo = CreateSelfProcessStartInfo(commandTokens);
            startInfo.CreateNoWindow = true;

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

        /// <summary>
        /// Creates a start info that runs this CLI again with the given arguments.
        /// </summary>
        internal static ProcessStartInfo CreateSelfProcessStartInfo(IEnumerable<string> commandTokens)
        {
            var entryPoint = Environment.GetCommandLineArgs().FirstOrDefault() ?? string.Empty;
            var processPath = Environment.ProcessPath;
            var executable = string.IsNullOrWhiteSpace(processPath) ? entryPoint : processPath;

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                WorkingDirectory = Environment.CurrentDirectory
            };

            // Only the dotnet muxer ("dotnet byo.dll") needs the entry assembly as its first argument;
            // the byo apphost would treat it as an unknown command.
            var isDotnetHost = string.Equals(Path.GetFileNameWithoutExtension(executable), "dotnet", StringComparison.OrdinalIgnoreCase);
            if (isDotnetHost && entryPoint.EndsWith(".dll", StringComparison.OrdinalIgnoreCase))
            {
                startInfo.ArgumentList.Add(entryPoint);
            }

            foreach (var token in commandTokens)
            {
                startInfo.ArgumentList.Add(token);
            }

            return startInfo;
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
    }
}
