using System;
using System.Linq;

namespace MsiInjectPoc
{
	// format: <data><Length of data as uint32>DCFG
	// note: Fields in <> do not include the <> and are variables
	internal class InjectedData
	{
		public static readonly byte[] Signature = { (byte)'D', (byte)'C', (byte)'F', (byte)'G' };

		public InjectedData()
		{
			this.Data = Array.Empty<byte>();
		}

		public InjectedData(byte[] data)
		{
			this.Data = data;
		}

		public byte[] Data { get; set; }

		public static InjectedData TryFrom(byte[] data)
		{
			int tailSize = Signature.Length + SizeField.Size;

			if (data.Length < tailSize || !data.Skip(data.Length - Signature.Length).SequenceEqual(Signature))
			{
				return null;
			}

			uint size = SizeField.Deserialize(data, data.Length - tailSize);
			if (size > data.Length - tailSize)
			{
				return null;
			}

			byte[] encodedData = new byte[size];
			Array.Copy(data, data.Length - tailSize - size, encodedData, 0, encodedData.Length);

			return new InjectedData(encodedData);
		}

		public byte[] Serialize() => this.Data.Concat(SizeField.Serialize((uint)this.Data.LongLength)).Concat(Signature).ToArray();

		internal class SizeField
		{
			public const int Size = 4;

			public static uint Deserialize(byte[] data) => BitConverter.ToUInt32(data, 0);

			public static uint Deserialize(byte[] data, int offset) => BitConverter.ToUInt32(data, offset);

			public static byte[] Serialize(uint size) => BitConverter.GetBytes(size);
		}
	}
}
