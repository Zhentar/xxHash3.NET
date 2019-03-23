using System;
using System.Runtime.InteropServices;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;

namespace xxHash3
{
#pragma warning disable IDE1006 // Naming Styles
	public partial class xxHash3
#pragma warning restore IDE1006 // Naming Styles
	{
		private const ulong PRIME64_1 = 11400714785074694791UL;
		private const ulong PRIME64_2 = 14029467366897019727UL;
		private const ulong PRIME64_3 = 1609587929392839161UL;
		private const ulong PRIME64_4 = 9650029242287828579UL;
		private const ulong PRIME64_5 = 2870177450012600261UL;

		static readonly uint[] kKey = {
			kKey_1_left,kKey_1_right,0x7c01812c,0xf721ad1c,
			0xded46de9,0x839097db,0x7240a4a4,0xb7b3671f,
			0xcb79e64e,0xccc0e578,0x825ad07d,0xccff7221,
			0xb8084674,0xf743248e,0xe03590e6,0x813a264c,
			0x3c2852bb,0x91c300cb,0x88d0658b,0x1b532ea3,
			0x71644897,0xa20df94e,0x3819ef46,0xa9deacd8,
			0xa8fa763f,0xe39c343f,0xf9dcbbc7,0xc70b4f1d,
			0x8a51e04b,0xcdb45931,0xc89f7ec9,0xd9787364,
			0xeac5ac83,0x34d3ebc3,0xc581a0ff,0xfa1363eb,
			0x170ddd51,0xb7f0da49,0xd3165526,0x29d4689e,
			0x2b16be58,0x7d47a1fc,0x8ff8b8d1,0x7ad031ce,
			0x45cb3a8f,0x95160428,0xafd7fbca,0xbb4b407e,
		};

		private static readonly UnshingledKeys<OctoKey> Keys;

		const uint kKey_1_left = 0xb8fe6c39;
		const uint kKey_1_right = 0x23a44bbe;
		const ulong kKey_1 = 0x23a44bbe_b8fe6c39;
		const ulong kKey_2 = 0xf721ad1c_7c01812c;

		static xxHash3()
		{
			var keys = MemoryMarshal.Cast<uint, KeyPair>(kKey);

			Span<OctoKey> unshingledKeys = stackalloc OctoKey[17];

			for (int i = 0; i < unshingledKeys.Length; i++)
			{
				unshingledKeys[i] = MemoryMarshal.Cast<KeyPair, OctoKey>(keys)[0];
				keys = keys.Slice(1);
			}
			Keys = MemoryMarshal.Cast<OctoKey, UnshingledKeys<OctoKey>>(unshingledKeys)[0];
		}

		private struct UnshingledKeys<T>
		{
			public readonly T K00;
			public readonly T K01;
			public readonly T K02;
			public readonly T K03;
			public readonly T K04;
			public readonly T K05;
			public readonly T K06;
			public readonly T K07;
			public readonly T K08;
			public readonly T K09;
			public readonly T K10;
			public readonly T K11;
			public readonly T K12;
			public readonly T K13;
			public readonly T K14;
			public readonly T K15;
			public readonly T Scramble;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct Stripe
		{
			//Unfortunately, fixed can't be used with user defined structs, so we get this instead...
			public readonly UintPair A;
			public readonly UintPair B;
			public readonly UintPair C;
			public readonly UintPair D;
			public readonly UintPair E;
			public readonly UintPair F;
			public readonly UintPair G;
			public readonly UintPair H;
		}

		[StructLayout(LayoutKind.Explicit)]
		private readonly struct UintPair
		{
			[FieldOffset(0)]
			private readonly uint _left;
			[FieldOffset(4)]
			private readonly uint _right;
			[FieldOffset(0)]
			private readonly ulong _value64;
			public uint Left => _left.AsLittleEndian();
			public uint Right => _right.AsLittleEndian();

			public ulong Value64
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => BitConverter.IsLittleEndian ? _value64 : Left + ((ulong)Right << 32);
			}
		}

		[StructLayout(LayoutKind.Sequential)]
		private readonly struct UserDataUlongPair
		{
			private readonly ulong _left;
			private readonly ulong _right;
			public ulong Left => _left.AsLittleEndian();
			public ulong Right => _right.AsLittleEndian();
		}


		[StructLayout(LayoutKind.Explicit)]
		private readonly struct KeyPair
		{
			[FieldOffset(0)]
			public readonly uint Left;
			[FieldOffset(4)]
			public readonly uint Right;
			[FieldOffset(0)]
			private readonly ulong _key64;

			public ulong Key64
			{
				[MethodImpl(MethodImplOptions.AggressiveInlining)]
				get => BitConverter.IsLittleEndian ? _key64 : Left + ((ulong)Right << 32);
			}

			public KeyPair(uint left, uint right) => (_key64, Left, Right) = (0ul, left, right);
		}
		
		[StructLayout(LayoutKind.Sequential)]
		private struct OctoKey
		{
			public readonly KeyPair A;
			public readonly KeyPair B;
			public readonly KeyPair C;
			public readonly KeyPair D;
			public readonly KeyPair E;
			public readonly KeyPair F;
			public readonly KeyPair G;
			public readonly KeyPair H;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct OctoAccumulator
		{
			public ulong A;
			public ulong B;
			public ulong C;
			public ulong D;
			public ulong E;
			public ulong F;
			public ulong G;
			public ulong H;
		}


		[StructLayout(LayoutKind.Sequential)]
		private struct StripeBlock<T> where T : struct
		{
			public readonly T S00;
			public readonly T S01;
			public readonly T S02;
			public readonly T S03;
			public readonly T S04;
			public readonly T S05;
			public readonly T S06;
			public readonly T S07;
			public readonly T S08;
			public readonly T S09;
			public readonly T S10;
			public readonly T S11;
			public readonly T S12;
			public readonly T S13;
			public readonly T S14;
			public readonly T S15;
		}


		public static ulong Hash64(ReadOnlySpan<byte> data, ulong seed = 0)
		{
			if (data.Length <= 16) { return ZeroToSixteenBytes(data, seed); }
			if (data.Length > 128) { return HashLongSequence64(data, seed); }
			return SixteenToOneTwentyEight(data, seed);
		}

		private static ulong SixteenToOneTwentyEight(ReadOnlySpan<byte> data, ulong seed)
		{
			ref readonly var key = ref Keys.K00;
			ulong acc = PRIME64_1 * ((uint)data.Length + seed);

			var fromTheFront = MemoryMarshal.Cast<byte, UserDataUlongPair>(data);
			var fromTheBack = MemoryMarshal.Cast<byte, UserDataUlongPair>(data.Slice(data.Length & 0xF));

			if (fromTheFront.Length > 4)
			{
				ref readonly var key2 = ref Keys.K01;
				if (fromTheFront.Length > 6)
				{
					acc += MixSixteenBytes(fromTheFront[3], key2.E, key2.F);
					acc += MixSixteenBytes(fromTheBack[fromTheBack.Length - 4], key2.G, key2.H);
				}
				acc += MixSixteenBytes(fromTheFront[2], key2.A, key2.B);
				acc += MixSixteenBytes(fromTheBack[fromTheBack.Length - 3], key2.C, key2.D);
			}

			if (fromTheFront.Length > 2)
			{
				acc += MixSixteenBytes(fromTheFront[1], key.E, key.F);
				acc += MixSixteenBytes(fromTheBack[fromTheBack.Length - 2], key.G, key.H);
			}

			acc += MixSixteenBytes(fromTheFront[0], key.A, key.B);
			acc += MixSixteenBytes(fromTheBack[fromTheBack.Length - 1], key.C, key.D);

			return Avalanche(acc);
		}


		private static ulong ZeroToSixteenBytes(ReadOnlySpan<byte> data, ulong seed)
		{
			if (data.Length > 8) return NineToSixteenBytes(data, seed);
			if (data.Length >= 4) return FourToEightBytes(data, seed);
			if (data.Length > 0) return OneToThreeBytes(data, seed);
			return seed;
		}

		private static ulong OneToThreeBytes(ReadOnlySpan<byte> data, ulong seed)
		{
			byte c1 = data[0];
			byte c2 = data[data.Length >> 1];
			byte c3 = data[data.Length - 1];
			uint l1 = c1 + (uint)(c2 << 8);
			uint l2 = (uint)data.Length + (uint)(c3 << 2);
			ulong ll11 = Multiply32to64(l1 + (uint)seed + kKey_1_left, l2 + kKey_1_right);
			return Avalanche(ll11);
		}

		private static ulong FourToEightBytes(ReadOnlySpan<byte> data, ulong seed)
		{
			ulong acc = PRIME64_1 * ((uint)data.Length + seed);
			uint l1 = BinaryPrimitives.ReadUInt32LittleEndian(data) + kKey_1_left;
			uint l2 = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(data.Length - 4)) + kKey_1_right;
			acc += Multiply32to64(l1, l2);
			return Avalanche(acc);
		}

		private static ulong NineToSixteenBytes(ReadOnlySpan<byte> data, ulong seed)
		{
			ulong acc = PRIME64_1 * ((uint)data.Length + seed);
			ulong ll1 = BinaryPrimitives.ReadUInt64LittleEndian(data) + kKey_1;
			ulong ll2 = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(data.Length - 8)) + kKey_2;
			acc += MultiplyAdd64(ll1, ll2);
			return Avalanche(acc);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong Avalanche(ulong h64)
		{
			h64 ^= h64 >> 29;
			h64 *= PRIME64_3;
			h64 ^= h64 >> 32;
			return h64;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong MixSixteenBytes(UserDataUlongPair data, KeyPair left, KeyPair right)
		{
			return MultiplyAdd64(data.Left ^ left.Key64, data.Right ^ right.Key64);
		}

		private static void AccumulateStripe(ref OctoAccumulator acc, in Stripe data, in OctoKey theKeys)
		{
			//Hand unrolled...
			acc.A = AccumulateOnePair(acc.A, data.A, theKeys.A);
			acc.B = AccumulateOnePair(acc.B, data.B, theKeys.B);
			acc.C = AccumulateOnePair(acc.C, data.C, theKeys.C);
			acc.D = AccumulateOnePair(acc.D, data.D, theKeys.D);
			acc.E = AccumulateOnePair(acc.E, data.E, theKeys.E);
			acc.F = AccumulateOnePair(acc.F, data.F, theKeys.F);
			acc.G = AccumulateOnePair(acc.G, data.G, theKeys.G);
			acc.H = AccumulateOnePair(acc.H, data.H, theKeys.H);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static ulong AccumulateOnePair(ulong acc, UintPair value, KeyPair key)
		{
			var dataLeft = value.Left;
			var dataRight = value.Right;
			var mul = Multiply32to64(dataLeft + key.Left, dataRight + key.Right);
			acc += mul + dataLeft + ((ulong)dataRight << 32);
			return acc;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ScrambleAccumulators_Scalar(ref OctoAccumulator accumulator)
		{
			var keys = Safeish.AsSpan<OctoKey, KeyPair>(Keys.Scramble);
			var acc = Safeish.AsMutableSpan<OctoAccumulator, ulong>(ref accumulator);
			for (int i = 0; i < acc.Length; i++)
			{
				acc[i] ^= acc[i] >> 47;
				ulong p1 = Multiply32to64((uint)acc[i], keys[i].Left);
				ulong p2 = Multiply32to64((uint)(acc[i] >> 32), keys[i].Right);
				acc[i] = p1 ^ p2;
			}
		}

		private static void AccumulateStripeBlocks_Scalar(ref OctoAccumulator acc, ReadOnlySpan<StripeBlock<Stripe>> blocks)
		{
			ref readonly var keys = ref Keys;

			for (int i = 0; i < blocks.Length; i++)
			{
				ref readonly var stripes = ref blocks[i];
				AccumulateStripe(ref acc, stripes.S00, keys.K00);
				AccumulateStripe(ref acc, stripes.S01, keys.K01);
				AccumulateStripe(ref acc, stripes.S02, keys.K02);
				AccumulateStripe(ref acc, stripes.S03, keys.K03);
				AccumulateStripe(ref acc, stripes.S04, keys.K04);
				AccumulateStripe(ref acc, stripes.S05, keys.K05);
				AccumulateStripe(ref acc, stripes.S06, keys.K06);
				AccumulateStripe(ref acc, stripes.S07, keys.K07);
				AccumulateStripe(ref acc, stripes.S08, keys.K08);
				AccumulateStripe(ref acc, stripes.S09, keys.K09);
				AccumulateStripe(ref acc, stripes.S10, keys.K10);
				AccumulateStripe(ref acc, stripes.S11, keys.K11);
				AccumulateStripe(ref acc, stripes.S12, keys.K12);
				AccumulateStripe(ref acc, stripes.S13, keys.K13);
				AccumulateStripe(ref acc, stripes.S14, keys.K14);
				AccumulateStripe(ref acc, stripes.S15, keys.K15);
				ScrambleAccumulators_Scalar(ref acc);
			}

		}

		public static bool UseAvx2;
		public static bool UseSse2;

		private static void LongSequenceHash_Scalar(ref OctoAccumulator acc, ReadOnlySpan<byte> data)
		{
			var unprocessedData = data;
			ReadOnlySpan<StripeBlock<Stripe>> blocks = unprocessedData.PopAll<StripeBlock<Stripe>>();

			AccumulateStripeBlocks_Scalar(ref acc, blocks);

			if (unprocessedData.Length == 0) { goto Exit; /*Goto dedupes function epilog*/ }

			var keysSpan = Safeish.AsSpan<UnshingledKeys<OctoKey>, OctoKey>(Keys);
			var stripes = MemoryMarshal.Cast<byte, Stripe>(unprocessedData);
				

			/* last partial block */
			for (int i = 0; i < stripes.Length; i++)
			{
				AccumulateStripe(ref acc, stripes[i], keysSpan[stripes.Length]);
			}
			/* last stripe */
			if ((data.Length & (Unsafe.SizeOf<Stripe>() - 1)) != 0)
			{
				ref readonly Stripe stripe = ref data.Last<Stripe>();
				AccumulateStripe(ref acc, stripe , keysSpan[stripes.Length]);
			}
		Exit:
			return;
		}

		private static ulong HashLongSequence64(ReadOnlySpan<byte> data, ulong seed)
		{
			var acc2 = new OctoAccumulator
			{
				A = seed,
				B = PRIME64_1,
				C = PRIME64_2,
				D = PRIME64_3,
				E = PRIME64_4,
				F = PRIME64_5,
				G = seed,
				H = 0
			};


#if NETCOREAPP3_0
			if (System.Runtime.Intrinsics.X86.Avx2.IsSupported && UseAvx2)
			{
				LongSequenceHash_AVX2(ref acc2, data);
			}
			else if (System.Runtime.Intrinsics.X86.Sse2.IsSupported && UseSse2)
			{
				LongSequenceHash_SSE2(ref acc2, data);
			}
			else
#endif
			{
				LongSequenceHash_Scalar(ref acc2, data);
			}
			/* converge into final hash */
			ref readonly var key = ref Keys.K00;
			ulong result64 = (ulong)data.Length * PRIME64_1;
			result64 += MultiplyAdd64(acc2.A ^ key.A.Key64, acc2.B ^ key.B.Key64);
			result64 += MultiplyAdd64(acc2.C ^ key.C.Key64, acc2.D ^ key.D.Key64);
			result64 += MultiplyAdd64(acc2.E ^ key.E.Key64, acc2.F ^ key.F.Key64);
			result64 += MultiplyAdd64(acc2.G ^ key.G.Key64, acc2.H ^ key.H.Key64);
			return Avalanche(result64);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe ulong MultiplyAdd64(ulong lhs, ulong rhs)
		{
#if NETCOREAPP3_0
			if(System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported)
			{
				ulong lowHalf;
				ulong highHalf = System.Runtime.Intrinsics.X86.Bmi2.X64.MultiplyNoFlags(lhs, rhs, &lowHalf);
				return lowHalf + highHalf;
			}
#endif
			return MultiplyAdd64Slow(lhs, rhs);
		}

		private static unsafe ulong MultiplyAdd64Slow(ulong lhs, ulong rhs)
		{
			/* emulate 64x64->128b multiplication, using four 32x32->64 */
			uint lhsHigh = (uint)(lhs >> 32);
			uint rhsHigh = (uint)(rhs >> 32);
			uint lhsLow = (uint)lhs;
			uint rhsLow = (uint)rhs;

			ulong high = Multiply32to64(lhsHigh, rhsHigh);
			ulong middleOne = Multiply32to64(lhsLow, rhsHigh);
			ulong middleTwo = Multiply32to64(lhsHigh, rhsLow);
			ulong low = Multiply32to64(lhsLow, rhsLow);

			ulong t = low + (middleOne << 32);
			ulong carry1 = t < low ? 1u : 0u;

			low = t + (middleTwo << 32);
			ulong carry2 = low < t ? 1u : 0u;
			high = high + (middleOne >> 32) + (middleTwo >> 32) + carry1 + carry2;

			return high + low;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static unsafe ulong Multiply32to64(uint a, uint b)
		{
//TODO: test if this helps on 32 bit processes
//#if NETCOREAPP3_0
//			if (Bmi2.IsSupported && !Environment.Is64BitProcess)
//			{
//				uint low = 0;
//				uint high = Bmi2.MultiplyNoFlags(a, b, &low);
//				return low + ((ulong)high << 32);
//			}
//#endif
			return a * (ulong)b;
		}
	}
}
