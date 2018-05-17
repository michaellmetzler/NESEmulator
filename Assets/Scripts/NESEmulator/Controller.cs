using System;

public class Controller
{
    public const ushort AddressOne = 0x4016;
    public const ushort AddressTwo = 0x4017;

    [Flags]
    public enum Button : byte
    {
        Right = 1 << 0,
        Left = 1 << 1,
        Down = 1 << 2,
        Up = 1 << 3,
        Start = 1 << 4,
        Select = 1 << 5,
        B = 1 << 6,
        A = 1 << 7
    }

    private Button data;
    private bool strobe;

    public void Input(Button button, bool pressed)
    {
        if (pressed)
        {
            data |= button;
        }
        else
        {
            data &= ~button;
        }
    }
    public byte Read()
    {
        byte value = (byte)((((byte)data & 0x80) > 0) ? 1 : 0);
        if (!strobe)
        {
            data = (Button)((byte)data << 1);
        }
        return value;
    }

    public void Write(byte value)
    {
        strobe = value == 1 ? true : false;
    }
}

