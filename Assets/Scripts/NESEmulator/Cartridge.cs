using System.IO;
using static Constants;

public class Cartridge
{
    private const uint iNesHeader = 0x1A53454E; // NES<EOF>

    private const byte HeaderSize = 16;
    private const ushort TrainerSize = 512;
    public const byte TrainerBit = 0x04;

    public byte[] prgRAM;
    public byte[] prgROM;
    public byte[] chr;

    public byte prgRomBanks;
    public byte chrBanks;

    public byte flags6;
    public byte flags7;

    public bool mirror;

    public bool IsValid;

    public Cartridge(string path)
    {
        using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
        {
            using (var reader = new BinaryReader(stream))
            {

                IsValid = ParseHeader(reader);

                if (IsValid)
                {
                    prgRAM = new byte[EightKB];
                    LoadPrgRom(reader);
                    LoadChrMem(reader);
                }
            }
        }
    }

    private bool ParseHeader(BinaryReader reader)
    {
        if (iNesHeader != reader.ReadUInt32())
        {
            return false;
        }

        prgRomBanks = reader.ReadByte();
        chrBanks = reader.ReadByte();

        flags6 = reader.ReadByte();
        flags7 = reader.ReadByte();

        return true;
    }

    private void LoadPrgRom(BinaryReader reader)
    {
        var prgRomSize = SixteenKB * prgRomBanks;
        prgROM = new byte[prgRomSize];

        var seekOffset = flags6.IsBitSet(TrainerBit) ? HeaderSize + TrainerSize : HeaderSize;
        reader.BaseStream.Seek(seekOffset, SeekOrigin.Begin);
        reader.Read(prgROM, 0, prgRomSize);
    }

    private void LoadChrMem(BinaryReader reader)
    {
        var chrRomSize = EightKB * chrBanks;
        chr = new byte[chrRomSize];
        reader.Read(chr, 0, chrRomSize);
    }

    public byte GetMapper()
    {
        return (byte)((flags7 & 0xF0) | (flags6 >> 4 & 0xF));
    }
}
