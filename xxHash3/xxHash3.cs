using System;
using System.Runtime.InteropServices;
using System.Buffers.Binary;
using System.Runtime.CompilerServices;



namespace xxHash3
{
	public partial class xxHash3
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
			//The following key values are optional
			0xeac5ac83,0x34d3ebc3,0xc581a0ff,0xfa1363eb,
			0x170ddd51,0xb7f0da49,0xd3165526,0x29d4689e,
			0x2b16be58,0x7d47a1fc,0x8ff8b8d1,0x7ad031ce,
			0x45cb3a8f,0x95160428,0xafd7fbca,0xbb4b407e,
		};
		const int KEYSET_DEFAULT_SIZE = 48;
		const int STRIPE_BYTES = 64;
		const int STRIPE_ELEMENTS = (STRIPE_BYTES / sizeof(uint));
		const int STRIPES_PER_BLOCK = (KEYSET_DEFAULT_SIZE - STRIPE_ELEMENTS) / 2;

		const uint kKey_1_left = 0xb8fe6c39;
		const uint kKey_1_right = 0x23a44bbe;
		const ulong kKey_1 = 0x23a44bbe_b8fe6c39;
		const ulong kKey_2 = 0xf721ad1c_7c01812c;


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

		[StructLayout(LayoutKind.Sequential)]
		private unsafe struct StripeBytes
		{
			public fixed byte Bytes[STRIPE_BYTES];
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct StripeSegment
		{
			public readonly UintPair First;
			public readonly UintPair Second;
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
		private readonly struct KeyPairPair
		{
			public readonly KeyPair First;
			public readonly KeyPair Second;
		}

		[StructLayout(LayoutKind.Sequential)]
		private struct OctoKey
		{
			//Unfortunately, fixed can't be used with user defined structs, so we get this instead...
			public readonly KeyPair A;
			public readonly KeyPair B;
			public readonly KeyPair C;
			public readonly KeyPair D;
			public readonly KeyPair E;
			public readonly KeyPair F;
			public readonly KeyPair G;
			public readonly KeyPair H;
		}

		public static ulong Hash64(in ReadOnlySpan<byte> data, ulong seed = 0)
		{
			int len = data.Length;
			if (len <= 16) { return ZeroToSixteenBytes(data, seed); }

			var keys = MemoryMarshal.Cast<uint, KeyPairPair>(kKey);
			ulong acc = PRIME64_1 * ((uint)data.Length + seed);
			if (len > 32)
			{
				if (len > 64)
				{
					if (len > 96)
					{
						if (len > 128) return HashLongSequence64(data, seed);

						acc += MixSixteenBytes(data.Slice(48), keys[6]);
						acc += MixSixteenBytes(data.Slice(len - 64), keys[7]);
					}

					acc += MixSixteenBytes(data.Slice(32), keys[4]);
					acc += MixSixteenBytes(data.Slice(len - 48), keys[5]);
				}

				acc += MixSixteenBytes(data.Slice(16), keys[2]);
				acc += MixSixteenBytes(data.Slice(len - 32), keys[3]);

			}

			acc += MixSixteenBytes(data.Slice(0), keys[0]);
			acc += MixSixteenBytes(data.Slice(len - 16), keys[1]);

			return Avalanche(acc);
		}

		private static ulong ZeroToSixteenBytes(in ReadOnlySpan<byte> data, ulong seed)
		{
			if (data.Length > 8) return NineToSixteenBytes(data, seed);
			if (data.Length >= 4) return FourToEightBytes(data, seed);
			if (data.Length > 0) return OneToThreeBytes(data, seed);
			return seed;
		}

		private static ulong OneToThreeBytes(in ReadOnlySpan<byte> data, ulong seed)
		{
			byte c1 = data[0];
			byte c2 = data[data.Length >> 1];
			byte c3 = data[data.Length - 1];
			uint l1 = c1 + (uint)(c2 << 8);
			uint l2 = (uint)data.Length + (uint)(c3 << 2);
			ulong ll11 = Multiply32to64(l1 + (uint)seed + kKey_1_left, l2 + kKey_1_right);
			return Avalanche(ll11);
		}

		private static ulong FourToEightBytes(in ReadOnlySpan<byte> data, ulong seed)
		{
			ulong acc = PRIME64_1 * ((uint)data.Length + seed);
			uint l1 = BinaryPrimitives.ReadUInt32LittleEndian(data) + kKey_1_left;
			uint l2 = BinaryPrimitives.ReadUInt32LittleEndian(data.Slice(data.Length - 4)) + kKey_1_right;
			acc += Multiply32to64(l1, l2);
			return Avalanche(acc);
		}

		private static ulong NineToSixteenBytes(in ReadOnlySpan<byte> data, ulong seed)
		{
			ulong acc = PRIME64_1 * ((uint)data.Length + seed);
			ulong ll1 = BinaryPrimitives.ReadUInt64LittleEndian(data) + kKey_1;
			ulong ll2 = BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(data.Length - 8)) + kKey_2;
			acc += MultiplyAdd64(ll1, ll2);
			return Avalanche(acc);
		}

		private static ulong Avalanche(ulong h64)
		{
			h64 ^= h64 >> 29;
			h64 *= PRIME64_3;
			h64 ^= h64 >> 32;
			return h64;
		}

		private static ulong MixSixteenBytes(in ReadOnlySpan<byte> data, in KeyPairPair key)
		{
			return MultiplyAdd64(
					   BinaryPrimitives.ReadUInt64LittleEndian(data) ^ key.First.Key64,
					   BinaryPrimitives.ReadUInt64LittleEndian(data.Slice(8)) ^ key.Second.Key64);
		}

		private static ulong MixTwoAccumulators(in ReadOnlySpan<ulong> acc, in KeyPairPair key)
		{
			return MultiplyAdd64(acc[0] ^ key.First.Key64, acc[1] ^ key.Second.Key64);
		}

		private static void AccumulateStripe(in Span<ulong> acc, in Stripe data, in ReadOnlySpan<KeyPair> keys)
		{
			ref var theKeys = ref MemoryMarshal.GetReference(MemoryMarshal.Cast<KeyPair, OctoKey>(keys));
			//Hand unrolled...
			AccumulateOnePair(ref Unsafe.Add(ref MemoryMarshal.GetReference(acc), 0), data.A, theKeys.A);
			AccumulateOnePair(ref Unsafe.Add(ref MemoryMarshal.GetReference(acc), 1), data.B, theKeys.B);
			AccumulateOnePair(ref Unsafe.Add(ref MemoryMarshal.GetReference(acc), 2), data.C, theKeys.C);
			AccumulateOnePair(ref Unsafe.Add(ref MemoryMarshal.GetReference(acc), 3), data.D, theKeys.D);
			AccumulateOnePair(ref Unsafe.Add(ref MemoryMarshal.GetReference(acc), 4), data.E, theKeys.E);
			AccumulateOnePair(ref Unsafe.Add(ref MemoryMarshal.GetReference(acc), 5), data.F, theKeys.F);
			AccumulateOnePair(ref Unsafe.Add(ref MemoryMarshal.GetReference(acc), 6), data.G, theKeys.G);
			AccumulateOnePair(ref Unsafe.Add(ref MemoryMarshal.GetReference(acc), 7), data.H, theKeys.H);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void AccumulateOnePair(ref ulong acc, UintPair value, KeyPair key)
		{
			var dataLeft = value.Left;
			var dataRight = value.Right;
			var mul = Multiply32to64(dataLeft + key.Left, dataRight + key.Right);
			acc += mul + dataLeft + ((ulong)dataRight << 32);
		}


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static void ScrambleAccumulators(in Span<ulong> acc, in ReadOnlySpan<uint> key)
		{
			for (int i = 0; i < acc.Length; i++)
			{
				acc[i] ^= acc[i] >> 47;
				ulong p1 = Multiply32to64((uint)acc[i], key[2 * i]);
				ulong p2 = Multiply32to64((uint)(acc[i] >> 32), key[2 * i + 1]);
				acc[i] = p1 ^ p2;
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		static void AccumulateStripeBlock(in Span<ulong> acc, in ReadOnlySpan<Stripe> stripes, ReadOnlySpan<KeyPair> key)
		{
			for (int i = 0; i < stripes.Length; i++)
			{
				AccumulateStripe(acc, stripes[i], key);
				key = key.Slice(1);
			}
		}

		public static void LongSequenceHashInternal(in Span<ulong> acc, in ReadOnlySpan<byte> data)
		{
#if NETCOREAPP3_0
			if (System.Runtime.Intrinsics.X86.Sse2.IsSupported)
			{
				AccumulateStripes_SSE2(data, acc);
				return;
			}
#endif

			var keys = MemoryMarshal.Cast<uint, KeyPair>(kKey);
			var stripes = MemoryMarshal.Cast<byte, Stripe>(data);
				
			while (stripes.Length >= STRIPES_PER_BLOCK)
			{
				var blockStripes = stripes.Slice(0, STRIPES_PER_BLOCK);
				stripes = stripes.Slice(STRIPES_PER_BLOCK);
				AccumulateStripeBlock(acc, blockStripes, keys);
				ScrambleAccumulators(acc, kKey.AsSpan(KEYSET_DEFAULT_SIZE - STRIPE_ELEMENTS));
			}

			/* last partial block */
			AccumulateStripeBlock(acc, stripes, keys);

			/* last stripe */
			if ((data.Length & (Unsafe.SizeOf<Stripe>() - 1)) != 0)
			{
#if NETSTANDARD2_0
				ref readonly Stripe stripe = ref MemoryMarshal.Cast<byte, Stripe>(data.Slice(data.Length - Unsafe.SizeOf<Stripe>()))[0];
#else
				ref readonly Stripe stripe = ref MemoryMarshal.AsRef<Stripe>(data.Slice(data.Length - Unsafe.SizeOf<Stripe>()));
#endif
				AccumulateStripe(acc, stripe , keys.Slice(stripes.Length));
			}
		}


		private static ulong MergeAccumulators(in Span<ulong> acc, ulong start)
		{
			var keys = MemoryMarshal.Cast<uint, KeyPairPair>(kKey);
			ulong result64 = start;

			result64 += MixTwoAccumulators(acc, keys[0]);
			result64 += MixTwoAccumulators(acc.Slice(2), keys[1]);
			result64 += MixTwoAccumulators(acc.Slice(4), keys[2]);
			result64 += MixTwoAccumulators(acc.Slice(6), keys[3]);

			return Avalanche(result64);
		}

		private static ulong HashLongSequence64(in ReadOnlySpan<byte> data, ulong seed)
		{
			Span<ulong> acc = stackalloc ulong[8];
			acc[0] = seed;
			acc[1] = PRIME64_1;
			acc[2] = PRIME64_2;
			acc[3] = PRIME64_3;
			acc[4] = PRIME64_4;
			acc[5] = PRIME64_5;
			acc[6] = seed;
			acc[7] = 0; //already zeroed, but allows for stripping localsinit if one so desires

			LongSequenceHashInternal(acc, data);

			/* converge into final hash */
			return MergeAccumulators(acc, (ulong)data.Length * PRIME64_1);
		}

		static unsafe ulong MultiplyAdd64(ulong lhs, ulong rhs)
		{
#if NETCOREAPP3_0
			if(System.Runtime.Intrinsics.X86.Bmi2.X64.IsSupported)
			{
				ulong lowHalf;
				ulong highHalf = System.Runtime.Intrinsics.X86.Bmi2.X64.MultiplyNoFlags(lhs, rhs, &lowHalf);
				return lowHalf + highHalf;
			}
#endif
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
