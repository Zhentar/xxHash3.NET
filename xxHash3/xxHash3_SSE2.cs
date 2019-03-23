#if !NETSTANDARD2_0
using System;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Intrinsics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace xxHash3
{
	public partial class xxHash3
	{
		private static int LongSequenceHash_SSE2(ReadOnlySpan<byte> userData, Span<ulong> accumulators)
		{
			const int VEC128_PER_STRIPE = STRIPE_BYTES / 16;
			var keys = MemoryMarshal.Cast<uint, KeyPair>(kKey);
			var dataVec = MemoryMarshal.Cast<byte, Vector128<uint>>(userData);
			var accVec = MemoryMarshal.Cast<ulong, Vector128<ulong>>(accumulators);

			while (dataVec.Length >= STRIPES_PER_BLOCK * VEC128_PER_STRIPE)
			{
				for (int i = 0; i < VEC128_PER_STRIPE; i++)
				{
					var acc = accVec[i];
					for (int j = 0; j < STRIPES_PER_BLOCK; j++)
					{
						var key = Unsafe.As<KeyPair, Vector128<uint>>(ref keys[i * (Unsafe.SizeOf<Vector128<uint>>() / Unsafe.SizeOf<KeyPair>()) + j]);
						var data = dataVec[i + j * VEC128_PER_STRIPE];
						var dk = Sse2.Add(data, key);
						var shuff = Sse2.Shuffle(dk, 0x31);
						Vector128<ulong> res = Sse2.Multiply(dk, shuff);
						Vector128<ulong> add = Sse2.Add(data.AsUInt64(), acc);
						acc = Sse2.Add(res, add);
					}

					var shifted = Sse2.ShiftRightLogical(acc, 47);
					acc = Sse2.Xor(acc, shifted);
					var k = Unsafe.As<KeyPair, Vector128<uint>>(ref keys[i * (Unsafe.SizeOf<Vector128<uint>>() / Unsafe.SizeOf<KeyPair>()) + STRIPES_PER_BLOCK]);
					var accKey = Sse2.Multiply(acc.AsUInt32(), k);
					var dataShuff = Sse2.Shuffle(acc.AsUInt32(), 0x31);
					var keyShuff = Sse2.Shuffle(k, 0x31);
					var dk2 = Sse2.Multiply(dataShuff, keyShuff);
					accVec[i] = Sse2.Xor(accKey, dk2);

				}

				dataVec = dataVec.Slice(STRIPES_PER_BLOCK * VEC128_PER_STRIPE);
			}

			int remainingStripes = dataVec.Length / VEC128_PER_STRIPE;

			if ((userData.Length & (Unsafe.SizeOf<Stripe>() - 1)) != 0) { remainingStripes++; }

			var lastStripeVec = MemoryMarshal.Cast<byte, Vector128<uint>>(userData.Slice(userData.Length - Unsafe.SizeOf<Stripe>()));

			/* last partial block */
			for (int i = 0; i < VEC128_PER_STRIPE; i++)
			{
				var acc = accVec[i];
				for (int j = 0; j < remainingStripes; j++)
				{
					var key = Unsafe.As<KeyPair, Vector128<uint>>(ref keys[i * (Unsafe.SizeOf<Vector128<uint>>() / Unsafe.SizeOf<KeyPair>()) + j]);
					var data = j == remainingStripes - 1 ? lastStripeVec[i] : dataVec[i + j * VEC128_PER_STRIPE];
					var dk = Sse2.Add(data, key);
					var shuff = Sse2.Shuffle(dk, 0x31);
					Vector128<ulong> res = Sse2.Multiply(dk, shuff);
					Vector128<ulong> add = Sse2.Add(data.AsUInt64(), acc);
					acc = Sse2.Add(res, add);
				}
				accVec[i] = acc;
			}

			return remainingStripes;
		}
	}
}


#endif
