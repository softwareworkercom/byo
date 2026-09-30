using System.CommandLine;
using SoftwareWorker.BYO.CLI.Core.Helpers;
using SoftwareWorker.BYO.CLI.Core.Service;
using SoftwareWorker.BYO.CLI.Core.Shell;

namespace SoftwareWorker.BYO.CLI.Core.Engine
{
    public class CommandsRouter
    {
        public static int Route(string[] args)
        {
            try
            {
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
