using FloName;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;

namespace FloNameConsole.Commands
{
    public class GenerateCommand : Command<GenerateCommand.Settings>
    {
        private readonly FilenameGenerator _generator;

        public GenerateCommand(FilenameGenerator generator)
        {
            _generator = generator;
        }

        public class Settings : CommandSettings
        {
            [CommandArgument(0, "<lang>")]
            [Description("Language code e.g. 'en'")]
            public string Lang { get; set; } = string.Empty;

            [CommandOption("-f|--format <FORMAT>")]
            [Description("Format string e.g. '{W}-{W}'")]
            public string Format { get; set; } = "{W}-{W}";

            [CommandOption("-e|--extension <EXTENSION>")]
            [Description("File extension e.g. '.txt'")]
            [DefaultValue(".txt")]
            public string Extension { get; set; } = ".txt";
        }

        protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
        {
            try
            {
                var result = _generator.For(settings.Lang)
                    .WithExtension(settings.Extension)
                    .WithFormat(settings.Format)
                    .Generate();

                AnsiConsole.MarkupLine($"[green]{result}[/]");
                return 0;
            }
            catch (Exception ex)
            {
                AnsiConsole.MarkupLine($"[red]Error: {ex.Message}[/]");
                return 1;
            }
        }
    }
}