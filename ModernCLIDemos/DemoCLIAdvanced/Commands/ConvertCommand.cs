﻿using Emulator;
using Spectre.Console;
using System.CommandLine;
using System.CommandLine.Invocation;
using System.CommandLine.NamingConventionBinder;
using System.IO;
using System.Linq;
using System.Threading;

namespace AdvancedCLI
{
    [ExampleUsage("convert zarlor.bin", "Convert 'zarlor.bin' image file to 'zarlor.lnx' ROM")]
    [ExampleUsage("convert zarlor.bin --output game.lnx", "Convert 'zarlor.bin' to 'game.lnx'")]
    public class ConvertCommand : Command
    {
        public ConvertCommand() : base("convert", "Converts binary ROM to Handy ROM file")
        {
            // Arguments
            Argument inputArgument = 
                new Argument<FileInfo>("input", "Binary ROM file (*.bin)")
                .ExistingOnly();
            this.AddArgument(inputArgument);

            // Options
            this.AddOption(new Option<FileInfo>(new[] { "--output", "-o" },
                isDefault: true,
                parseArgument: arg =>
                {
                    if (arg.Tokens.Any()) return new FileInfo(arg.Tokens.Single().Value);
                    FileInfo input = arg.FindResultFor(inputArgument)?.GetValueOrDefault<FileInfo>();
                    if (input == null) return null;
                    return new FileInfo(Path.ChangeExtension(input.Name, ".lnx"));
                },
                description: "Output Handy ROM file")
                .LegalFileNamesOnly());

            // Naming convention binding
            this.Handler = CommandHandler.Create<FileInfo, FileInfo, InvocationContext>(Convert);
        }

        private void Convert(FileInfo input, FileInfo output, InvocationContext context)
        {
            var token = context.GetCancellationToken();

            AnsiConsole.Progress()
                .AutoRefresh(false)
                .AutoClear(false)
                .HideCompleted(false)
                .Columns(new ProgressColumn[]
                {
                    new TaskDescriptionColumn(),    // Task description
                    new ProgressBarColumn(),        // Progress bar
                    new PercentageColumn(),         // Percentage
                    new RemainingTimeColumn(),      // Remaining time
                    new SpinnerColumn() { Spinner = Spinner.Known.Default } 
                })
                .Start(progress =>
                {
                    // Define tasks
                    var task1 = progress.AddTask("[green]Converting[/]");
                    var task2 = progress.AddTask("[green]Writing[/]");

                    while (!progress.IsFinished && !token.IsCancellationRequested)
                    {
                        task1.Increment(1.5);
                        task2.Increment(0.5);
                        Thread.Sleep(20);

                        // Check whether output is piped 
                        if (!context.Console.IsOutputRedirected) 
                            progress.Refresh();
                    }
                });

            context.Console.Out.Write("Finished converting");
            context.ExitCode = 0; // Non-zero exit code indicates failure
        }
    }
}