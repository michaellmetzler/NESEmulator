namespace Nes
{
    public class Nrom : Mapper
    {
        public Nrom(Cartridge cartridge) : base(cartridge) { }

        protected override int GetPrgRomIndex(ushort address)
        {
            var index = base.GetPrgRomIndex(address);
            if (_cartridge.PrgRomBanks == 1 && index >= PrgRomBankSize)
            {
                index -= PrgRomBankSize;
            }

            return index;
        }
    }
}
