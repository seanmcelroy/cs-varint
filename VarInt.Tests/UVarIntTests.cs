using System;
using System.Linq;
using Xunit;
using System.IO;

namespace VarInt.Tests
{
    public class UVarIntTests
    {
        [Fact]
        public void GoEncodingBinaryVarIntPutUVarInt()
        {
            (ulong input, ulong output)[] inputsAndOutputs = new[]{
                (1ul,0x01ul),
                (2ul,0x02ul),
                (127ul,0x7ful),
                (128ul,0x8001ul),
                (255ul,0xff01ul),
                (256ul,0x8002ul)
            };

            foreach (var io in inputsAndOutputs)
            {
                var buf = new byte[VarInt.MAX_VARINT_LEN_64];
                var n = VarInt.GoEncodingBinaryVarIntPutUVarInt(buf, io.input);

                var convertedBytes = buf.Take(n);
                var shouldBeBytes = BitConverter.GetBytes(io.output).Take(n).Reverse();

                Assert.True(convertedBytes.SequenceEqual(shouldBeBytes), $"Expected ({io.input}) {convertedBytes.Select(n => n.ToString("X")).Aggregate((c, n) => c + n)} is not same as {shouldBeBytes.Select(n => n.ToString("X")).Aggregate((c, n) => c + n)}");
            }
        }

        [Theory]
        [ClassData(typeof(UVarIntTestData))]
        public void ToAndFromUVarInt(ulong i)
        {
            var into = VarInt.ToUVarInt(i);
            var back = VarInt.FromUVarInt(into);
            Assert.True(i.Equals(back.Item1), $"Converted {i} into {into.Select(x => x.ToString("X")).Aggregate((c, n) => c + n)} but back to {back.Item1}");
        }

        [Theory]
        [ClassData(typeof(UVarIntTestData))]
        public void checkVarint(UInt64 x)
        {
            var buf = new byte[VarInt.MAX_VARINT_LEN_16];
            var expected = VarInt.GoEncodingBinaryVarIntPutUVarInt(buf, x);

            var size = VarInt.UVarIntSize(x);
            Assert.True(size == expected, $"expected varintsize of {x} to be {expected}, got {size}");

            UInt64 xi;
            int n;
            try
            {
                (xi, n) = VarInt.FromUVarInt(buf);
            }
            catch (Exception e)
            {
                throw new Exception("decoding error", e);
            }

            Assert.True(n == size, $"read the wrong size (expected {size} but got {n})");
            Assert.True(xi == x, $"Expected a different result {x} but got {xi}");
        }

        [Fact]
        public void TestVarintSize()
        {
            UInt64 max = 1 << 16;
            for (UInt64 x = 0; x < max; x++)
            {
                checkVarint(x);
            }
        }

        [Fact]
        public void TestOverflow_9thSignalsMore()
        {
            var buf = new byte[]{
                0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff,
                0x80,
            };

            using (var ms = new MemoryStream(buf))
            using (var br = new BinaryReader(ms))
            {
                Assert.Throws<OverflowException>(() => VarInt.ReadUVarInt(br));
            }
        }

        [Fact]
        public void TestOverflow_ReadBuffer()
        {
            var buf = new byte[]{
                0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff,
            };

            using (var ms = new MemoryStream(buf))
            using (var br = new BinaryReader(ms))
            {
                Assert.Throws<OverflowException>(() => VarInt.ReadUVarInt(br));
            }
        }

        [Fact]
        public void TestOverflow()
        {
            Assert.Throws<OverflowException>(() => VarInt.FromUVarInt(new byte[] {
                0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0xff,
                0xff, 0xff, 0xff, 0x00,
            }));
        }

        [Fact]
        public void TestNotMinimal()
        {
            var buf = new byte[] { 0x81, 0x00 };
            Assert.Throws<InvalidOperationException>(() => VarInt.FromUVarInt(buf));

            var (i, n) = VarInt.GoEncodingBinaryVarIntUVarInt(buf);

            Assert.True(n == buf.Length, "expected to read entire buffer");
            Assert.True(i == 1, "expected varint 1");
        }

        [Fact]
        public void TestNotMinimalRead()
        {
            var buf = new byte[] { 0x81, 0x00 };
            using (var ms = new MemoryStream(buf))
            using (var br = new BinaryReader(ms))
            {
                Assert.Throws<InvalidOperationException>(() => VarInt.ReadUVarInt(br));
            }

            using (var ms = new MemoryStream(buf))
            using (var br = new BinaryReader(ms))
            {
                var i = VarInt.GoEncodingBinaryVarIntReadUVarInt(br);
                Assert.True(i == 1, "expected varint 1");
            }
        }

        [Fact]
        public void TestUnderflow()
        {
            var buf = new byte[] { 0x81, 0x81 };
            Assert.Throws<InvalidOperationException>(() => VarInt.FromUVarInt(buf));
        }

        [Fact]
        public void TestEOF()
        {
            var buf = new byte[0];

            using (var ms = new MemoryStream(buf))
            using (var br = new BinaryReader(ms))
            {
                Assert.Throws<EndOfStreamException>(() => VarInt.ReadUVarInt(br));
            }
        }

        [Fact]
        public void TestUnexpectedEOF()
        {
            var buf = new byte[] { 0x81, 0x81 };

            using (var ms = new MemoryStream(buf))
            using (var br = new BinaryReader(ms))
            {
                Assert.Throws<EndOfStreamException>(() => VarInt.ReadUVarInt(br));
            }
        }
    }
}