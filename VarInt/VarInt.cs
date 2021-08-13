using System;

namespace VarInt
{
    public static class VarInt
    {
        // MAX_LEN_UVAR_INT63 is the maximum number of bytes representing an uvarint in
        // this encoding, supporting a maximum value of 2^63 (uint63), aka
        // MAX_LEN_UVAR_INT63.
        public const int MAX_LEN_UVAR_INT63 = 9;

        public const int MAX_VARINT_LEN_16 = 3;
        public const int MAX_VARINT_LEN_32 = 5;
        public const int MAX_VARINT_LEN_64 = 10;

        // MAX_VALUE_UVAR_INT63 is the maximum encodable uint63 value.
        public const int MAX_VALUE_UVAR_INT63 = unchecked((1 << 63) - 1);

        private static byte[] GO_MATH_BITS_TABLES_LEN8TAB = new byte[256] {
            0x00, 0x01, 0x02, 0x02, 0x03, 0x03, 0x03, 0x03, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04, 0x04,
            0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05, 0x05,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06, 0x06,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07, 0x07,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
            0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08, 0x08,
        };

        // GoMathBitsLen64 returns the minimum number of bits required to represent x; the result is 0 for x == 0.
        private static int GoMathBitsLen64(UInt64 x)
        {
            int n = 0;

            if (x >= (ulong)1 << 32)
            {
                x >>= 32;
                n = 32;
            }

            if (x >= (ulong)1 << 16)
            {
                x >>= 16;
                n += 16;
            }

            if (x >= (ulong)1 << 8)
            {
                x >>= 8;
                n += 8;
            }

            return n + Convert.ToInt32(Math.Ceiling(Convert.ToDecimal(GO_MATH_BITS_TABLES_LEN8TAB[x])));
        }

        // UVarIntSize returns the size (in bytes) of `num` encoded as a unsigned varint.
        //
        // This may return a size greater than MaxUVarIntLen63, which would be an
        // illegal value, and would be rejected by readers.
        public static int UVarIntSize(UInt64 num)
        {
            var bits = GoMathBitsLen64(num);
            var q = bits / 7;
            var r = bits % 7;
            var size = q;
            if (r > 0 || size == 0)
            {
                size++;
            }
            return size;
        }

        // PutUVarInt encodes a uint64 into buf and returns the number of bytes written.
        // If the buffer is too small, PutUVarInt will panic.
        public static int GoEncodingBinaryVarIntPutUVarInt(byte[] buf, UInt64 x)
        {
            var i = 0;
            for (; x >= 0x80;)
            {
                buf[i] = (byte)(x | 0x80);
                x >>= 7;
                i++;
            }
            buf[i] = (byte)x;
            return i + 1;
        }

        // Uvarint decodes a uint64 from buf and returns that value and the
        // number of bytes read (> 0). If an error occurred, the value is 0
        // and the number of bytes n is <= 0 meaning:
        //
        // 	n == 0: buf too small
        // 	n  < 0: value larger than 64 bits (overflow)
        // 	        and -n is the number of bytes read
        //
        public static (UInt64, int) GoEncodingBinaryVarIntUVarInt(byte[] buf)
        {
            UInt64 x = 0;
            int s = 0;

            var i = -1;
            foreach (var b in buf)
            {
                i++;
                if (b < 0x80)
                {

                    if (i >= MAX_VARINT_LEN_64 || i == MAX_VARINT_LEN_64 - 1 && b > 1)
                    {
                        return (0, -(i + 1)); // overflow
                    }

                    return (x | (UInt64)b << s, i + 1);
                }

                x |= (UInt64)(b & 0x7f) << s;
                s += 7;
            }

            return (0, 0);
        }

        // ToUVarInt converts an unsigned integer to a varint-encoded []byte
        public static byte[] ToUVarInt(UInt64 num)
        {
            var buf = new byte[UVarIntSize(num)];
            var n = GoEncodingBinaryVarIntPutUVarInt(buf, num);
            return buf;
        }

        // FromUVarInt reads an unsigned varint from the beginning of buf, returns the
        // varint, and the number of bytes read.
        public static (UInt64, int) FromUVarInt(byte[] buf)
        {
            // Modified from the go standard library. Copyright the Go Authors and
            // released under the BSD License.
            UInt64 x = 0;
            int s = 0;

            var i = -1;
            foreach (var b in buf)
            {
                i++;
                if ((i == 8 && b >= 0x80) || i >= MAX_LEN_UVAR_INT63)
                {
                    // this is the 9th and last byte we're willing to read, but it
                    // signals there's more (1 in MSB).
                    // or this is the >= 10th byte, and for some reason we're still here.
                    throw new OverflowException("varints larger than uint63 not supported");
                }
                if (b < 0x80)
                {
                    if (b == 0 && s > 0)
                    {
                        throw new InvalidOperationException("varint not minimally encoded");
                    }
                    return (x | (UInt64)b << s, i + 1);
                }
                x |= (UInt64)(b & 0x7f) << s;
                s += 7;
            }

            throw new InvalidOperationException("varints malformed, could not reach the end");
        }

        // ReadUvarint reads an encoded unsigned integer from r and returns it as a uint64.
        public static UInt64 GoEncodingBinaryVarIntReadUVarInt(System.IO.BinaryReader r)
        {
            UInt64 x = 0;
            int s = 0;

            for (var i = 0; i < MAX_VARINT_LEN_64; i++)
            {
                var b = r.ReadByte();

                if (b < 0x80)
                {
                    if (i == MAX_VARINT_LEN_64 - 1 && b > 1)
                    {
                        throw new OverflowException();
                    }

                    return x | (UInt64)b << s;
                }

                x |= (UInt64)(b & 0x7f) << s;
                s += 7;
            }

            throw new OverflowException();
        }

        // ReadUVarInt reads a unsigned varint from the given reader.
        public static UInt64 ReadUVarInt(System.IO.BinaryReader r)
        {
            // Modified from the go standard library. Copyright the Go Authors and
            // released under the BSD License.
            UInt64 x = 0;
            int s = 0;

            for (var i = 0; ; i++)
            {
                byte b;
                try
                {
                    b = r.ReadByte();
                }
                catch (System.IO.EndOfStreamException eos)
                {
                    if (i != 0)
                    {
                        // "eof" will look like a success.
                        // If we've read part of a value, this is not a
                        // success.
                        throw new System.IO.EndOfStreamException("Unexpected EOF", eos);
                    }
                    throw;
                }

                if ((i == 8 && b >= 0x80) || i >= MAX_LEN_UVAR_INT63)
                {
                    // this is the 9th and last byte we're willing to read, but it
                    // signals there's more (1 in MSB).
                    // or this is the >= 10th byte, and for some reason we're still here.
                    throw new OverflowException("varints larger than uint63 not supported");
                }

                if (b < 0x80)
                {
                    if (b == 0 && s > 0)
                        throw new InvalidOperationException("varint not minimally encoded");

                    return x | (UInt64)b << s;
                }
                x |= (UInt64)0x7f << s;
                s += 7;
            }
        }

        // PutUVarInt is an alias for binary.PutUVarInt.
        //
        // This is provided for convenience so users of this library can avoid built-in
        // varint functions and easily audit code for uses of those functions.
        //
        // Make sure that x is smaller or equal to MaxValueUVarInt63, otherwise this
        // function will produce values that may be rejected by readers.
        public static int PutUVarInt(byte[] buf, UInt64 x) => GoEncodingBinaryVarIntPutUVarInt(buf, x);
    }
}