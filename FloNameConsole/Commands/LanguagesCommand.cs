using FloName;
using Spectre.Console;
using Spectre.Console.Cli;

namespace FloName.Console.Commands
{
    public class LanguagesCommand : Command<LanguagesCommand.Settings>
    {
        private readonly FilenameGenerator _generator;

        public LanguagesCommand(FilenameGenerator generator)
        {
            _generator = generator;
        }

        public class Settings : CommandSettings { }

        protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
        {
            var languages = _generator.SupportedLanguages();

            var table = new Table();
            table.AddColumn("Supported Languages");
            table.Border(TableBorder.Rounded);

            foreach (var lang in languages)
                table.AddRow(lang);

            AnsiConsole.Write(table);
            return 0;
        }
    }
}