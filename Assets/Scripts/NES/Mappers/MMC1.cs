using System;

using static Nes.Constants;

namespace Nes
{
    public class Mmc1 : Mapper
    {
        [Flags]
        private enum Load : byte
        {
            Data = 1 << 0,
            Reset = 1 << 7,
        }

        [Flags]
        private enum Control : byte
        {
            NameTableArrangement = 3 << 0,
            PrgRomBankMode = 3 << 2,
            ChrBankmode = 1 << 4,
        }

        [Flags]
        private enum PrgBank : byte
        {
            NameTableSelect = 15 << 0,
            PrgRamEnabled = 1 << 4,
        }

        private const ushort PrgBankAddress = 0xE000;
        private const ushort ChrBank1Address = 0xC000;
        private const ushort ChrBank0Address = 0xA000;

        private byte _shiftRegister;
        private int _writeCount;
        private Control _control;
        private byte _chrBank0, _chrBank1;
        private PrgBank _prgBank;

        private int _prgRomBankZeroSelect, _prgRomBankOneSelect;
        private int _chrBankSelect; 
        public Mmc1(Cartridge cartridge) : base(cartridge)
        {
            _prgRomBankZeroSelect = 0;
            _prgRomBankOneSelect = cartridge.PrgRomBanks - 1;

            _chrBankSelect = 0;
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

        protected override int GetChrIndex(ushort address)
        {
            int index = base.GetChrIndex(address);

            if (index < ChrBankSize)
            {
                index += ChrBankSize * _chrBankSelect;
            }
            else
            {
                index += (ChrBankSize * _chrBankSelect) - ChrBankSize;
            }

            return index;
        }

        protected override void WriteRegisters(ushort address, byte data)
        {
            if(data.IsBitSet((int)Load.Reset))
            {
                _shiftRegister = 0;
                _writeCount = 0;

                _control = (Control)((byte)_control | 0x0C);
            }
            else
            {
                _shiftRegister <<= 1;
                _shiftRegister |= (byte)(data & 1);

                if (++_writeCount < 5)
                {
                    return;
                }

                _writeCount = 0;

                if (address >= PrgBankAddress)
                {
                    _prgBank = (PrgBank) (_shiftRegister & 0x1F);
                }
                else if (address >= ChrBank1Address)
                {
                    _chrBank1 = (byte)(_shiftRegister & 0x1F);
                }
                else if (address >= ChrBank0Address )
                {
                    _chrBank0 = (byte)(_shiftRegister & 0x1F);
                } 
                else
                {
                    _control = (Control) (_shiftRegister & 0x1F);
                }
            }
        }
    }
}
