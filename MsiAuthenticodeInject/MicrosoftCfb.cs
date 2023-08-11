using System;
using System.IO.Packaging;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MsiAuthenticodeInject
{
	internal class MicrosoftCfb : IDisposable
	{
        private readonly StorageInfo _root;

		private bool _disposed;

		public MicrosoftCfb(string path)
        {
            this._root = StorageRoot.Open(path);
        }

		~MicrosoftCfb()
		{
			this.Dispose();
		}

		public StorageInfo GetSubStorageInfo(string name) => this._root.GetSubStorageInfo(name);

		public StreamInfo GetStreamInfo(string name) => this._root.GetStreamInfo(name);

		public StorageInfo[] GetSubStorages() => this._root.GetSubStorages();

		public StreamInfo[] GetStreams() => this._root.GetStreams();

		public void DeleteSubStorage(string name) => this._root.DeleteSubStorage(name);

		public void DeleteStream(string name) => this._root.DeleteStream(name);

		public void Dispose()
		{
			if (this._disposed)
			{
				return;
			}

			this._disposed = true;
			
            StorageRoot.Close(this._root);
		}
	}
}