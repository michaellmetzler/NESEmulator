public abstract class Mapper
{
    public enum MirrorMode
    {
        Horizontal,
        Vertical
    };

    protected Cartridge cartridge;

    public Mapper(Cartridge cartridge)
    {
        this.cartridge = cartridge;
    }

    public abstract byte Read(ushort address);

    public abstract void Write(ushort address, byte data);

    public MirrorMode GetMirrorMode()
    {
        return (cartridge.flags6 & 1) == 1 ? MirrorMode.Vertical : MirrorMode.Horizontal;
    }
}
