namespace Nes
{
    public static class ByteExtensions
    {
        public static bool IsBitSet(this byte b, int index) => (byte)(b >> index & 1) != 0;

        public static byte ReverseBits(this byte b)
        {
            b = (byte)((b & 0xF0) >> 4 | (b & 0x0F) << 4);
            b = (byte)((b & 0xCC) >> 2 | (b & 0x33) << 2);
            b = (byte)((b & 0xAA) >> 1 | (b & 0x55) << 1);
            return b;
        }
    }
}
