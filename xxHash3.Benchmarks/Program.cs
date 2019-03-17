using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Jobs;



namespace xxHash3
{
	class Program
	{
		static void Main()
		{

#if RELEASE
			BenchmarkDotNet.Running.BenchmarkRunner.Run<LongKeyTests>();
			return;
#endif

			foreach (var size in Sizes())
			{
				var bytesToHash = LongKeyTests.GetRandomBytes(1337, size);
				var hash = xxHash3.Hash64(bytesToHash, 12345);
				Console.WriteLine($"The hash is: {hash:X}");
			}
		}



		private static IEnumerable<int> Sizes()
		{
			yield return 0;
			yield return 64;
			yield return 128;
			yield return 192; //No superblocks, no offset final stripe
			yield return 180; //No superblocks, final stripe offset
			yield return 1024; //One superblock exactly
			yield return 1080; //One superblock, final stripe offset
			yield return 1152; //One superblock plus one strip
			yield return 2048; //Two superblocks
			yield return 10000;
		}
	}

	public class MyConfig : ManualConfig
	{
		public MyConfig()
		{
			var run = Job.InProcess.WithMaxRelativeError(0.10)
						 .With(new[] { new EnvironmentVariable("COMPlus_TieredCompilation", "0") });
			Add(run);
		}
	}

	public class Profiled : ManualConfig
	{
		public Profiled()
		{
			var run = Job.Default.WithMaxRelativeError(0.1)
						 .With(new[] { new EnvironmentVariable("COMPlus_TieredCompilation", "0") });
			Add(run);
			Add(new EtwProfiler(new EtwProfilerConfig(false, cpuSampleIntervalInMiliseconds: 0.125f)));
		}
	}

	public class DiagnoserConfig : ManualConfig
	{
		public DiagnoserConfig()
		{
			var run = Job.Dry.With(new[] { new EnvironmentVariable("COMPlus_TieredCompilation", "0") });
			Add(run);
			Add(DisassemblyDiagnoser.Create(new DisassemblyDiagnoserConfig(true, false, true, true, 4)));
			Add(new InliningDiagnoser());
		}
	}


	[Config(typeof(DiagnoserConfig))]
	public class LongKeyTests
	{
		private byte[] _bytes;

		[Params(1_000_000)]
		public int ByteLength { get; set; }

		[GlobalSetup]
		public void Setup() => _bytes = GetRandomBytes(1337, ByteLength);

		public static byte[] GetRandomBytes(int seed, int count)
		{
			var bytes = new byte[count];
			var ints = MemoryMarshal.Cast<byte, int>(bytes);
			var rng = new xxHash32RNG(seed);
			for (int i = 0; i < ints.Length; i++)
			{
				ints[i] = rng.IntValue;
			}
			return bytes;
		}

		//[Benchmark]
		public uint XxHash32() => xxHash32.Hash(_bytes);

		//[Benchmark]
		public ulong XxHash64() => xxHash64.Hash(_bytes);

		[Benchmark]
		public ulong XxHash3() => xxHash3.Hash64(_bytes);
	}

	[SimpleJob]
	public class HashShortKeyLatency
	{

		private static readonly byte[][] TestData = { Encoding.UTF8.GetBytes("abc"), Encoding.UTF8.GetBytes("longerkey"), Encoding.UTF8.GetBytes("evenmorelongerkey") };

		[Benchmark]
		public uint XxHash32()
		{
			uint result = 0;
			foreach (var bytes in TestData)
			{
				var byteSpan = new ReadOnlySpan<byte>(bytes);
				for (int i = 0; i < 111; i++)
					result ^= xxHash32.Hash(byteSpan);
			}
			return result;
		}


		[Benchmark]
		public ulong XxHash64()
		{
			ulong result = 0;
			foreach (var bytes in TestData)
			{
				var byteSpan = new ReadOnlySpan<byte>(bytes);
				for (int i = 0; i < 111; i++)
					result ^= xxHash64.Hash(byteSpan);
			}
			return result;
		}


		[Benchmark]
		public ulong XxHash3()
		{
			ulong result = 0;
			foreach (var bytes in TestData)
			{
				var byteSpan = new ReadOnlySpan<byte>(bytes);
				for (int i = 0; i < 111; i++)
					result ^= xxHash3.Hash64(byteSpan);
			}
			return result;
		}

		[Benchmark]
		public int SimpleMultHash()
		{
			int result = 0;
			foreach (var bytes in TestData)
			{
				var byteSpan = new ReadOnlySpan<byte>(bytes);
				for (int i = 0; i < 111; i++)
					result ^= GetStringHash(byteSpan);
			}
			return result;

			static int GetStringHash(ReadOnlySpan<byte> bytes)
			{
				int result = 0;
				foreach (var nextByte in bytes)
				{
					result = (result * 31) ^ nextByte;
				}
				return result;
			}
		}
	}
}
