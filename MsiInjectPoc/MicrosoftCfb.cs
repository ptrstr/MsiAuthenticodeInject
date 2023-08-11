using System;
using System.IO.Packaging;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace MsiInjectPoc
{
	internal class MicrosoftCfb : IDisposable
	{
		private static bool _patched;

		private readonly StorageInfo _root;

		private bool _disposed;

		public MicrosoftCfb(string path)
		{
			this._root = StorageRoot.Open(path);

			if (this._root == null)
			{
				throw new ArgumentException();
			}
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

			if (this._root != null)
			{
				StorageRoot.Close(this._root);
			}
		}

		// https://referencesource.microsoft.com/#windowsbase/Base/System/IO/Packaging/CompoundFile/StorageInfo.cs,1381
		// Only useful for listing entries.
		private static void EnableSpecialEntryNames()
		{
			if (_patched)
			{
				return;
			}

			_patched = true;

			Type containerUtilities = typeof(StorageInfo).Assembly.GetType("MS.Internal.IO.Packaging.CompoundFile.ContainerUtilities", true, false);

			MethodInfo isReservedName = containerUtilities?.GetMethod("IsReservedName", BindingFlags.NonPublic | BindingFlags.Static);
			MethodInfo isReservedNamePatch = ((Func<string, bool>)IsReservedNamePatch).Method;

			HookFunction(isReservedName, isReservedNamePatch);
		}

		private static void HookFunction(MethodInfo methodToReplace, MethodInfo methodToInject)
		{
			RuntimeHelpers.PrepareMethod(methodToReplace.MethodHandle);
			RuntimeHelpers.PrepareMethod(methodToInject.MethodHandle);

			IntPtr injectionAddr = methodToInject.MethodHandle.GetFunctionPointer();
			IntPtr targetMetadataAddr = methodToInject.MethodHandle.Value;
			IntPtr targetAddr = methodToInject.MethodHandle.GetFunctionPointer();

			unsafe
			{
				int offset = 0;

				while (true)
				{
					if ((IntPtr.Size == 4 && *(int*)(targetMetadataAddr.ToInt32() + offset) == targetAddr.ToInt32()) ||
						(IntPtr.Size == 8 && *(long*)(targetMetadataAddr.ToInt64() + offset) == targetAddr.ToInt64()))
					{
						break;
					}

					offset++;
				}

				if (IntPtr.Size == 4)
				{
					*(int*)(targetMetadataAddr.ToInt32() + offset) = injectionAddr.ToInt32();
				}
				else
				{
					*(long*)(targetMetadataAddr.ToInt64() + offset) = injectionAddr.ToInt64();
				}
			}
		}

		private static bool IsReservedNamePatch(string nameString) => false;
	}
}