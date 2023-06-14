using System;
using System.IO.Packaging;
using System.Reflection;

namespace MsiInjectPoc
{
	internal class StorageRoot
	{
		public static StorageInfo Open(string path)
		{
			return (StorageInfo)InvokeStorageRootMethod(null, "Open", path);
		}

		public static void Close(StorageInfo storageRoot)
		{
			InvokeStorageRootMethod(storageRoot, "Close");
		}

		private static object InvokeStorageRootMethod(StorageInfo storageRoot, string methodName, params object[] methodArgs)
		{
			Type storageRootType = typeof(StorageInfo).Assembly.GetType("System.IO.Packaging.StorageRoot", true, false);
			return storageRootType.InvokeMember(methodName,
				BindingFlags.Static | BindingFlags.Instance | BindingFlags.Public |
				BindingFlags.NonPublic | BindingFlags.InvokeMethod,
				null, storageRoot, methodArgs);
		}
	}
}