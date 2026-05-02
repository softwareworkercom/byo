using System.CommandLine;

namespace SoftwareWorker.BYO.CLI.Core.Engine
{
    public class CommandsRouter
    {
        public static int Route(string[] args)
        {
            var trunkCommands = CommandsScanner.BuildFromReflection();
            CommandsBuilder.UpdateCommandsBookmark(trunkCommands);

            var rootCommand = new RootCommand();
            CommandsBuilder.LoadCommands(rootCommand, trunkCommands);
            var parseResult = rootCommand.Parse(args);
            return parseResult.Invoke();
        }
    }
}
