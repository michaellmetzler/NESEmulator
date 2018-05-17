public static class IntExtensions
{
    public static bool IsBitSet(this int b, int index)
    {
        return (byte)((b >> index) & 1) != 0;
    }
}
