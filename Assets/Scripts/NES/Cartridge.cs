using Microsoft.Extensions.Logging;
using System.IO;

using static Nes.Constants;

namespace Nes
{
    public class Cartridge
    {
        private const uint iNesHeader = 0x1A53454E; // NES<EOF>

        private const byte TrainerBit = 0x04;
        private const byte HeaderSize = 16;
        private const ushort TrainerSize = 512;

        public byte[] PrgRAM { get; private set; }
        public byte[] PrgRom { get; private set; }
        public byte[] Chr { get; private set; }
        public byte PrgRomBanks { get; private set; }
        public byte ChrBanks { get; private set; }
        public byte Flag6 { get; private set; }
        public byte Flag7 { get; private set; }
        public byte Mapper { get; private set; }
        public bool Mirror { get; private set; }

        private Cartridge() { }

        public static Cartridge Create(string path, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<Cartridge>();

            var cartridge = new Cartridge();

            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
            using var reader = new BinaryReader(stream);

            // Parse Header
            if (iNesHeader != reader.ReadUInt32())
            {
                return null;
            }

            cartridge.PrgRomBanks = reader.ReadByte();

            cartridge.ChrBanks = reader.ReadByte();

            cartridge.Flag6 = reader.ReadByte();
            
            cartridge.Flag7 = reader.ReadByte();

            // Load PRGROM
            var prgRomSize = SixteenKB * cartridge.PrgRomBanks;
            cartridge.PrgRom = new byte[prgRomSize];
            var seekOffset = cartridge.Flag6.IsBitSet(TrainerBit) ? HeaderSize + TrainerSize : HeaderSize;
            reader.BaseStream.Seek(seekOffset, SeekOrigin.Begin);
            reader.Read(cartridge.PrgRom, 0, prgRomSize);

            // Load CHR
            if (cartridge.ChrBanks != 0)
            {
                // ROM
                var chrRomSize = EightKB * cartridge.ChrBanks;
                cartridge.Chr = new byte[chrRomSize];
                reader.Read(cartridge.Chr, 0, chrRomSize);
            }
            else
            {
                // RAM
                cartridge.Chr = new byte[EightKB];
            }

            cartridge.PrgRAM = new byte[EightKB];

            cartridge.Mapper = (byte)(cartridge.Flag7 & 0xF0 | cartridge.Flag6 >> 4 & 0xF);

            return cartridge;
        }
    }
}
