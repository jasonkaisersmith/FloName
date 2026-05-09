using FloName;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FloName.Console.Commands
{
    public class HelpCommand : Command<HelpCommand.Settings>
    {
        public class Settings : CommandSettings { }

        protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
        {
            var help = FilenameGenerator.GetHelp();

            AnsiConsole.MarkupLine($"[bold yellow]FloName Format String Reference[/]");
            AnsiConsole.MarkupLine($"[grey]{help.Description}[/]");
            AnsiConsole.WriteLine();

            // Tokens table
            var tokenTable = new Table();
            tokenTable.AddColumn("[bold]Token[/]");
            tokenTable.AddColumn("[bold]Description[/]");
            tokenTable.AddColumn("[bold]Example[/]");
            tokenTable.Border(TableBorder.Rounded);
            tokenTable.Title("[yellow]Tokens[/]");

            foreach (var token in help.Tokens)
                tokenTable.AddRow(
                    $"[cyan]{token.Token}[/]",
                    token.Description,
                    $"[grey]{token.Example}[/]");

            AnsiConsole.Write(tokenTable);
            AnsiConsole.WriteLine();

            // Modifiers table
            var modTable = new Table();
            modTable.AddColumn("[bold]Modifier[/]");
            modTable.AddColumn("[bold]Description[/]");
            modTable.AddColumn("[bold]Example[/]");
            modTable.Border(TableBorder.Rounded);
            modTable.Title("[yellow]Modifiers[/]");

            foreach (var mod in help.Modifiers)
                modTable.AddRow(
                    $"[cyan]{mod.Token}[/]",
                    mod.Description,
                    $"[grey]{mod.Example}[/]");

            AnsiConsole.Write(modTable);
            AnsiConsole.WriteLine();

            // Examples
            AnsiConsole.MarkupLine("[bold yellow]Examples[/]");
            foreach (var example in help.Examples)
                AnsiConsole.MarkupLine($"  [grey]{example}[/]");

            return 0;
        }
    }
}