// iNES mapper 000
public class Nrom : Mapper
{
    private const int PrgRomAddress = 0x8000;
    private const int PrgRamAddress = 0x6000;

    public Nrom(Cartridge cartridge) : base(cartridge) { }

    public override byte Read(ushort address)
    {
        byte data = 0;

        if (address >= PrgRomAddress)
        {
            data = cartridge.prgROM[GetPrgRomIndex(address)];
        }
        else if (address >= PrgRamAddress)
        {
            data = cartridge.prgRAM[GetPrgRamIndex(address)];
        }
        else
        {
            data = cartridge.chr[address];
        }
        return data;
    }

    public override void Write(ushort address, byte data)
    {
        if (address >= PrgRamAddress && address <= 0x7fff)
        {
            cartridge.prgRAM[GetPrgRamIndex(address)] = data;
        }
    }

    private int GetPrgRomIndex(ushort address)
    {
        if (cartridge.prgRomBanks > 1)
        {
           return address - PrgRomAddress;
        }
        else
        {
            return address - 0xC000;
        }
    }

    private int GetPrgRamIndex(ushort address)
    {
        return address - PrgRamAddress;
    }
}
