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
		private readonly struct Vec128Quad
		{
			public readonly Vector128<uint> A;
			public readonly Vector128<uint> B;
			public readonly Vector128<uint> C;
			public readonly Vector128<uint> D;
		}

		private struct MutableVec128Quad
		{
			public Vector128<ulong> A;
			public Vector128<ulong> B;
			public Vector128<ulong> C;
			public Vector128<ulong> D;
		}

		private static void LongSequenceHash_SSE2(ref OctoAccumulator accumulator, ReadOnlySpan<byte> userData)
		{
			var unprocessedData = userData;
			var blocks = unprocessedData.PopAll<StripeBlock<Vec128Quad>>();
			ref var acc = ref Safeish.AsMut<OctoAccumulator, MutableVec128Quad>(ref accumulator);

			ProcessFullStripeBlocks_SSE2(blocks, ref acc);

			if (unprocessedData.Length == 0) { goto Exit; /*Goto dedupes function epilog*/ }

			var dataVec = MemoryMarshal.Cast<byte, Vec128Quad>(unprocessedData);
			var keysSpan = Safeish.AsSpan<UnshingledKeys<OctoKey>, Vec128Quad>(Keys);

			var accA = acc.A;
			var accB = acc.B;
			var accC = acc.C;
			var accD = acc.D;
			int j = 0;
			for (; j < dataVec.Length; j++)
			{
				accA = ProcessStripePiece_SSE2(keysSpan[j].A, accA, dataVec[j].A);
				accB = ProcessStripePiece_SSE2(keysSpan[j].B, accB, dataVec[j].B);
				accC = ProcessStripePiece_SSE2(keysSpan[j].C, accA, dataVec[j].C);
				accD = ProcessStripePiece_SSE2(keysSpan[j].D, accB, dataVec[j].D);
			}
			if ((userData.Length & (Unsafe.SizeOf<Stripe>() - 1)) != 0)
			{
				ref readonly var lastStripeVec = ref userData.Last<Vec128Quad>();
				accA = ProcessStripePiece_SSE2(keysSpan[j].A, accA, lastStripeVec.A);
				accB = ProcessStripePiece_SSE2(keysSpan[j].B, accB, lastStripeVec.B);
				accC = ProcessStripePiece_SSE2(keysSpan[j].C, accA, lastStripeVec.C);
				accD = ProcessStripePiece_SSE2(keysSpan[j].D, accB, lastStripeVec.D);
			}

			acc.A = accA;
			acc.B = accB;
			acc.C = accC;
			acc.D = accD;

		Exit:
			return;
		}

		//Powershell "template" code for this:
		//@("A","B","C","D") | % { Set-Variable -Name "letter" -Value $_  -PassThru } | % { 0..15 | % { 'acc{1} = ProcessStripePiece_SSE2(keys.K{0:D2}.{1}, acc{1}, block.S{0:D2}.{1});' -f ( $_, $letter) } }
		private static void ProcessFullStripeBlocks_SSE2(ReadOnlySpan<StripeBlock<Vec128Quad>> blocks, ref MutableVec128Quad acc)
		{
			ref readonly var keys = ref Safeish.As<UnshingledKeys<OctoKey>, UnshingledKeys<Vec128Quad>>(Keys);
			var accA = acc.A;
			var accB = acc.B;
			var accC = acc.C;
			var accD = acc.D;

			for (int i = 0; i < blocks.Length; i++)
			{
				ref readonly var block = ref blocks[i];

				accA = ProcessStripePiece_SSE2(keys.K00.A, accA, block.S00.A);
				accA = ProcessStripePiece_SSE2(keys.K01.A, accA, block.S01.A);
				accA = ProcessStripePiece_SSE2(keys.K02.A, accA, block.S02.A);
				accA = ProcessStripePiece_SSE2(keys.K03.A, accA, block.S03.A);
				accA = ProcessStripePiece_SSE2(keys.K04.A, accA, block.S04.A);
				accA = ProcessStripePiece_SSE2(keys.K05.A, accA, block.S05.A);
				accA = ProcessStripePiece_SSE2(keys.K06.A, accA, block.S06.A);
				accA = ProcessStripePiece_SSE2(keys.K07.A, accA, block.S07.A);
				accA = ProcessStripePiece_SSE2(keys.K08.A, accA, block.S08.A);
				accA = ProcessStripePiece_SSE2(keys.K09.A, accA, block.S09.A);
				accA = ProcessStripePiece_SSE2(keys.K10.A, accA, block.S10.A);
				accA = ProcessStripePiece_SSE2(keys.K11.A, accA, block.S11.A);
				accA = ProcessStripePiece_SSE2(keys.K12.A, accA, block.S12.A);
				accA = ProcessStripePiece_SSE2(keys.K13.A, accA, block.S13.A);
				accA = ProcessStripePiece_SSE2(keys.K14.A, accA, block.S14.A);
				accA = ProcessStripePiece_SSE2(keys.K15.A, accA, block.S15.A);
				accB = ProcessStripePiece_SSE2(keys.K00.B, accB, block.S00.B);
				accB = ProcessStripePiece_SSE2(keys.K01.B, accB, block.S01.B);
				accB = ProcessStripePiece_SSE2(keys.K02.B, accB, block.S02.B);
				accB = ProcessStripePiece_SSE2(keys.K03.B, accB, block.S03.B);
				accB = ProcessStripePiece_SSE2(keys.K04.B, accB, block.S04.B);
				accB = ProcessStripePiece_SSE2(keys.K05.B, accB, block.S05.B);
				accB = ProcessStripePiece_SSE2(keys.K06.B, accB, block.S06.B);
				accB = ProcessStripePiece_SSE2(keys.K07.B, accB, block.S07.B);
				accB = ProcessStripePiece_SSE2(keys.K08.B, accB, block.S08.B);
				accB = ProcessStripePiece_SSE2(keys.K09.B, accB, block.S09.B);
				accB = ProcessStripePiece_SSE2(keys.K10.B, accB, block.S10.B);
				accB = ProcessStripePiece_SSE2(keys.K11.B, accB, block.S11.B);
				accB = ProcessStripePiece_SSE2(keys.K12.B, accB, block.S12.B);
				accB = ProcessStripePiece_SSE2(keys.K13.B, accB, block.S13.B);
				accB = ProcessStripePiece_SSE2(keys.K14.B, accB, block.S14.B);
				accB = ProcessStripePiece_SSE2(keys.K15.B, accB, block.S15.B);
				accC = ProcessStripePiece_SSE2(keys.K00.C, accC, block.S00.C);
				accC = ProcessStripePiece_SSE2(keys.K01.C, accC, block.S01.C);
				accC = ProcessStripePiece_SSE2(keys.K02.C, accC, block.S02.C);
				accC = ProcessStripePiece_SSE2(keys.K03.C, accC, block.S03.C);
				accC = ProcessStripePiece_SSE2(keys.K04.C, accC, block.S04.C);
				accC = ProcessStripePiece_SSE2(keys.K05.C, accC, block.S05.C);
				accC = ProcessStripePiece_SSE2(keys.K06.C, accC, block.S06.C);
				accC = ProcessStripePiece_SSE2(keys.K07.C, accC, block.S07.C);
				accC = ProcessStripePiece_SSE2(keys.K08.C, accC, block.S08.C);
				accC = ProcessStripePiece_SSE2(keys.K09.C, accC, block.S09.C);
				accC = ProcessStripePiece_SSE2(keys.K10.C, accC, block.S10.C);
				accC = ProcessStripePiece_SSE2(keys.K11.C, accC, block.S11.C);
				accC = ProcessStripePiece_SSE2(keys.K12.C, accC, block.S12.C);
				accC = ProcessStripePiece_SSE2(keys.K13.C, accC, block.S13.C);
				accC = ProcessStripePiece_SSE2(keys.K14.C, accC, block.S14.C);
				accC = ProcessStripePiece_SSE2(keys.K15.C, accC, block.S15.C);
				accD = ProcessStripePiece_SSE2(keys.K00.D, accD, block.S00.D);
				accD = ProcessStripePiece_SSE2(keys.K01.D, accD, block.S01.D);
				accD = ProcessStripePiece_SSE2(keys.K02.D, accD, block.S02.D);
				accD = ProcessStripePiece_SSE2(keys.K03.D, accD, block.S03.D);
				accD = ProcessStripePiece_SSE2(keys.K04.D, accD, block.S04.D);
				accD = ProcessStripePiece_SSE2(keys.K05.D, accD, block.S05.D);
				accD = ProcessStripePiece_SSE2(keys.K06.D, accD, block.S06.D);
				accD = ProcessStripePiece_SSE2(keys.K07.D, accD, block.S07.D);
				accD = ProcessStripePiece_SSE2(keys.K08.D, accD, block.S08.D);
				accD = ProcessStripePiece_SSE2(keys.K09.D, accD, block.S09.D);
				accD = ProcessStripePiece_SSE2(keys.K10.D, accD, block.S10.D);
				accD = ProcessStripePiece_SSE2(keys.K11.D, accD, block.S11.D);
				accD = ProcessStripePiece_SSE2(keys.K12.D, accD, block.S12.D);
				accD = ProcessStripePiece_SSE2(keys.K13.D, accD, block.S13.D);
				accD = ProcessStripePiece_SSE2(keys.K14.D, accD, block.S14.D);
				accD = ProcessStripePiece_SSE2(keys.K15.D, accD, block.S15.D);
				accA = ScrambleAccumulators_SSE2(accA, keys.Scramble.A);
				accB = ScrambleAccumulators_SSE2(accB, keys.Scramble.B);
				accC = ScrambleAccumulators_SSE2(accC, keys.Scramble.C);
				accD = ScrambleAccumulators_SSE2(accD, keys.Scramble.D);
			}

			acc.A = accA;
			acc.B = accB;
			acc.C = accC;
			acc.D = accD;
		}

		//Test in for key once https://github.com/dotnet/coreclr/pull/22944 merges
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector128<ulong> ProcessStripePiece_SSE2(/*in*/ Vector128<uint> key, Vector128<ulong> acc, Vector128<uint> data)
		{	//Note: non-essential temp locals have been cut out here to avoid exceeding JIT locals limit and aborting inlining
			var dk = Sse2.Add(data, key);
			return Sse2.Add(Sse2.Multiply(dk, Sse2.Shuffle(dk, 0x31)), Sse2.Add(data.AsUInt64(), acc));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Vector128<ulong> ScrambleAccumulators_SSE2(Vector128<ulong> acc, Vector128<uint> key)
		{
			var shifted = Sse2.ShiftRightLogical(acc, 47);
			acc = Sse2.Xor(acc, shifted);
			var accKey = Sse2.Multiply(acc.AsUInt32(), key);
			var keyShuff = Sse2.Shuffle(key, 0x31);
			var dk2 = Sse2.Multiply(Sse2.Shuffle(acc.AsUInt32(), 0x31), keyShuff);
			return Sse2.Xor(accKey, dk2);
		}
	}
}


#endif
