public static class ByteExtensions
{
    public static bool IsBitSet(this byte b, int index)
    {
        return (byte)((b >> index) & 1) != 0;
    }
}
