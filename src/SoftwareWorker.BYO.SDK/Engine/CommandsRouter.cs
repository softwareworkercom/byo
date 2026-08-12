using System.CommandLine;
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
            catch (Exception ex) when (ex is MissingMethodException || ex is TypeLoadException || ex is MissingFieldException)
            {
                UserInterfaceService.ShowError(
                    $"Error: {ex.GetType().Name}: {ex.Message}\n" +
                    "This usually means an installed plugin was built against a different version of SoftwareWorker.BYO.SDK than the one currently installed. " +
                    "Try updating or reinstalling the plugin (byo plugin install <plugin>) to a version compatible with the current CLI.");
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
