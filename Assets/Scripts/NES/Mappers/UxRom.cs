namespace Nes
{
    public class UxRom : Mapper
    {
        private int _prgRomBankZeroSelect, _prgRomBankOneSelect;

        public UxRom(Cartridge cartridge) : base(cartridge)
        {
            _prgRomBankZeroSelect = 0;
            _prgRomBankOneSelect = cartridge.PrgRomBanks - 1;
        }

        protected override int GetPrgRomIndex(ushort address)
        {
            var index = base.GetPrgRomIndex(address);

            if (index < PrgRomBankSize)
            {
                index += PrgRomBankSize * _prgRomBankZeroSelect;
            }
            else
            {
                index += (PrgRomBankSize * _prgRomBankOneSelect) - PrgRomBankSize;
            }

            return index;
        }

        protected override void WriteRegisters(ushort address, byte data)
        {
            _prgRomBankZeroSelect = data & 0x0F;
        }
    }
}
