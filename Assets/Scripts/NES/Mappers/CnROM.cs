namespace Nes
{
    public class CnRom : Mapper
    {
        private int _chrBankSelect;

        public CnRom(Cartridge cartridge) : base(cartridge)
        {
            _chrBankSelect = 0;
        }
        protected override int GetPrgRomIndex(ushort address)
        {
            var index = base.GetPrgRomIndex(address);

            if (_cartridge.PrgRomBanks == 1 && index >= PrgRomBankSize)
            {
                index -= PrgRomBankSize;
            }

            return index;
        }

        protected override int GetChrIndex(ushort address)
        {
            int index = base.GetChrIndex(address);
            index += ChrBankSize * _chrBankSelect;
            return index;
        }

        protected override void WriteRegisters(ushort address, byte data)
        {
            _chrBankSelect = data & 0x03;
        }
    }
}
