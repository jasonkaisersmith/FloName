using FloName;
using FloName.Console.Commands;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Spectre.Console.Cli;
using Microsoft.Extensions.Logging.Console;
using FloNameConsole.Commands;

namespace FloNameConsole
{
    internal class Program
    {
        static int Main(string[] args)
        {
            var services = new ServiceCollection();

            services.AddLogging(b => b.AddConsole().SetMinimumLevel(LogLevel.Warning));
            services.AddSingleton<FilenameGenerator>(sp =>
            {
                var logger = sp.GetRequiredService<ILogger<FilenameGenerator>>();
                var path = Path.Combine(AppContext.BaseDirectory, "dicts");
                return new FilenameGenerator(path, logger);
            });

            var registrar = new TypeRegistrar(services);
            var app = new CommandApp(registrar);

            app.Configure(config =>
            {
                config.SetApplicationName("floname");
                config.SetApplicationVersion("1.0.0");

                config.AddCommand<GenerateCommand>("generate")
                    .WithDescription("Generate a single filename.")
                    .WithExample("generate", "en", "--format", "{W}-{W}", "--extension", ".txt");

                config.AddCommand<BatchCommand>("batch")
                    .WithDescription("Generate a batch of filenames.")
                    .WithExample("batch", "en", "--format", "{W}-{W}", "--count", "10");

                config.AddCommand<LanguagesCommand>("languages")
                    .WithDescription("List all supported languages.");

                config.AddCommand<HelpCommand>("tokens")
                    .WithDescription("Show all supported format tokens and modifiers.");
            });

            return app.Run(args);
        }
    }
}
            //FloName.FilenameGenerator generator = new();
            //var result = generator.GenerateComplexName("en", "{w}-{W}-{w}", extension: "log");
            //Console.WriteLine($"3 words: {result}");

            //result = generator.GenerateComplexName("en", "{w}-{w}-{w}-{DATE:yyyyMMdd_HHmmss_fff_gg_zzz}", extension: "log");
            //Console.WriteLine($"3 words + date: {result}");

            //result = generator.GenerateComplexName("en", "{A}{A}{A}-{w}-{N}{n}-{DATE:yyyy_MMM_ddd}-{AN}", extension: "log");
            //Console.WriteLine($"Complex: {result}");

            //result = generator.GenerateComplexName("en", "{A:12:-}", extension: "log");
            //Console.WriteLine($"Separator: {result}");

            //var resultList = generator.GenerateBatch("en", "{A}{A}{A}-{w}-{N}{n}-{DATE:yyyy_MMM_ddd}-{SEQ:100}", 5, extension: "log");
            //foreach (var file in resultList)
            //    Console.WriteLine(file);

            //var app = new CommandApp<GreetCommand>();
            //return app.Run(args);

