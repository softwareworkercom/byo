using System.CommandLine;
using SoftwareWorker.BYO.CLI.Core.Helpers;
using SoftwareWorker.BYO.CLI.Core.Service;

namespace SoftwareWorker.BYO.CLI.Core.Engine
{
    public class CommandsRouter
    {
        public static int Route(string[] args)
        {
            try
            {
                var trunkCommands = CommandsScanner.BuildFromReflection();

                var rootCommand = new RootCommand();
                CommandsBuilder.LoadCommands(rootCommand, trunkCommands);
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
    }
}
