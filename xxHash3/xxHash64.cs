using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace xxHash3
{
	public static class xxHash64
	{
		private const ulong PRIME64_1 = 11400714785074694791UL;
		private const ulong PRIME64_2 = 14029467366897019727UL;
		private const ulong PRIME64_3 = 1609587929392839161UL;
		private const ulong PRIME64_4 = 9650029242287828579UL;
		private const ulong PRIME64_5 = 2870177450012600261UL;

		[StructLayout(LayoutKind.Sequential)]
		private struct QuadUlong
		{
			public ulong v1;
			public ulong v2;
			public ulong v3;
			public ulong v4;
		}

		public static ulong Hash(in ReadOnlySpan<byte> buffer)
		{
			unchecked
			{
				var remainingBytes = buffer;
				var bulkVals = remainingBytes.PopAll<QuadUlong>();

				var h64 = !bulkVals.IsEmpty ? BulkStride(bulkVals) : PRIME64_5;

				h64 += (uint)buffer.Length;

				var ulongSpan = remainingBytes.PopAll<ulong>();
				for (int i = 0; i < ulongSpan.Length; i++)
				{
					var val = ulongSpan[i] * PRIME64_2;
					val = RotateLeft(val, 31);
					val *= PRIME64_1;
					h64 ^= val;
					h64 = RotateLeft(h64, 27) * PRIME64_1;
					h64 += PRIME64_4;
				}

				ref byte remaining = ref MemoryMarshal.GetReference(remainingBytes);
				if (remainingBytes.Length >= sizeof(uint))
				{
					h64 ^= Unsafe.As<byte, uint>(ref remaining) * PRIME64_1;
					h64 = RotateLeft(h64, 23) * PRIME64_2;
					h64 += PRIME64_3;
					Unsafe.Add(ref remaining, sizeof(uint));
				}

				switch (remainingBytes.Length % sizeof(uint))
				{
					case 3:
						h64 = RotateLeft(h64 ^ remaining * PRIME64_5, 11) * PRIME64_1;
						Unsafe.Add(ref remaining, 1);
						goto case 2;
					case 2:
						h64 = RotateLeft(h64 ^ remaining * PRIME64_5, 11) * PRIME64_1;
						Unsafe.Add(ref remaining, 1);
						goto case 1;
					case 1:
						h64 = RotateLeft(h64 ^ remaining * PRIME64_5, 11) * PRIME64_1;
						break;
				}

				h64 ^= h64 >> 33;
				h64 *= PRIME64_2;
				h64 ^= h64 >> 29;
				h64 *= PRIME64_3;
				h64 ^= h64 >> 32;

				return h64;
			}
		}

		[MethodImpl(MethodImplOptions.NoInlining)]
		private static ulong BulkStride(in ReadOnlySpan<QuadUlong> bulkVals)
		{
			unchecked
			{
				ulong acc1 = 0 + PRIME64_1 + PRIME64_2;
				ulong acc2 = 0 + PRIME64_2;
				ulong acc3 = 0 + 0;
				ulong acc4 = 0 - PRIME64_1;

				for (int i = 0; i < bulkVals.Length; i++)
				{
					ref readonly QuadUlong val = ref bulkVals[i];

					acc1 += val.v1 * PRIME64_2;
					acc2 += val.v2 * PRIME64_2;
					acc3 += val.v3 * PRIME64_2;
					acc4 += val.v4 * PRIME64_2;

					acc1 = RotateLeft(acc1, 31);
					acc2 = RotateLeft(acc2, 31);
					acc3 = RotateLeft(acc3, 31);
					acc4 = RotateLeft(acc4, 31);

					acc1 *= PRIME64_1;
					acc2 *= PRIME64_1;
					acc3 *= PRIME64_1;
					acc4 *= PRIME64_1;
				}

				return MergeValues(acc1, acc2, acc3, acc4);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong RotateLeft(ulong val, int bits) => (val << bits) | (val >> (64 - bits));

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong MergeValues(ulong v1, ulong v2, ulong v3, ulong v4)
		{
			var acc = RotateLeft(v1, 1) + RotateLeft(v2, 7) + RotateLeft(v3, 12) + RotateLeft(v4, 18);
			acc = MergeAccumulator(acc, v1);
			acc = MergeAccumulator(acc, v2);
			acc = MergeAccumulator(acc, v3);
			acc = MergeAccumulator(acc, v4);
			return acc;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong MergeAccumulator(ulong accMain, ulong accN)
		{
			accN = (accN * PRIME64_2);
			accN = RotateLeft(accN, 31);
			accN = accN * PRIME64_1;
			accMain ^= accN;
			accMain *= PRIME64_1;
			return accMain + PRIME64_4;
		}

	}
}
