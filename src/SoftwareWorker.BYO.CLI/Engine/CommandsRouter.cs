using System.CommandLine;
using SoftwareWorker.BYO.CLI.Helpers;
using SoftwareWorker.BYO.CLI.Service;
using SoftwareWorker.BYO.CLI.Shell;
using SoftwareWorker.BYO.SDK.Helpers;
using SoftwareWorker.BYO.SDK.Services;

namespace SoftwareWorker.BYO.CLI.Engine
{
    public class CommandsRouter
    {
        public static int Route(string[] args)
        {
            try
            {
                AliasService.EnsureAliasConfigured();

                // Running "byo" on its own in a terminal opens the interactive shell.
                // Scripts and redirected input keep the regular command-line behavior.
                if (args.Length == 0 && UserInterfaceService.IsInteractive)
                {
                    return InteractiveShell.Run();
                }

                var rootCommand = BuildRootCommand();
                var parseResult = rootCommand.Parse(args);
                return parseResult.Invoke();
            }
            catch (Exception ex) when (PluginCompatibilityHelper.IsCompatibilityException(ex))
            {
                UserInterfaceService.ShowError(PluginCompatibilityHelper.BuildErrorMessage(ex));
                return 1;
            }
            catch (Exception ex)
            {
                UserInterfaceService.ShowError($"Error: {ex.Message}");
                return 1;
            }
        }

        internal static RootCommand BuildRootCommand()
        {
            var trunkCommands = CommandsScanner.BuildFromReflection();

            var rootCommand = new RootCommand();
            CommandsBuilder.LoadCommands(rootCommand, trunkCommands);
            return rootCommand;
        }
    }
}
