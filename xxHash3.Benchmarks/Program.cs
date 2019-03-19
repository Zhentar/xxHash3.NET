using System;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Diagnostics.Windows;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Reports;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Order;

namespace xxHash3
{
	class Program
	{
		static void Main()
		{

//#if RELEASE
			var config = DefaultConfig.Instance.With(ConfigOptions.DisableOptimizationsValidator)
											   .With(new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest));
			BenchmarkDotNet.Running.BenchmarkRunner.Run<LibComparison>(config);
			return;
//#endif
			foreach (var len in new [] { 0, 1,14, 101})
			{
				var bytes = ReferenceBytes(len);
				Console.WriteLine(xxHash32.Hash(bytes).ToString("X8"));

				Console.WriteLine();
			}
			
			foreach (var size in Sizes())
			{
				var bytesToHash = LongKeyTests.GetRandomBytes(1337, size);
				//using (var file = File.OpenWrite($"{size}.bin"))
				//{
				//	file.Write(bytesToHash);
				//}

				var hash = xxHash3.Hash64(bytesToHash);
				Console.WriteLine($"The hash for size {size} is: {hash:X}");
			}
		}

		private static byte[] ReferenceBytes(int TEST_DATA_SIZE)
		{
			byte[] test_data = new byte[TEST_DATA_SIZE];
			uint byte_gen = 0x9E3779B1U;


			/* Fill in the test_data buffer with "random" data. */
			for (int i = 0; i < TEST_DATA_SIZE; ++i)
			{
				test_data[i] = (byte)(byte_gen >> 24);
				byte_gen *= byte_gen;
			}
			return test_data;
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


	public class ThroughputColumn : IColumn
	{

		public string Id => nameof(ThroughputColumn);
		public string ColumnName => "Throughput";

		public ThroughputColumn() { }

		public bool IsDefault(Summary summary, BenchmarkCase benchmarkCase) => false;

		public string GetValue(Summary summary, BenchmarkCase benchmarkCase)
		{
			var bytes = (int)benchmarkCase.Parameters.Items[0].Value;
			var iterTime = summary[benchmarkCase].ResultStatistics.Mean;
			var iterPerSec = BenchmarkDotNet.Horology.TimeUnit.Second.NanosecondAmount / iterTime;
			var bytesPerSec = iterPerSec * bytes;
			var MBps = bytesPerSec / (1 << 20);
			return $"{MBps:N1} MB/s";
		}

		public bool IsAvailable(Summary summary) => true;
		public bool AlwaysShow => true;
		public ColumnCategory Category => ColumnCategory.Metric;
		public int PriorityInCategory => 0;
		public bool IsNumeric => true;
		public UnitType UnitType => UnitType.Dimensionless;
		public string Legend => $"Throughput in MB/s";
		public string GetValue(Summary summary, BenchmarkCase benchmarkCase, SummaryStyle style) => GetValue(summary, benchmarkCase);
		public override string ToString() => ColumnName;
	}

	public class MyConfig : ManualConfig
	{
		public MyConfig()
		{
			var run = Job.InProcess.WithMaxRelativeError(0.05)
						 .With(new[] { new EnvironmentVariable("COMPlus_TieredCompilation", "0") });
			Add(run);
			Add(new ThroughputColumn());
			Orderer = DefaultOrderer.Instance;
			//Orderer = new DefaultOrderer(SummaryOrderPolicy.FastestToSlowest);
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


	[Config(typeof(MyConfig))]
	public class LongKeyTests
	{
		private byte[] _bytes;

		//[Params(5, 10, 50, 1000, 1_000_000)]
		public int ByteLength { get; set; } = 1_000_000;

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

		[Benchmark]
		public uint XxHash32() => xxHash32.Hash(_bytes);

		[Benchmark]
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

	[Orderer(SummaryOrderPolicy.FastestToSlowest, MethodOrderPolicy.Declared)]
	[Config(typeof(MyConfig))]
	public class LibComparison
	{
		private byte[] _bytes;

		[Params(15, 1_000_000)]
		public int ByteLength { get; set; } = 1000000;

		[GlobalSetup]
		public void Setup() => _bytes = LongKeyTests.GetRandomBytes(1337, ByteLength);

		[Benchmark]
		public uint Zhent_xxHash32() => xxHash32.Hash(_bytes);

		[Benchmark]
		public ulong Zhent_xxHash64() => xxHash64.Hash(_bytes);

		[Benchmark]
		public ulong HashFunctions_xxHash64() => BitConverter.ToUInt64(ixxHash.ComputeHash(_bytes).Hash);
		private static readonly System.Data.HashFunction.xxHash.IxxHash ixxHash = System.Data.HashFunction.xxHash.xxHashFactory.Instance.Create(new System.Data.HashFunction.xxHash.xxHashConfig { HashSizeInBits = 64 });

		[Benchmark]
		public ulong xxHashSharp_xxHash32() => xxHashSharp.xxHash.CalculateHash(_bytes);

		[Benchmark]
		public ulong NeoSmart_xxHash64() => NeoSmart.Hashing.XXHash.XXHash64.Hash(_bytes);

		[Benchmark]
		public ulong core20_xxHash64()
		{
			core20.Initialize();
			core20.TransformFinalBlock(_bytes, 0, _bytes.Length);
			return BitConverter.ToUInt64(core20.Hash);
		}
		private static readonly Extensions.Data.XXHash64 core20 = Extensions.Data.XXHash64.Create();

		[Benchmark]
		public ulong yyproj_xxHash64()
		{
			yyproj.Initialize();
			yyproj.TransformFinalBlock(_bytes, 0, _bytes.Length);
			return BitConverter.ToUInt64(yyproj.Hash);
		}
		private static readonly Benchmarks.YYProject_XXHash64 yyproj = Benchmarks.YYProject_XXHash64.Create();
	}
}
