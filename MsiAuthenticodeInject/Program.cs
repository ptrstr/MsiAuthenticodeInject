using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;

namespace MsiAuthenticodeInject
{
    internal class Program
    {
        private static void ValidateMsi(ArgumentResult symbolresult)
        {
            try
            {
                using (new MsiInjector(symbolresult.GetValueOrDefault<FileInfo>().FullName))
                {
                }
            }
            catch (Exception ex)
            {
                symbolresult.ErrorMessage = ex.Message;
            }
        }

        private static Command BuildInjectCommand()
        {
            var command = new Command("inject", "Injects data after the certificate");

            var targetArgument = new Argument<FileInfo>("MSI to inject");
            command.AddArgument(targetArgument);
            targetArgument.AddValidator(ValidateMsi);

            var payloadArgument = new Argument<FileInfo>("payload to inject");
            command.AddArgument(payloadArgument);

            command.SetHandler((target, payload) =>
            {
                using MsiInjector injector = new MsiInjector(target.FullName);

                injector.SetInjection(File.ReadAllBytes(payload.FullName));
            }, targetArgument, payloadArgument);

            return command;
        }


        private static Command BuildVerifyCommand()
        {
            var command = new Command("verify", "Verifies if data lies after certificate");

            var pathArgument = new Argument<FileInfo>("MSI to verify");
            command.AddArgument(pathArgument);
            pathArgument.AddValidator(ValidateMsi);

            command.SetHandler(path =>
            {
                using var injector = new MsiInjector(path.FullName);
                var injection = injector.GetInjection();

                Console.WriteLine(injection.Length == 0
                    ? "No data found after certificate"
                    : $"Found {injection.Length} bytes of data after certificate");
            }, pathArgument);

            return command;
        }

        private static Command BuildExtractCommand()
        {
            var command = new Command("extract", "Extracts data after certificate");

            var targetArgument = new Argument<FileInfo>("MSI to extract");
            command.AddArgument(targetArgument);
            targetArgument.AddValidator(ValidateMsi);

            var destinationArgument = new Argument<FileInfo>("file to extract to");
            command.AddArgument(destinationArgument);

            command.SetHandler((target, destination) =>
            {
                using var injector = new MsiInjector(target.FullName);
                var injection = injector.GetInjection();

                File.WriteAllBytes(destination.FullName, injection);
            }, targetArgument, destinationArgument);

            return command;
        }

        public static int Main(string[] args)
        {
            var rootCommand = new RootCommand("MSI Certificate Padding Injection Tool");
            rootCommand.AddCommand(BuildInjectCommand());
            rootCommand.AddCommand(BuildExtractCommand());
            rootCommand.AddCommand(BuildVerifyCommand());

            return rootCommand.Invoke(args);
        }
    }
}