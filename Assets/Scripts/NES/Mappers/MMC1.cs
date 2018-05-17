namespace Nes
{
    public class Mmc1 : Mapper
    {
        private int _prgRomBankZeroSelect, _prgRomBankOneSelect;
        private int _chrBankZeroSelect, _chrBankOneSelect;

        public Mmc1(Cartridge cartridge) : base(cartridge)
        {

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
                index += PrgRomBankSize * _prgRomBankOneSelect;
            }

            return index;
        }

        protected override int GetChrIndex(ushort address)
        {
            var index = base.GetChrIndex(address);

            if (index < ChrBankSize)
            {
                index += ChrBankSize * _chrBankZeroSelect;
            }
            else
            {
                index += ChrBankSize * _chrBankOneSelect;
            }

            return index;
        }

        protected override void WriteRegisters(ushort address, byte data)
        {

        }
    }
}
