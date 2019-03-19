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
		private static int AccumulateStripes_AVX2(in ReadOnlySpan<byte> userData, in Span<ulong> accumulators)
		{
			const int VEC256_PER_STRIPE = STRIPE_BYTES / 32;
			var keys = MemoryMarshal.Cast<uint, KeyPair>(kKey);
			var dataVec = MemoryMarshal.Cast<byte, Vector256<uint>>(userData);
			var accVec = MemoryMarshal.Cast<ulong, Vector256<ulong>>(accumulators);

			while (dataVec.Length >= STRIPES_PER_BLOCK * VEC256_PER_STRIPE)
			{
				for (int i = 0; i < VEC256_PER_STRIPE; i++)
				{
					var acc = accVec[i];
					for (int j = 0; j < STRIPES_PER_BLOCK; j++)
					{
						var key = Unsafe.As<KeyPair, Vector256<uint>>(ref keys[i * (Unsafe.SizeOf<Vector256<uint>>() / Unsafe.SizeOf<KeyPair>()) + j]);
						var data = dataVec[i + j * VEC256_PER_STRIPE];
						var dk = Avx2.Add(data, key);
						var shuff = Avx2.Shuffle(dk, 0x31);
						Vector256<ulong> res = Avx2.Multiply(dk, shuff);
						Vector256<ulong> add = Avx2.Add(data.AsUInt64(), acc);
						acc = Avx2.Add(res, add);
					}

					var shifted = Avx2.ShiftRightLogical(acc, 47);
					acc = Avx2.Xor(acc, shifted);
					var k = Unsafe.As<KeyPair, Vector256<uint>>(ref keys[i * (Unsafe.SizeOf<Vector256<uint>>() / Unsafe.SizeOf<KeyPair>()) + STRIPES_PER_BLOCK]);
					var accKey = Avx2.Multiply(acc.AsUInt32(), k);
					var dataShuff = Avx2.Shuffle(acc.AsUInt32(), 0x31);
					var keyShuff = Avx2.Shuffle(k, 0x31);
					var dk2 = Avx2.Multiply(dataShuff, keyShuff);
					accVec[i] = Avx2.Xor(accKey, dk2);

				}

				dataVec = dataVec.Slice(STRIPES_PER_BLOCK * VEC256_PER_STRIPE);
			}

			int remainingStripes = dataVec.Length / VEC256_PER_STRIPE;

			if ((userData.Length & (Unsafe.SizeOf<Stripe>() - 1)) != 0) { remainingStripes++; }

			var lastStripeVec = MemoryMarshal.Cast<byte, Vector256<uint>>(userData.Slice(userData.Length - Unsafe.SizeOf<Stripe>()));

			/* last partial block */
			for (int i = 0; i < VEC256_PER_STRIPE; i++)
			{
				var acc = accVec[i];
				for (int j = 0; j < remainingStripes; j++)
				{
					var key = Unsafe.As<KeyPair, Vector256<uint>>(ref keys[i * (Unsafe.SizeOf<Vector256<uint>>() / Unsafe.SizeOf<KeyPair>()) + j]);
					var data = j == remainingStripes - 1 ? lastStripeVec[i] : dataVec[i + j * VEC256_PER_STRIPE];
					var dk = Avx2.Add(data, key);
					var shuff = Avx2.Shuffle(dk, 0x31);
					Vector256<ulong> res = Avx2.Multiply(dk, shuff);
					Vector256<ulong> add = Avx2.Add(data.AsUInt64(), acc);
					acc = Avx2.Add(res, add);
				}
				accVec[i] = acc;
			}

			return remainingStripes;
		}
	}
}


#endif
