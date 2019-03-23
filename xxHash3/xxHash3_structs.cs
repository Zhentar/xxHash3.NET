using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace xxHash3
{
	partial class xxHash3
	{
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

		private interface IAccumulatorWiseAccessor
		{
			ref readonly UintPair Piece(in Stripe stripe);
			ref readonly KeyPair Piece(in OctoKey key);
			ulong Piece(in OctoAccumulator acc);
			void SetAcc(ref OctoAccumulator acc, ulong val);
		}

		//These accessors generated with this powershell script:
		/*
@("A","B","C","D","E","F","G","H") | % { `
@'
private struct {0}Accessor : IAccumulatorWiseAccessor 
{{
	public ref readonly UintPair Piece(in Stripe stripe) => ref stripe.{0};
	public ref readonly KeyPair Piece(in OctoKey key) => ref key.{0};
	public ulong Piece(in OctoAccumulator acc) => acc.{0};
	public void SetAcc(ref OctoAccumulator acc, ulong val) => acc.{0} = val;
}}
'@ -f $_ }
		*/
		
		private struct AAccessor : IAccumulatorWiseAccessor
		{
			public ref readonly UintPair Piece(in Stripe stripe) => ref stripe.A;
			public ref readonly KeyPair Piece(in OctoKey key) => ref key.A;
			public ulong Piece(in OctoAccumulator acc) => acc.A;
			public void SetAcc(ref OctoAccumulator acc, ulong val) => acc.A = val;
		}
		private struct BAccessor : IAccumulatorWiseAccessor
		{
			public ref readonly UintPair Piece(in Stripe stripe) => ref stripe.B;
			public ref readonly KeyPair Piece(in OctoKey key) => ref key.B;
			public ulong Piece(in OctoAccumulator acc) => acc.B;
			public void SetAcc(ref OctoAccumulator acc, ulong val) => acc.B = val;
		}
		private struct CAccessor : IAccumulatorWiseAccessor
		{
			public ref readonly UintPair Piece(in Stripe stripe) => ref stripe.C;
			public ref readonly KeyPair Piece(in OctoKey key) => ref key.C;
			public ulong Piece(in OctoAccumulator acc) => acc.C;
			public void SetAcc(ref OctoAccumulator acc, ulong val) => acc.C = val;
		}
		private struct DAccessor : IAccumulatorWiseAccessor
		{
			public ref readonly UintPair Piece(in Stripe stripe) => ref stripe.D;
			public ref readonly KeyPair Piece(in OctoKey key) => ref key.D;
			public ulong Piece(in OctoAccumulator acc) => acc.D;
			public void SetAcc(ref OctoAccumulator acc, ulong val) => acc.D = val;
		}
		private struct EAccessor : IAccumulatorWiseAccessor
		{
			public ref readonly UintPair Piece(in Stripe stripe) => ref stripe.E;
			public ref readonly KeyPair Piece(in OctoKey key) => ref key.E;
			public ulong Piece(in OctoAccumulator acc) => acc.E;
			public void SetAcc(ref OctoAccumulator acc, ulong val) => acc.E = val;
		}
		private struct FAccessor : IAccumulatorWiseAccessor
		{
			public ref readonly UintPair Piece(in Stripe stripe) => ref stripe.F;
			public ref readonly KeyPair Piece(in OctoKey key) => ref key.F;
			public ulong Piece(in OctoAccumulator acc) => acc.F;
			public void SetAcc(ref OctoAccumulator acc, ulong val) => acc.F = val;
		}
		private struct GAccessor : IAccumulatorWiseAccessor
		{
			public ref readonly UintPair Piece(in Stripe stripe) => ref stripe.G;
			public ref readonly KeyPair Piece(in OctoKey key) => ref key.G;
			public ulong Piece(in OctoAccumulator acc) => acc.G;
			public void SetAcc(ref OctoAccumulator acc, ulong val) => acc.G = val;
		}
		private struct HAccessor : IAccumulatorWiseAccessor
		{
			public ref readonly UintPair Piece(in Stripe stripe) => ref stripe.H;
			public ref readonly KeyPair Piece(in OctoKey key) => ref key.H;
			public ulong Piece(in OctoAccumulator acc) => acc.H;
			public void SetAcc(ref OctoAccumulator acc, ulong val) => acc.H = val;
		}

	}
}
