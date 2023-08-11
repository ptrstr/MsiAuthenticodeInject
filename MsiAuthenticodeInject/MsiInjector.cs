using System.IO;
using System.Security.Cryptography.Pkcs;

namespace MsiAuthenticodeInject
{
	internal class MsiInjector : IDisposable
	{
		private const string DigitalSignatureName = "\x0005DigitalSignature";

		private readonly MicrosoftCfb _cfb;

		public MsiInjector(string path)
		{
            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Could not open MSI file");
            }

			this._cfb = new MicrosoftCfb(path);

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
			var certStreamInfo = this._cfb.GetStreamInfo(DigitalSignatureName);

            var origInjection = this.GetInjection();

            var delta = newInjection.LongLength - origInjection.LongLength;

			var certStream = certStreamInfo.GetStream();
			certStream.Seek(0, SeekOrigin.Begin);
			certStream.SetLength(certStream.Length + delta);
			certStream.Seek(-newInjection.Length, SeekOrigin.End);
			certStream.Write(newInjection, 0, newInjection.Length);
			certStream.Flush();
			certStream.Dispose();
		}

		public byte[] GetInjection()
		{
			var certStreamInfo = this._cfb.GetStreamInfo(DigitalSignatureName);
			var certStream = certStreamInfo.GetStream();

			certStream.Seek(0, SeekOrigin.Begin);
			var certStreamData = new byte[certStream.Length];
			var readLength = certStream.Read(certStreamData, 0, certStreamData.Length);
			certStream.Dispose();

			if (readLength != certStreamData.Length)
			{
				throw new IOException("Failed to fully read stream");
			}

			var cms = new SignedCms();
			cms.Decode(certStreamData);

			return certStreamData.Skip(cms.Encode().Length).ToArray();
		}
	}
}