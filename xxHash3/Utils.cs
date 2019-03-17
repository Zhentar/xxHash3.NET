using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Buffers.Binary;

namespace xxHash3
{
	internal static class Utils
	{

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<TTo> PopAll<TTo>(this ref ReadOnlySpan<byte> @this) where TTo : struct
		{
			return @this.PopAll<TTo, byte>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<TTo> PopAll<TTo, TFrom>(this ref ReadOnlySpan<TFrom> @this) where TFrom : struct where TTo : struct
		{
			var totBytes = @this.Length * Unsafe.SizeOf<TFrom>();
			var toLength = (totBytes / Unsafe.SizeOf<TTo>());
			var sliceLength = toLength * Unsafe.SizeOf<TTo>() / Unsafe.SizeOf<TFrom>();
#if NETCOREAPP2_1 || NETCOREAPP3_0
			var result = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(@this)), toLength);
#else
			var result = MemoryMarshal.Cast<TFrom, TTo>(@this);
#endif
			@this = @this.Slice(sliceLength);
			return result;
		}

		public static uint AsLittleEndian(this uint @this)
		{
			if (BitConverter.IsLittleEndian) { return @this; }
			return BinaryPrimitives.ReverseEndianness(@this);
		}
	}
}
