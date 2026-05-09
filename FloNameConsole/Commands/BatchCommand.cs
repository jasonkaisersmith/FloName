using FloName;
using Spectre.Console;
using Spectre.Console.Cli;
using System.ComponentModel;
using System.Reflection.Emit;

namespace FloNameConsole.Commands
{
    public class BatchCommand : Command<BatchCommand.Settings>
    {
        private readonly FilenameGenerator _generator;

        public BatchCommand(FilenameGenerator generator)
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

            [CommandOption("-c|--count <COUNT>")]
            [Description("Number of filenames to generate")]
            [DefaultValue(10)]
            public int Count { get; set; } = 10;

            [CommandOption("-e|--extension <EXTENSION>")]
            [Description("File extension e.g. '.txt'")]
            [DefaultValue(".txt")]
            public string Extension { get; set; } = ".txt";

            [CommandOption("-t|--table")]
            [Description("Display results as a table")]
            public bool Table { get; set; }
        }

        protected override int Execute(CommandContext context, Settings settings, CancellationToken cancellationToken)
        {
            try
            {
                var results = _generator.For(settings.Lang)
                    .WithFormat(settings.Format)
                    .WithExtension(settings.Extension)
                    .GenerateBatch(settings.Count);

                if (settings.Table)
                {
                    var table = new Table();
                    table.AddColumn("#");
                    table.AddColumn("Filename");
                    table.Border(TableBorder.Rounded);

                    for (int i = 0; i < results.Count; i++)
                        table.AddRow((i + 1).ToString(), results[i]);

                    AnsiConsole.Write(table);
                }
                else
                {
                    foreach (var name in results)
                        AnsiConsole.MarkupLine($"[green]{name}[/]");
                }

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