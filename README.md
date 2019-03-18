# xxHash3.NET
C# port of XXH3 (plus bonuses)

# XXH3
XXH3 is described here: http://fastcompression.blogspot.com/2019/03/presenting-xxh3.html

This is a poorly tested port of an experimental hash algorithm. Using it for something important would be dumb. It's also quite a bit slower than the xxHash64 implementation for large keys.

# xxHash32, xxHash64

I believe I have the fastest C# xxHash32 & xxHash64 implementations around, though I have not tested that rigorously. 

# xxHash32RNG

A PRNG implementation based on xxHash32. Very fast (particularly at generating uniform float values) but it discards 10 out of 32 bits of entropy to achieve it.
