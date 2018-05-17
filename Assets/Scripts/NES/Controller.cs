using System;

namespace Nes
{
    public class Controller
    {
        public const ushort AddressOne = 0x4016;
        public const ushort AddressTwo = 0x4017;

        [Flags]
        public enum Button : byte
        {
            A = 1 << 0,
            B = 1 << 1,
            Select = 1 << 2,
            Start = 1 << 3,
            Up = 1 << 4,
            Down = 1 << 5,
            Left = 1 << 6,
            Right = 1 << 7
        }

        private Button _currentButtons;
        private Button _shiftRegister;
        private bool _strobe;

        public void Input(Button button, bool pressed)
        {
            if (pressed)
            {
                _currentButtons |= button;
            }
            else
            {
                _currentButtons &= ~button;
            }
        }
        public byte Read()
        {
            byte value = (byte)(((byte)_shiftRegister & 0x01) > 0 ? 1 : 0);
            if (!_strobe)
            {
                _shiftRegister = (Button)((byte)_shiftRegister >> 1 | 0x80); // Set bit 7 after reads
            }
            return value;
        }

        public void Write(byte value)
        {
            bool newStrobe = value == 1;
            // On strobe transition from high to low, latch the current button state
            if (_strobe && !newStrobe)
            {
                _shiftRegister = _currentButtons;
            }
            _strobe = newStrobe;
            
            // While strobe is high, continuously reload the current state
            if (_strobe)
            {
                _shiftRegister = _currentButtons;
            }
        }
    }
}
