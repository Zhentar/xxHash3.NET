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
		[StructLayout(LayoutKind.Sequential)]
		private readonly struct Vec256Pair<T> where T : unmanaged
		{
			public readonly Vector256<T> A;
			public readonly Vector256<T> B;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct MutableVec256Pair
		{
			public Vector256<ulong> A;
			public Vector256<ulong> B;
		}

		private static void LongSequenceHash_AVX2(ref OctoAccumulator accumulator, ReadOnlySpan<byte> userData)
		{
			var unprocessedData = userData;
			var blocks = unprocessedData.PopAll<StripeBlock<Vec256Pair<uint>>>();
			ref var acc = ref Safeish.AsMut<OctoAccumulator, MutableVec256Pair>(ref accumulator);

			ProcessFullStripeBlocks_AVX2(blocks, ref acc);

			if (unprocessedData.Length == 0) { goto Exit; /*Goto dedupes function epilog*/ }

			var dataVec = MemoryMarshal.Cast<byte, Vec256Pair<uint>>(unprocessedData);
			var keysSpan = Safeish.AsSpan<UnshingledKeys<OctoKey>, Vec256Pair<uint>>(Keys);

			var accA = acc.A;
			var accB = acc.B;
			int j = 0;
			for (; j < dataVec.Length; j++)
			{
				accA = ProcessStripePiece_AVX2(keysSpan[j].A, accA, dataVec[j].A);
				accB = ProcessStripePiece_AVX2(keysSpan[j].B, accB, dataVec[j].B);
			}
			if ((userData.Length & (Unsafe.SizeOf<Stripe>() - 1)) != 0)
			{
				ref readonly var lastStripeVec = ref userData.Last<Vec256Pair<uint>>();
				accA = ProcessStripePiece_AVX2(keysSpan[j].A, accA, lastStripeVec.A);
				accB = ProcessStripePiece_AVX2(keysSpan[j].B, accB, lastStripeVec.B);
			}
			acc.A = accA;
			acc.B = accB;

		Exit:
			return;
		}

		//Splitting this out for convenience while optimizing
		private static void ProcessFullStripeBlocks_AVX2(ReadOnlySpan<StripeBlock<Vec256Pair<uint>>> blocks,ref MutableVec256Pair acc)
		{
			ref readonly var keys2 = ref Safeish.As<UnshingledKeys<OctoKey>, UnshingledKeys<Vec256Pair<uint>>>(Keys);
			var accA = acc.A;
			var accB = acc.B;


			//There are 16 AVX registers. The generated code only needs five of them. So caching a bunch of the loads
			//here makes use of those registers effectively for free, and reduces the odds of paying penalties
			//for cache-line crossing loads in the main loop.
			var K00B04A = keys2.K00.B;
			var K01B05A = keys2.K01.B;
			var K02B06A = keys2.K02.B;
			var K03B07A = keys2.K03.B;
			var K04B08A = keys2.K04.B;
			var K05B09A = keys2.K05.B;
			var K07B11A = keys2.K07.B;
			var K09B13A = keys2.K09.B;
			var K10B14A = keys2.K10.B;
			var K11B15A = keys2.K11.B;
			var K12B16A = keys2.K12.B;


			for (int i = 0; i < blocks.Length; i++)
			{
				ref readonly var block = ref blocks[i];

				accA = ProcessStripePiece_AVX2(keys2.K00.A, accA, block.S00.A);
				accB = ProcessStripePiece_AVX2(K00B04A, accB, block.S00.B);
				accA = ProcessStripePiece_AVX2(keys2.K01.A, accA, block.S01.A);
				accB = ProcessStripePiece_AVX2(K01B05A, accB, block.S01.B);
				accA = ProcessStripePiece_AVX2(keys2.K02.A, accA, block.S02.A);
				accB = ProcessStripePiece_AVX2(K02B06A, accB, block.S02.B);
				accA = ProcessStripePiece_AVX2(keys2.K03.A, accA, block.S03.A);
				accB = ProcessStripePiece_AVX2(K03B07A, accB, block.S03.B);
				accA = ProcessStripePiece_AVX2(K00B04A, accA, block.S04.A);
				accB = ProcessStripePiece_AVX2(K04B08A, accB, block.S04.B);
				accA = ProcessStripePiece_AVX2(K01B05A, accA, block.S05.A);
				accB = ProcessStripePiece_AVX2(K05B09A, accB, block.S05.B);
				accA = ProcessStripePiece_AVX2(K02B06A, accA, block.S06.A);
				accB = ProcessStripePiece_AVX2(keys2.K06.B, accB, block.S06.B);
				accA = ProcessStripePiece_AVX2(K03B07A, accA, block.S07.A);
				accB = ProcessStripePiece_AVX2(K07B11A, accB, block.S07.B);
				accA = ProcessStripePiece_AVX2(K04B08A, accA, block.S08.A);
				accB = ProcessStripePiece_AVX2(keys2.K08.B, accB, block.S08.B);
				accA = ProcessStripePiece_AVX2(K05B09A, accA, block.S09.A);
				accB = ProcessStripePiece_AVX2(K09B13A, accB, block.S09.B);
				accA = ProcessStripePiece_AVX2(keys2.K10.A, accA, block.S10.A);
				accB = ProcessStripePiece_AVX2(K10B14A, accB, block.S10.B);
				accA = ProcessStripePiece_AVX2(K07B11A, accA, block.S11.A);
				accB = ProcessStripePiece_AVX2(K11B15A, accB, block.S11.B);
				accA = ProcessStripePiece_AVX2(keys2.K12.A, accA, block.S12.A);
				accB = ProcessStripePiece_AVX2(K12B16A, accB, block.S12.B);
				accA = ProcessStripePiece_AVX2(K09B13A, accA, block.S13.A);
				accB = ProcessStripePiece_AVX2(keys2.K13.B, accB, block.S13.B);
				accA = ProcessStripePiece_AVX2(K10B14A, accA, block.S14.A);
				accB = ProcessStripePiece_AVX2(keys2.K14.B, accB, block.S14.B);
				accA = ProcessStripePiece_AVX2(K11B15A, accA, block.S15.A);
				accB = ProcessStripePiece_AVX2(keys2.K15.B, accB, block.S15.B);
				accA = ScrambleAccumulators_AVX2(accA, K12B16A);
				accB = ScrambleAccumulators_AVX2(accB, keys2.Scramble.B);
			}

			acc.A = accA;
			acc.B = accB;
		}


		//Test in for key once https://github.com/dotnet/coreclr/pull/22944 merges
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector256<ulong> ProcessStripePiece_AVX2(/*in*/ Vector256<uint> key, Vector256<ulong> acc, Vector256<uint> data)
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
