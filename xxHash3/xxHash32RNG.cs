using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace xxHash3
{
	public class xxHash32RNG
	{
		private int position;
		private readonly uint seed;

		public xxHash32RNG() : this(Environment.TickCount) { }
		public xxHash32RNG(int seed) => this.seed = (uint)seed;

		public bool TestOneIn(int odds) => Value * odds <= 1f;
		public int Range(int min, int max) => min + (int)(GetHash(position++) % (max - min));
		public float Range(float min, float max) => min + GetHash(position++) * (max - min) * (1.0f / uint.MaxValue);
		public float Value => new FloatUnion(0x3F800000U | (GetHash(position++) >> 9)).FloatVal - 1.0f;
		public int IntValue => (int)GetHash(position++);

		const uint PRIME32_2 = 2246822519U;
		const uint PRIME32_3 = 3266489917U;
		const uint PRIME32_4 = 668265263U;
		const uint PRIME32_5 = 374761393U;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private uint GetHash(int buf)
		{
			uint h32 = seed + PRIME32_5;
			h32 += (uint)buf * PRIME32_3;
			h32 = ((h32 << 17) | (h32 >> 15)) * PRIME32_4;
			h32 ^= h32 >> 15;
			h32 *= PRIME32_2;
			h32 ^= h32 >> 13;
			h32 *= PRIME32_3;
			h32 ^= h32 >> 16;
			return h32;
		}

		[StructLayout(LayoutKind.Explicit)]
		private struct FloatUnion
		{
			[FieldOffset(0)] public readonly uint IntVal;
			[FieldOffset(0)] public readonly float FloatVal;

			public FloatUnion(uint intVal)
			{
				FloatVal = 0;
				IntVal = intVal;
			}
		}
	}
}