# xxHash3.NET
C# port of XXH3 (plus bonuses)

# XXH3
XXH3 is described here: http://fastcompression.blogspot.com/2019/03/presenting-xxh3.html

This is a poorly tested port of an experimental hash algorithm. Using it for something important would be dumb. It's also quite a bit slower than the xxHash64 implementation for large keys.

# xxHash32, xxHash64

I believe I have the fastest C# xxHash32 & xxHash64 implementations around, though I have not tested that rigorously. 

But I have tested it somewhat... and I win. By a lot.

|                 Method | ByteLength |            Mean |    Throughput |
|----------------------- |----------- |----------------:|--------------:|
|         Zhent_xxHash32 |         15 |        12.59 ns |  1,136.5 MB/s |
|   xxHashSharp_xxHash32 |         15 |        12.64 ns |  1,132.1 MB/s |
|         Zhent_xxHash64 |         15 |        16.05 ns |    891.4 MB/s |
|      NeoSmart_xxHash64 |         15 |       153.18 ns |     93.4 MB/s |
|        yyproj_xxHash64 |         15 |       232.18 ns |     61.6 MB/s |
| HashFunctions_xxHash64 |         15 |       380.50 ns |     37.6 MB/s |
|        core20_xxHash64 |         15 |       653.27 ns |     21.9 MB/s |
|         Zhent_xxHash64 |    1000000 |    89,566.91 ns | 10,647.6 MB/s |
|         Zhent_xxHash32 |    1000000 |   170,840.81 ns |  5,582.2 MB/s |
|        yyproj_xxHash64 |    1000000 |   799,521.44 ns |  1,192.8 MB/s |
| HashFunctions_xxHash64 |    1000000 | 1,110,313.84 ns |    858.9 MB/s |
|   xxHashSharp_xxHash32 |    1000000 | 1,310,830.35 ns |    727.5 MB/s |
|      NeoSmart_xxHash64 |    1000000 | 1,562,210.75 ns |    610.5 MB/s |
|        core20_xxHash64 |    1000000 | 6,933,568.09 ns |    137.5 MB/s |

# xxHash32RNG

A PRNG implementation based on xxHash32. Very fast (particularly at generating uniform float values) but it discards 10 out of 32 bits of entropy to achieve it.
