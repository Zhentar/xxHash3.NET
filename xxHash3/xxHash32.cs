using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace xxHash3
{
	public static class xxHash32
	{
		private const uint PRIME32_1 = 2654435761U;
		private const uint PRIME32_2 = 2246822519U;
		private const uint PRIME32_3 = 3266489917U;
		private const uint PRIME32_4 = 668265263U;
		private const uint PRIME32_5 = 374761393U;

		[StructLayout(LayoutKind.Sequential)]
		private struct QuadUint
		{
			public uint v1;
			public uint v2;
			public uint v3;
			public uint v4;
		}

		public static uint Hash(in ReadOnlySpan<byte> buffer)
		{
			unchecked
			{
				uint h32;

				var remainingBytes = buffer;
				var bulkuints = remainingBytes.PopAll<QuadUint>();

				if (!bulkuints.IsEmpty)
				{
					uint v1 = PRIME32_1 + PRIME32_2;
					uint v2 = PRIME32_2;
					uint v3 = 0;
					uint v4 = 0 - PRIME32_1;

					for (int i = 0; i < bulkuints.Length; i++)
					{
						ref readonly QuadUint val = ref bulkuints[i];
						v1 += val.v1 * PRIME32_2;
						v2 += val.v2 * PRIME32_2;
						v3 += val.v3 * PRIME32_2;
						v4 += val.v4 * PRIME32_2;

						v1 = RotateLeft(v1, 13);
						v2 = RotateLeft(v2, 13);
						v3 = RotateLeft(v3, 13);
						v4 = RotateLeft(v4, 13);

						v1 *= PRIME32_1;
						v2 *= PRIME32_1;
						v3 *= PRIME32_1;
						v4 *= PRIME32_1;
					}

					h32 = MergeValues(v1, v2, v3, v4);
				}
				else
				{
					h32 = PRIME32_5;
				}

				h32 += (uint)buffer.Length;


				ref uint remainingInt = ref Unsafe.As<byte, uint>(ref MemoryMarshal.GetReference(remainingBytes));


				switch (remainingBytes.Length >> 2)
				{
					case 3:
						h32 = RotateLeft(h32 + remainingInt * PRIME32_3, 17) * PRIME32_4;
						remainingInt = ref Unsafe.Add(ref remainingInt, 1);
						goto case 2;
					case 2:
						h32 = RotateLeft(h32 + remainingInt * PRIME32_3, 17) * PRIME32_4;
						remainingInt = ref Unsafe.Add(ref remainingInt, 1);
						goto case 1;
					case 1:
						h32 = RotateLeft(h32 + remainingInt * PRIME32_3, 17) * PRIME32_4;
						remainingInt = ref Unsafe.Add(ref remainingInt, 1);
						break;
				}


				ref byte remaining = ref Unsafe.As<uint, byte>(ref remainingInt);

				switch (remainingBytes.Length % sizeof(uint))
				{
					case 3:
						h32 = RotateLeft(h32 + remaining * PRIME32_5, 11) * PRIME32_1;
						remaining = ref Unsafe.Add(ref remaining, 1);
						goto case 2;
					case 2:
						h32 = RotateLeft(h32 + remaining * PRIME32_5, 11) * PRIME32_1;
						remaining = ref Unsafe.Add(ref remaining, 1);
						goto case 1;
					case 1:
						h32 = RotateLeft(h32 + remaining * PRIME32_5, 11) * PRIME32_1;
						break;
				}

				h32 ^= h32 >> 15;
				h32 *= PRIME32_2;
				h32 ^= h32 >> 13;
				h32 *= PRIME32_3;
				h32 ^= h32 >> 16;

				return h32;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint RotateLeft(uint val, int bits) => (val << bits) | (val >> (32 - bits));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static uint MergeValues(uint v1, uint v2, uint v3, uint v4)
		{
			return RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
		}




	}
}
