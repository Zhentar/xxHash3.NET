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
		private struct Vec256Pair<T> where T : struct
		{
			public Vector256<T> A;
			public Vector256<T> B;
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct Keys_AVX2
		{
			[FieldOffset(0)]   public readonly Vec256Pair<uint> K00;
			[FieldOffset(8)]   public readonly Vec256Pair<uint> K01;
			[FieldOffset(16)]  public readonly Vec256Pair<uint> K02;
			[FieldOffset(24)]  public readonly Vec256Pair<uint> K03;
			[FieldOffset(32)]  public readonly Vec256Pair<uint> K04;
			[FieldOffset(40)]  public readonly Vec256Pair<uint> K05;
			[FieldOffset(48)]  public readonly Vec256Pair<uint> K06;
			[FieldOffset(56)]  public readonly Vec256Pair<uint> K07;
			[FieldOffset(64)]  public readonly Vec256Pair<uint> K08;
			[FieldOffset(72)]  public readonly Vec256Pair<uint> K09;
			[FieldOffset(80)]  public readonly Vec256Pair<uint> K10;
			[FieldOffset(88)]  public readonly Vec256Pair<uint> K11;
			[FieldOffset(96)]  public readonly Vec256Pair<uint> K12;
			[FieldOffset(104)] public readonly Vec256Pair<uint> K13;
			[FieldOffset(112)] public readonly Vec256Pair<uint> K14;
			[FieldOffset(120)] public readonly Vec256Pair<uint> K15;
			[FieldOffset(128)] public readonly Vec256Pair<uint> Scramble;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct StripeBlock_AVX2
		{
			public readonly Vec256Pair<uint> S00;
			public readonly Vec256Pair<uint> S01;
			public readonly Vec256Pair<uint> S02;
			public readonly Vec256Pair<uint> S03;
			public readonly Vec256Pair<uint> S04;
			public readonly Vec256Pair<uint> S05;
			public readonly Vec256Pair<uint> S06;
			public readonly Vec256Pair<uint> S07;
			public readonly Vec256Pair<uint> S08;
			public readonly Vec256Pair<uint> S09;
			public readonly Vec256Pair<uint> S10;
			public readonly Vec256Pair<uint> S11;
			public readonly Vec256Pair<uint> S12;
			public readonly Vec256Pair<uint> S13;
			public readonly Vec256Pair<uint> S14;
			public readonly Vec256Pair<uint> S15;
		}


		private static int AccumulateStripes_AVX2(in ReadOnlySpan<byte> userData, in Span<ulong> accumulators)
		{
			var keys = MemoryMarshal.Cast<uint, KeyPair>(kKey);
			const int VEC256_PER_STRIPE = STRIPE_BYTES / 32;
			ref var keys2 = ref MemoryMarshal.AsRef<Keys_AVX2>(MemoryMarshal.Cast<uint, byte>(kKey));

			ref var accPair = ref MemoryMarshal.AsRef<Vec256Pair<ulong>>(MemoryMarshal.AsBytes(accumulators));

			var accVec = MemoryMarshal.Cast<ulong, Vector256<ulong>>(accumulators);
			var accA = accVec[0];
			var accB = accVec[1];
			var scramKeyA = keys2.Scramble.A;
			var scramKeyB = keys2.Scramble.B;

			var unprocessedData = userData;
			var blocks = unprocessedData.PopAll<StripeBlock_AVX2>();
			for (int i = 0; i < blocks.Length; i++)
			{
				ref readonly var block = ref blocks[i];

				accA = ProcessStripePiece_AVX2(keys2.K00.A, accA, block.S00.A);
				accB = ProcessStripePiece_AVX2(keys2.K00.B, accB, block.S00.B);
				accA = ProcessStripePiece_AVX2(keys2.K01.A, accA, block.S01.A);
				accB = ProcessStripePiece_AVX2(keys2.K01.B, accB, block.S01.B);
				accA = ProcessStripePiece_AVX2(keys2.K02.A, accA, block.S02.A);
				accB = ProcessStripePiece_AVX2(keys2.K02.B, accB, block.S02.B);
				accA = ProcessStripePiece_AVX2(keys2.K03.A, accA, block.S03.A);
				accB = ProcessStripePiece_AVX2(keys2.K03.B, accB, block.S03.B);
				accA = ProcessStripePiece_AVX2(keys2.K04.A, accA, block.S04.A);
				accB = ProcessStripePiece_AVX2(keys2.K04.B, accB, block.S04.B);
				accA = ProcessStripePiece_AVX2(keys2.K05.A, accA, block.S05.A);
				accB = ProcessStripePiece_AVX2(keys2.K05.B, accB, block.S05.B);
				accA = ProcessStripePiece_AVX2(keys2.K06.A, accA, block.S06.A);
				accB = ProcessStripePiece_AVX2(keys2.K06.B, accB, block.S06.B);
				accA = ProcessStripePiece_AVX2(keys2.K07.A, accA, block.S07.A);
				accB = ProcessStripePiece_AVX2(keys2.K07.B, accB, block.S07.B);
				accA = ProcessStripePiece_AVX2(keys2.K08.A, accA, block.S08.A);
				accB = ProcessStripePiece_AVX2(keys2.K08.B, accB, block.S08.B);
				accA = ProcessStripePiece_AVX2(keys2.K09.A, accA, block.S09.A);
				accB = ProcessStripePiece_AVX2(keys2.K09.B, accB, block.S09.B);
				accA = ProcessStripePiece_AVX2(keys2.K10.A, accA, block.S10.A);
				accB = ProcessStripePiece_AVX2(keys2.K10.B, accB, block.S10.B);
				accA = ProcessStripePiece_AVX2(keys2.K11.A, accA, block.S11.A);
				accB = ProcessStripePiece_AVX2(keys2.K11.B, accB, block.S11.B);
				accA = ProcessStripePiece_AVX2(keys2.K12.A, accA, block.S12.A);
				accB = ProcessStripePiece_AVX2(keys2.K12.B, accB, block.S12.B);
				accA = ProcessStripePiece_AVX2(keys2.K13.A, accA, block.S13.A);
				accB = ProcessStripePiece_AVX2(keys2.K13.B, accB, block.S13.B);
				accA = ProcessStripePiece_AVX2(keys2.K14.A, accA, block.S14.A);
				accB = ProcessStripePiece_AVX2(keys2.K14.B, accB, block.S14.B);
				accA = ProcessStripePiece_AVX2(keys2.K15.A, accA, block.S15.A);
				accB = ProcessStripePiece_AVX2(keys2.K15.B, accB, block.S15.B);
				accA = ScrambleAccumulators_AVX2(accA, scramKeyA);
				accB = ScrambleAccumulators_AVX2(accB, scramKeyB);
			}


			accVec[0] = accA;
			accVec[1] = accB;

			var dataVec = MemoryMarshal.Cast<byte, Vector256<uint>>(unprocessedData);
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

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<ulong> ProcessStripePiece_AVX2(Vector256<uint> key, Vector256<ulong> acc, Vector256<uint> data)
		{
			var dk = Avx2.Add(data, key);
			var shuff = Avx2.Shuffle(dk, 0x31);
			var res = Avx2.Multiply(dk, shuff);
			var add = Avx2.Add(data.AsUInt64(), acc);
			return Avx2.Add(res, add);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<ulong> ScrambleAccumulators_AVX2(Vector256<ulong> acc, Vector256<uint> key)
		{
			var shifted = Avx2.ShiftRightLogical(acc, 47);
			acc = Avx2.Xor(acc, shifted);
			var accKey = Avx2.Multiply(acc.AsUInt32(), key);
			var dataShuff = Avx2.Shuffle(acc.AsUInt32(), 0x31);
			var keyShuff = Avx2.Shuffle(key, 0x31);
			var dk2 = Avx2.Multiply(dataShuff, keyShuff);
			return Avx2.Xor(accKey, dk2);
		}

	}
}


#endif
