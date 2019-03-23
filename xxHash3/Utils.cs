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
#if NETCOREAPP3_0
			var totBytes = @this.Length;
			var toLength = (totBytes / Unsafe.SizeOf<TTo>());
			var sliceLength = toLength * Unsafe.SizeOf<TTo>();
			ref var thisRef = ref MemoryMarshal.GetReference(@this);
			@this = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.Add(ref thisRef, sliceLength), totBytes - sliceLength);
			return MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<byte, TTo>(ref thisRef), toLength);
#endif

			return @this.PopAll<TTo, byte>();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<TTo> PopAll<TTo, TFrom>(this ref ReadOnlySpan<TFrom> @this) where TFrom : struct where TTo : struct
		{
			var totBytes = @this.Length * Unsafe.SizeOf<TFrom>();
			var toLength = (totBytes / Unsafe.SizeOf<TTo>());
			var sliceLength = toLength * Unsafe.SizeOf<TTo>() / Unsafe.SizeOf<TFrom>();

#if NETSTANDARD2_0
			var result = MemoryMarshal.Cast<TFrom, TTo>(@this);
#else
			var result = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.As<TFrom, TTo>(ref MemoryMarshal.GetReference(@this)), toLength);
#endif
			@this = @this.Slice(sliceLength);
			return result;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static uint AsLittleEndian(this uint @this)
		{
			if (BitConverter.IsLittleEndian) { return @this; }
			return BinaryPrimitives.ReverseEndianness(@this);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ulong AsLittleEndian(this ulong @this)
		{
			if (BitConverter.IsLittleEndian) { return @this; }
			return BinaryPrimitives.ReverseEndianness(@this);
		}

		public static bool TryPop<TTo>(this ref ReadOnlySpan<byte> @this, int count, out ReadOnlySpan<TTo> popped) where TTo : struct
		{
			var byteCount = count * Unsafe.SizeOf<TTo>();
			if (@this.Length >= byteCount)
			{
				popped = MemoryMarshal.Cast<byte, TTo>(@this.Slice(0, byteCount));
				@this = @this.Slice(byteCount);
				return true;
			}
			popped = default;
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly TTo First<TTo>(this ReadOnlySpan<byte> @this) where TTo : struct
		{
			return ref MemoryMarshal.Cast<byte, TTo>(@this)[0];
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly TTo Last<TTo>(this ReadOnlySpan<byte> @this) where TTo : struct
		{
			return ref MemoryMarshal.Cast<byte, TTo>(@this.Slice(@this.Length - Unsafe.SizeOf<TTo>()))[0];
		}

		public static ref readonly TTo First<TFrom, TTo>(this ReadOnlySpan<TFrom> @this) where TTo : struct where TFrom : struct
		{
#if NETSTANDARD2_0
			return ref MemoryMarshal.Cast<TFrom, TTo>(@this)[0];
#else
			//TODO: is this version actually any faster/better at all?
			return ref MemoryMarshal.AsRef<TTo>(MemoryMarshal.AsBytes(@this));
#endif
		}

	}

	public static class Safeish
	{
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ref readonly TTo As<TFrom, TTo>(in TFrom from) where TTo : struct where TFrom : struct
		{
			if(Unsafe.SizeOf<TFrom>() < Unsafe.SizeOf<TTo>()) { throw new InvalidCastException(); }
			return ref Unsafe.As<TFrom, TTo>(ref Unsafe.AsRef(from));
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static ReadOnlySpan<TTo> AsSpan<TFrom, TTo>(in TFrom from) where TTo : struct where TFrom : struct
		{
#if NETSTANDARD2_0
			var asSpan = CreateReadOnlySpan(ref Unsafe.AsRef(from));
#else
			var asSpan = MemoryMarshal.CreateReadOnlySpan(ref Unsafe.AsRef(from), 1);
#endif
			return MemoryMarshal.Cast<TFrom, TTo>(asSpan);
		}

#if NETSTANDARD2_0
		private static unsafe ReadOnlySpan<T> CreateReadOnlySpan<T>(ref T from) where T : struct
		{
			void* ptr = Unsafe.AsPointer(ref from);
			return new ReadOnlySpan<T>(ptr, 1);
		}
#endif


	}
}
