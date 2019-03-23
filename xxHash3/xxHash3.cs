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
		private static readonly KeyPair64Quad Keys64_A;
		private static readonly KeyPair64Quad Keys64_B;

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

			Keys64_A = new KeyPair64Quad(Keys.K00);
			Keys64_B = new KeyPair64Quad(Keys.K01);
		}



		public static ulong Hash64(ReadOnlySpan<byte> data, ulong seed = 0)
		{
			if (data.Length <= 16) { return ZeroToSixteenBytes(data, seed); }
			if (data.Length > 128) { return HashLongSequence64(data, seed); }
			return SixteenToOneTwentyEight(data, seed);
		}

		private static ulong SixteenToOneTwentyEight(ReadOnlySpan<byte> data, ulong seed)
		{
			ref readonly var key = ref Keys64_A;
			ulong acc = PRIME64_1 * ((uint)data.Length + seed);

			var fromTheFront = MemoryMarshal.Cast<byte, UserDataUlongPair>(data);
			var fromTheBack = MemoryMarshal.Cast<byte, UserDataUlongPair>(data.Slice(data.Length & 0xF));

			if (fromTheFront.Length > 4)
			{
				ref readonly var key2 = ref Keys64_B;
				if (fromTheFront.Length > 6)
				{
					acc += MixSixteenBytes(fromTheFront[3], key2.C);
					acc += MixSixteenBytes(fromTheBack[fromTheBack.Length - 4], key2.D);
				}
				acc += MixSixteenBytes(fromTheFront[2], key2.A);
				acc += MixSixteenBytes(fromTheBack[fromTheBack.Length - 3], key2.B);
			}

			if (fromTheFront.Length > 2)
			{
				acc += MixSixteenBytes(fromTheFront[1], key.C);
				acc += MixSixteenBytes(fromTheBack[fromTheBack.Length - 2], key.D);
			}

			acc += MixSixteenBytes(fromTheFront[0], key.A);
			acc += MixSixteenBytes(fromTheBack[fromTheBack.Length - 1], key.B);

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
		private static ulong MixSixteenBytes(UserDataUlongPair data, KeyPair64 key)
		{
			return MultiplyAdd64(data.Left ^ key.Left, data.Right ^ key.Right);
		}

		private static void AccumulateStripe(ref OctoAccumulator acc, in Stripe data, in OctoKey theKeys)
		{
			//Hand unrolled...
			acc.A += AccumulateOnePair(data.A.Left, data.A.Right, theKeys.A.Left, theKeys.A.Right);
			acc.B += AccumulateOnePair(data.B.Left, data.B.Right, theKeys.B.Left, theKeys.B.Right);
			acc.C += AccumulateOnePair(data.C.Left, data.C.Right, theKeys.C.Left, theKeys.C.Right);
			acc.D += AccumulateOnePair(data.D.Left, data.D.Right, theKeys.D.Left, theKeys.D.Right);
			acc.E += AccumulateOnePair(data.E.Left, data.E.Right, theKeys.E.Left, theKeys.E.Right);
			acc.F += AccumulateOnePair(data.F.Left, data.F.Right, theKeys.F.Left, theKeys.F.Right);
			acc.G += AccumulateOnePair(data.G.Left, data.G.Right, theKeys.G.Left, theKeys.G.Right);
			acc.H += AccumulateOnePair(data.H.Left, data.H.Right, theKeys.H.Left, theKeys.H.Right);     
		}

		private static void AccumulateStripeBlocks_ScalarPiecewise(ref OctoAccumulator acc, ReadOnlySpan<StripeBlock<Stripe>> blocks)
		{
			ref readonly var keys = ref Keys;

			for (int i = 0; i < blocks.Length; i++)
			{
				ref readonly var stripes = ref blocks[i];
				AccumulateStripeBlock_ScalarPiecewise<AAccessor>(ref acc, stripes, keys);
				AccumulateStripeBlock_ScalarPiecewise<BAccessor>(ref acc, stripes, keys);
				AccumulateStripeBlock_ScalarPiecewise<CAccessor>(ref acc, stripes, keys);
				AccumulateStripeBlock_ScalarPiecewise<DAccessor>(ref acc, stripes, keys);
				AccumulateStripeBlock_ScalarPiecewise<EAccessor>(ref acc, stripes, keys);
				AccumulateStripeBlock_ScalarPiecewise<FAccessor>(ref acc, stripes, keys);
				AccumulateStripeBlock_ScalarPiecewise<GAccessor>(ref acc, stripes, keys);
				AccumulateStripeBlock_ScalarPiecewise<HAccessor>(ref acc, stripes, keys);
			}
		}

		private static void AccumulateStripeBlock_ScalarPiecewise<T>(ref OctoAccumulator accumulator, in StripeBlock<Stripe> stripes, in UnshingledKeys<OctoKey> keys) where T : IAccumulatorWiseAccessor
		{
			T accessor = default;
			ulong acc = accessor.Piece(accumulator);

			ref readonly var stripe = ref accessor.Piece(stripes.S00); ref readonly var key = ref accessor.Piece(keys.K00); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S01); key = ref accessor.Piece(keys.K01); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S02); key = ref accessor.Piece(keys.K02); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S03); key = ref accessor.Piece(keys.K03); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S04); key = ref accessor.Piece(keys.K04); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S05); key = ref accessor.Piece(keys.K05); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S06); key = ref accessor.Piece(keys.K06); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S07); key = ref accessor.Piece(keys.K07); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S08); key = ref accessor.Piece(keys.K08); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S09); key = ref accessor.Piece(keys.K09); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S10); key = ref accessor.Piece(keys.K10); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S11); key = ref accessor.Piece(keys.K11); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S12); key = ref accessor.Piece(keys.K12); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S13); key = ref accessor.Piece(keys.K13); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S14); key = ref accessor.Piece(keys.K14); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			stripe = ref accessor.Piece(stripes.S15); key = ref accessor.Piece(keys.K15); acc += AccumulateOnePair(stripe.Left, stripe.Right, key.Left, key.Right);
			acc ^= acc >> 47;
			ulong p1 = Multiply32to64((uint)acc, accessor.Piece(keys.Scramble).Left);
			ulong p2 = Multiply32to64((uint)(acc >> 32), accessor.Piece(keys.Scramble).Right);
			acc = p1 ^ p2;

			accessor.SetAcc(ref accumulator, acc);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static ulong AccumulateOnePair(ulong valueLeft, ulong valueRight, ulong keyLeft, ulong keyRight)
		{
			return valueLeft + (valueRight << 32) + (ulong)(uint)(valueLeft + keyLeft) * (ulong)(uint)(valueRight + keyRight);
		}

		public static bool UseAvx2;
		public static bool UseSse2;

		private static void LongSequenceHash_Scalar(ref OctoAccumulator acc, ReadOnlySpan<byte> data)
		{
			var unprocessedData = data;
			ReadOnlySpan<StripeBlock<Stripe>> blocks = unprocessedData.PopAll<StripeBlock<Stripe>>();

			AccumulateStripeBlocks_ScalarPiecewise(ref acc, blocks);

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
			result64 += MultiplyAdd64(acc2.A ^ Keys64_A.A.Left, acc2.B ^ Keys64_A.A.Right);
			result64 += MultiplyAdd64(acc2.C ^ Keys64_A.B.Left, acc2.D ^ Keys64_A.B.Right);
			result64 += MultiplyAdd64(acc2.E ^ Keys64_A.C.Left, acc2.F ^ Keys64_A.C.Right);
			result64 += MultiplyAdd64(acc2.G ^ Keys64_A.D.Left, acc2.H ^ Keys64_A.D.Right);
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
