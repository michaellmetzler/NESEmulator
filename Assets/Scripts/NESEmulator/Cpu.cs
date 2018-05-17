using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using static Constants;

public class Cpu
{
    [Flags]
    public enum Status : byte
    {
        Carry = 1 << 0,
        Zero = 1 << 1,
        InterruptDisabled = 1 << 2,
        DecimalMode = 1 << 3,
        Bit4 = 1 << 4,
        Bit5 = 1 << 5,
        Overflow = 1 << 6,
        Negative = 1 << 7
    }

    public enum AddressingMode
    {
        Implied,
        Accumulator,
        Immediate,
        ZeroPage,
        ZeroPageX,
        ZeroPageY,
        Absolute,
        AbsoluteX,
        AbsoluteY,
        Relative,
        Indirect,
        IndirectX,
        IndirectY
    }

    public struct Opcode
    {
        public Func<AddressingMode, byte, byte, bool, byte> Instruction { get; set; }
        public AddressingMode Mode { get; set; }
        public byte OpcodeSize { get; set; }
        public byte Cycles { get; set; }
        public bool OopsCycle { get; set; }

        public Opcode(Func<AddressingMode, byte, byte, bool, byte> instruction, AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle = false)
        {
            Instruction = instruction;
            Mode = mode;
            OpcodeSize = opcodeSize;
            Cycles = cycles;
            OopsCycle = oopsCycle;
        }
    }

    // Memory Sizes
    private const ushort InternalRamSize = 0x800;
    private const ushort PpuRegistersSize = 0x008;

    // Memory Addresses
    private const ushort InternalCpuRamAddressEnd = 0x1FFF;
    private const ushort PpuRegistersAddressStart = 0x2000;
    private const ushort PpuRegistersAddressEnd = 0x3FFF;
    private const ushort ApuIORegistersAddressEnd = 0x4017;
    private const ushort TestModeAddressEnd = 0x401F;
    private const ushort CartridgeAddress = 0x4020;

    // Memory Offsets
    private const ushort StackOffset = 0x0100;

    // Interrupt Vectors
    private const ushort NmiVector = 0xFFFA;
    private const ushort IrqBrkVector = 0xFFFE;
    private const ushort ResetVector = 0xFFFC;

    // Cycles
    private const int TotalDmaCycles = 513;
    private const int DmaCycles = 1;
    private const int InterruptCycles = 7;

    // Opcodes
    public readonly ReadOnlyDictionary<byte, Opcode> opcodeLookup;
    private ReadOnlyDictionary<byte, Opcode> InitializeOpcodes()
    {
        return new ReadOnlyDictionary<byte, Opcode>(new Dictionary<byte, Opcode>
        {
            { 0x69, new Opcode(ADC, AddressingMode.Immediate,   2, 2) },
            { 0x65, new Opcode(ADC, AddressingMode.ZeroPage,    2, 3) },
            { 0x75, new Opcode(ADC, AddressingMode.ZeroPageX,   2, 4) },
            { 0x6D, new Opcode(ADC, AddressingMode.Absolute,    3, 4) },
            { 0x7D, new Opcode(ADC, AddressingMode.AbsoluteX,   3, 4, true) },
            { 0x79, new Opcode(ADC, AddressingMode.AbsoluteY,   3, 4, true) },
            { 0x61, new Opcode(ADC, AddressingMode.IndirectX,   2, 6) },
            { 0x71, new Opcode(ADC, AddressingMode.IndirectY,   2, 5, true) },

            { 0x29, new Opcode(AND, AddressingMode.Immediate,   2, 2) },
            { 0x25, new Opcode(AND, AddressingMode.ZeroPage,    2, 3) },
            { 0x35, new Opcode(AND, AddressingMode.ZeroPageX,   2, 4) },
            { 0x2D, new Opcode(AND, AddressingMode.Absolute,    3, 4) },
            { 0x3D, new Opcode(AND, AddressingMode.AbsoluteX,   3, 4, true) },
            { 0x39, new Opcode(AND, AddressingMode.AbsoluteY,   3, 4, true) },
            { 0x21, new Opcode(AND, AddressingMode.IndirectX,   2, 6) },
            { 0x31, new Opcode(AND, AddressingMode.IndirectY,   2, 5, true) },

            { 0x0A, new Opcode(ASL, AddressingMode.Accumulator, 1, 2) },
            { 0x06, new Opcode(ASL, AddressingMode.ZeroPage,    2, 5) },
            { 0x16, new Opcode(ASL, AddressingMode.ZeroPageX,   2, 6) },
            { 0x0E, new Opcode(ASL, AddressingMode.Absolute,    3, 6) },
            { 0x1E, new Opcode(ASL, AddressingMode.AbsoluteX,   3, 7) },

            { 0x90, new Opcode(BCC, AddressingMode.Relative,    2, 2) },

            { 0xB0, new Opcode(BCS, AddressingMode.Relative,    2, 2) },

            { 0xF0, new Opcode(BEQ, AddressingMode.Relative,    2, 2) },

            { 0x24, new Opcode(BIT, AddressingMode.ZeroPage,    2, 3) },
            { 0x2C, new Opcode(BIT, AddressingMode.Absolute,    3, 4) },

            { 0x30, new Opcode(BMI, AddressingMode.Relative,    2, 2) },

            { 0xD0, new Opcode(BNE, AddressingMode.Relative,    2, 2) },

            { 0x10, new Opcode(BPL, AddressingMode.Relative,    2, 2) },

            { 0x00, new Opcode(BRK, AddressingMode.Implied,     1, 7) },

            { 0x50, new Opcode(BVC, AddressingMode.Relative,    2, 2) },

            { 0x70, new Opcode(BVS, AddressingMode.Relative,    2, 2) },

            { 0x18, new Opcode(CLC, AddressingMode.Implied,     1, 2) },

            { 0xD8, new Opcode(CLD, AddressingMode.Implied,     1, 2) },

            { 0x58, new Opcode(CLI, AddressingMode.Implied,     1, 2) },

            { 0xB8, new Opcode(CLV, AddressingMode.Implied,     1, 2) },

            { 0xC9, new Opcode(CMP, AddressingMode.Immediate,   2, 2) },
            { 0xC5, new Opcode(CMP, AddressingMode.ZeroPage,    2, 3) },
            { 0xD5, new Opcode(CMP, AddressingMode.ZeroPageX,   2, 4) },
            { 0xCD, new Opcode(CMP, AddressingMode.Absolute,    3, 4) },
            { 0xDD, new Opcode(CMP, AddressingMode.AbsoluteX,   3, 4, true) },
            { 0xD9, new Opcode(CMP, AddressingMode.AbsoluteY,   3, 4, true) },
            { 0xC1, new Opcode(CMP, AddressingMode.IndirectX,   2, 6) },
            { 0xD1, new Opcode(CMP, AddressingMode.IndirectY,   2, 5, true) },

            { 0xE0, new Opcode(CPX, AddressingMode.Immediate,   2, 2) },
            { 0xE4, new Opcode(CPX, AddressingMode.ZeroPage,    2, 3) },
            { 0xEC, new Opcode(CPX, AddressingMode.Absolute,    3, 4) },

            { 0xC0, new Opcode(CPY, AddressingMode.Immediate,   2, 2) },
            { 0xC4, new Opcode(CPY, AddressingMode.ZeroPage,    2, 3) },
            { 0xCC, new Opcode(CPY, AddressingMode.Absolute,    3, 4) },

            { 0xC6, new Opcode(DEC, AddressingMode.ZeroPage,    2, 5) },
            { 0xD6, new Opcode(DEC, AddressingMode.ZeroPageX,   2, 6) },
            { 0xCE, new Opcode(DEC, AddressingMode.Absolute,    3, 6) },
            { 0xDE, new Opcode(DEC, AddressingMode.AbsoluteX,   3, 7) },

            { 0xCA, new Opcode(DEX, AddressingMode.Implied,     1, 2) },

            { 0x88, new Opcode(DEY, AddressingMode.Implied,     1, 2) },

            { 0x49, new Opcode(EOR, AddressingMode.Immediate,   2, 2) },
            { 0x45, new Opcode(EOR, AddressingMode.ZeroPage,    2, 3) },
            { 0x55, new Opcode(EOR, AddressingMode.ZeroPageX,   2, 4) },
            { 0x4D, new Opcode(EOR, AddressingMode.Absolute,    3, 4) },
            { 0x5D, new Opcode(EOR, AddressingMode.AbsoluteX,   3, 4, true) },
            { 0x59, new Opcode(EOR, AddressingMode.AbsoluteY,   3, 4, true) },
            { 0x41, new Opcode(EOR, AddressingMode.IndirectX,   2, 6) },
            { 0x51, new Opcode(EOR, AddressingMode.IndirectY,   2, 5, true) },

            { 0xE6, new Opcode(INC, AddressingMode.ZeroPage,    2, 5) },
            { 0xF6, new Opcode(INC, AddressingMode.ZeroPageX,   2, 6) },
            { 0xEE, new Opcode(INC, AddressingMode.Absolute,    3, 6) },
            { 0xFE, new Opcode(INC, AddressingMode.AbsoluteX,   3, 7) },

            { 0xE8, new Opcode(INX, AddressingMode.Implied,     1, 2) },

            { 0xC8, new Opcode(INY, AddressingMode.Implied,     1, 2) },

            { 0x4C, new Opcode(JMP, AddressingMode.Absolute,    3, 3) },
            { 0x6C, new Opcode(JMP, AddressingMode.Indirect,    3, 5) },

            { 0x20, new Opcode(JSR, AddressingMode.Absolute,    3, 6) },

            { 0xA9, new Opcode(LDA, AddressingMode.Immediate,   2, 2) },
            { 0xA5, new Opcode(LDA, AddressingMode.ZeroPage,    2, 3) },
            { 0xB5, new Opcode(LDA, AddressingMode.ZeroPageX,   2, 4) },
            { 0xAD, new Opcode(LDA, AddressingMode.Absolute,    3, 4) },
            { 0xBD, new Opcode(LDA, AddressingMode.AbsoluteX,   3, 4, true) },
            { 0xB9, new Opcode(LDA, AddressingMode.AbsoluteY,   3, 4, true) },
            { 0xA1, new Opcode(LDA, AddressingMode.IndirectX,   2, 6) },
            { 0xB1, new Opcode(LDA, AddressingMode.IndirectY,   2, 5, true) },

            { 0xA2, new Opcode(LDX, AddressingMode.Immediate,   2, 2) },
            { 0xA6, new Opcode(LDX, AddressingMode.ZeroPage,    2, 3) },
            { 0xB6, new Opcode(LDX, AddressingMode.ZeroPageY,   2, 4) },
            { 0xAE, new Opcode(LDX, AddressingMode.Absolute,    3, 2) },
            { 0xBE, new Opcode(LDX, AddressingMode.AbsoluteY,   3, 4, true) },

            { 0xA0, new Opcode(LDY, AddressingMode.Immediate,   2, 2) },
            { 0xA4, new Opcode(LDY, AddressingMode.ZeroPage,    2, 3) },
            { 0xB4, new Opcode(LDY, AddressingMode.ZeroPageX,   2, 4) },
            { 0xAC, new Opcode(LDY, AddressingMode.Absolute,    3, 4) },
            { 0xBC, new Opcode(LDY, AddressingMode.AbsoluteX,   3, 4, true) },

            { 0x4A, new Opcode(LSR, AddressingMode.Accumulator, 1, 2) },
            { 0x46, new Opcode(LSR, AddressingMode.ZeroPage,    2, 5) },
            { 0x56, new Opcode(LSR, AddressingMode.ZeroPageX,   2, 6) },
            { 0x4E, new Opcode(LSR, AddressingMode.Absolute,    3, 6) },
            { 0x5E, new Opcode(LSR, AddressingMode.AbsoluteX,   3, 7) },

            { 0xEA, new Opcode(NOP, AddressingMode.Implied,     1, 2) },

            { 0x09, new Opcode(ORA, AddressingMode.Immediate,   2, 2) },
            { 0x05, new Opcode(ORA, AddressingMode.ZeroPage,    2, 3) },
            { 0x15, new Opcode(ORA, AddressingMode.ZeroPageX,   2, 4) },
            { 0x0D, new Opcode(ORA, AddressingMode.Absolute,    3, 4) },
            { 0x1D, new Opcode(ORA, AddressingMode.AbsoluteX,   3, 4, true) },
            { 0x19, new Opcode(ORA, AddressingMode.AbsoluteY,   3, 4, true) },
            { 0x01, new Opcode(ORA, AddressingMode.IndirectX,   2, 6) },
            { 0x11, new Opcode(ORA, AddressingMode.IndirectY,   2, 5, true) },

            { 0x48, new Opcode(PHA, AddressingMode.Implied,     1, 3) },

            { 0x08, new Opcode(PHP, AddressingMode.Implied,     1, 3) },

            { 0x68, new Opcode(PLA, AddressingMode.Implied,     1, 4) },

            { 0x28, new Opcode(PLP, AddressingMode.Implied,     1, 4) },

            { 0x2A, new Opcode(ROL, AddressingMode.Accumulator, 1, 2) },
            { 0x26, new Opcode(ROL, AddressingMode.ZeroPage,    2, 5) },
            { 0x36, new Opcode(ROL, AddressingMode.ZeroPageX,   2, 6) },
            { 0x2E, new Opcode(ROL, AddressingMode.Absolute,    3, 6) },
            { 0x3E, new Opcode(ROL, AddressingMode.AbsoluteX,   3, 7) },

            { 0x6A, new Opcode(ROR, AddressingMode.Accumulator, 1, 2) },
            { 0x66, new Opcode(ROR, AddressingMode.ZeroPage,    2, 5) },
            { 0x76, new Opcode(ROR, AddressingMode.ZeroPageX,   2, 6) },
            { 0x6E, new Opcode(ROR, AddressingMode.Absolute,    3, 6) },
            { 0x7E, new Opcode(ROR, AddressingMode.AbsoluteX,   3, 7) },

            { 0x40, new Opcode(RTI, AddressingMode.Implied,     1, 6) },

            { 0x60, new Opcode(RTS, AddressingMode.Implied,     1, 6) },

            { 0xE9, new Opcode(SBC, AddressingMode.Immediate,   2, 2) },
            { 0xE5, new Opcode(SBC, AddressingMode.ZeroPage,    2, 3) },
            { 0xF5, new Opcode(SBC, AddressingMode.ZeroPageX,   2, 4) },
            { 0xED, new Opcode(SBC, AddressingMode.Absolute,    3, 4) },
            { 0xFD, new Opcode(SBC, AddressingMode.AbsoluteX,   3, 4, true) },
            { 0xF9, new Opcode(SBC, AddressingMode.AbsoluteY,   3, 4, true) },
            { 0xE1, new Opcode(SBC, AddressingMode.IndirectX,   2, 6) },
            { 0xF1, new Opcode(SBC, AddressingMode.IndirectY,   2, 5, true) },

            { 0x38, new Opcode(SEC, AddressingMode.Implied,     1, 2) },

            { 0xF8, new Opcode(SED, AddressingMode.Implied,     1, 2) },

            { 0x78, new Opcode(SEI, AddressingMode.Implied,     1, 2) },

            { 0x85, new Opcode(STA, AddressingMode.ZeroPage,    2, 3) },
            { 0x95, new Opcode(STA, AddressingMode.ZeroPageX,   2, 4) },
            { 0x8D, new Opcode(STA, AddressingMode.Absolute,    3, 4) },
            { 0x9D, new Opcode(STA, AddressingMode.AbsoluteX,   3, 5) },
            { 0x99, new Opcode(STA, AddressingMode.AbsoluteY,   3, 5) },
            { 0x81, new Opcode(STA, AddressingMode.IndirectX,   2, 6) },
            { 0x91, new Opcode(STA, AddressingMode.IndirectY,   2, 6) },

            { 0x86, new Opcode(STX, AddressingMode.ZeroPage,    2, 3) },
            { 0x96, new Opcode(STX, AddressingMode.ZeroPageY,   2, 4) },
            { 0x8E, new Opcode(STX, AddressingMode.Absolute,    3, 4) },

            { 0x84, new Opcode(STY, AddressingMode.ZeroPage,    2, 3) },
            { 0x94, new Opcode(STY, AddressingMode.ZeroPageX,   2, 4) },
            { 0x8C, new Opcode(STY, AddressingMode.Absolute,    3, 4) },

            { 0xAA, new Opcode(TAX, AddressingMode.Implied,     1, 2) },

            { 0xA8, new Opcode(TAY, AddressingMode.Implied,     1, 2) },

            { 0xBA, new Opcode(TSX, AddressingMode.Implied,     1, 2) },

            { 0x8A, new Opcode(TXA, AddressingMode.Implied,     1, 2) },

            { 0x9A, new Opcode(TXS, AddressingMode.Implied,     1, 2) },

            { 0x98, new Opcode(TYA, AddressingMode.Implied,     1, 2) },
        });
    }

    public byte A { get; private set; }     // Accumulator
    public byte X { get; private set; }     // X Index
    public byte Y { get; private set; }     // Y Index
    public ushort PC { get; private set; }  // Program Counter
    public byte S { get; private set; }     // Stack Pointer
    public Status P { get; private set; }   // Processor Status

    // Memory
    private readonly byte[] internalRam;

    // Interrupts
    private bool nmiInterrupt;
    private bool irqInterrupt;

    public int TotalCycles { get; private set; }

    // DMA Transfer
    private int currentDmaCycle;
    private byte dmaPage;

    // Emulator
    private readonly NesEmulator emulator;

    public Cpu(NesEmulator emulator)
    {
        this.emulator = emulator;

        internalRam = new byte[InternalRamSize];

        opcodeLookup = InitializeOpcodes();

        PC = ReadMemoryWord(ResetVector);
        S = 0xFD;
        P = (Status)0x34;
    }

    public void Reset()
    {
        PC = ReadMemoryWord(ResetVector);
        S -= 3;

        SetStatusFlag(Status.InterruptDisabled, true);
    }

    private bool IsStatusFlagSet(Status flag)
    {
        return (P & flag) > 0;
    }

    private int GetStatusFlag(Status flag)
    {
        return IsStatusFlagSet(flag) ? 1 : 0;
    }

    private void SetStatusFlag(Status flag, bool value)
    {
        P = value ? P | flag : P & ~flag;
    }

    public int Step()
    {
        // OAMDMA Transfer
        if (currentDmaCycle > 0)
        {
            // Skip if on dummy cycle
            if (currentDmaCycle < TotalDmaCycles)
            {
                // Check if reading or writing
                if (TotalCycles % 2 == 0)
                {
                    emulator.ppu.OamData = ReadMemoryByte((ushort)((dmaPage << ByteLength) + emulator.ppu.OAMAddr));
                }
                else
                {
                    emulator.ppu.WriteOam(emulator.ppu.OamData);
                }
            }

            currentDmaCycle--;

            TotalCycles += DmaCycles;
            return DmaCycles;
        }

        // Interupts
        if (nmiInterrupt ||
           (irqInterrupt && IsStatusFlagSet(Status.InterruptDisabled)))
        {
            PushWordStack(PC);
            PushByteStack((byte)(P | Status.Bit5));

            SetStatusFlag(Status.InterruptDisabled, true);

            // NMI
            if (nmiInterrupt)
            {
                PC = ReadMemoryWord(NmiVector);

                nmiInterrupt = false;
            }
            // IRQ
            else
            {
                PC = ReadMemoryWord(IrqBrkVector);

                irqInterrupt = false;
            }

            TotalCycles += InterruptCycles;

            return InterruptCycles;
        }

        // Fetch
        var opcode = ReadMemoryByte(PC);

        // Decode
        var opcodeToExecute = opcodeLookup[opcode];

        // Execute
        var cycles = opcodeToExecute.Instruction(opcodeToExecute.Mode,
                                                  opcodeToExecute.OpcodeSize,
                                                  opcodeToExecute.Cycles,
                                                  opcodeToExecute.OopsCycle);

        TotalCycles += cycles;

        return cycles;
    }

    public void StartOamDma(byte page)
    {
        dmaPage = page;

        currentDmaCycle = TotalDmaCycles + (TotalCycles % 2); // Add one dummy cycle if odd
    }

    public void RaiseNmi() => nmiInterrupt = true;

    /// <summary>
    /// Add With Carry
    /// </summary>
    private byte ADC(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out bool crossedPage, oopsCycle);

        var origA = A;
        var carry = GetStatusFlag(Status.Carry);
        var result = A + value + carry;

        A = (byte)result;

        CheckZero(A);
        CheckCarry(result);
        CheckOverflow(origA, value, result);
        CheckNegative(A);

        PC += opcodeSize;

        if (crossedPage)
        {
            cycles++;
        }

        return cycles;
    }

    /// <summary>
    /// Logical AND
    /// </summary>
    private byte AND(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out bool crossedPage, oopsCycle);

        A &= value;

        CheckZero(A);
        CheckNegative(A);

        PC += opcodeSize;

        if (crossedPage)
        {
            cycles++;
        }

        return cycles;
    }

    /// <summary>
    /// Arithmetic Shift Left
    /// </summary>
    private byte ASL(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out _);

        var carry = value.IsBitSet(NegativeBit);
        SetStatusFlag(Status.Carry, carry);

        value <<= 1;

        WriteValue(mode, value);

        CheckZero(value);
        CheckNegative(value);

        PC += opcodeSize;

        return cycles;
    }

    private byte Branch(byte opcodeSize, byte cycles, bool condition)
    {
        if (condition)
        {
            cycles++;

            var oldPC = PC;

            sbyte offset = (sbyte)ReadOperandByte();
            PC = (ushort)(PC + offset);

            if (!IsSamePage(PC, oldPC))
            {
                cycles++;
            }
        }

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Branch if Carry Clear
    /// </summary>
    private byte BCC(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var carry = IsStatusFlagSet(Status.Carry);

        return Branch(opcodeSize, cycles, !carry);
    }

    /// <summary>
    /// Branch if Carry Set
    /// </summary>
    private byte BCS(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var carry = IsStatusFlagSet(Status.Carry);

        return Branch(opcodeSize, cycles, carry);
    }

    /// <summary>
    /// Branch if Equal
    /// </summary>
    private byte BEQ(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var zero = IsStatusFlagSet(Status.Zero);

        return Branch(opcodeSize, cycles, zero);
    }

    /// <summary>
    /// Bit Test
    /// </summary>
    private byte BIT(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out _);

        byte result = (byte)(value & A);
        CheckZero(result);

        SetStatusFlag(Status.Overflow, value.IsBitSet(6));

        SetStatusFlag(Status.Negative, value.IsBitSet(7));

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Branch if Minus
    /// </summary>
    private byte BMI(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var negative = IsStatusFlagSet(Status.Negative);

        return Branch(opcodeSize, cycles, negative);
    }

    /// <summary>
    /// Branch if Not Equal
    /// </summary>
    private byte BNE(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var zero = IsStatusFlagSet(Status.Zero);

        return Branch(opcodeSize, cycles, !zero);
    }

    /// <summary>
    /// Branch if Positive
    /// </summary>
    private byte BPL(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var negative = IsStatusFlagSet(Status.Negative);

        return Branch(opcodeSize, cycles, !negative);
    }

    /// <summary>
    /// Force Interrupt
    /// </summary>
    private byte BRK(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        PushWordStack(PC);

        byte status = (byte)(P | Status.Bit5 | Status.Bit4);
        PushByteStack(status);

        PC = ReadMemoryWord(IrqBrkVector);

        return cycles;
    }

    /// <summary>
    ///  Brach if Overflow Clear
    /// </summary>
    private byte BVC(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var overflow = IsStatusFlagSet(Status.Overflow);

        return Branch(opcodeSize, cycles, !overflow);
    }

    /// <summary>
    /// Branch if Overflow Set
    /// </summary>
    private byte BVS(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var overflow = IsStatusFlagSet(Status.Overflow);

        return Branch(opcodeSize, cycles, overflow);
    }

    /// <summary>
    /// Clear Carry Flag
    /// </summary>
    private byte CLC(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        SetStatusFlag(Status.Carry, false);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Clear Decimal Mode
    /// </summary>
    private byte CLD(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        SetStatusFlag(Status.DecimalMode, false);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Clear Interrupte Disable
    /// </summary>
    private byte CLI(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        SetStatusFlag(Status.InterruptDisabled, false);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Clear Overflow Flag
    /// </summary>
    private byte CLV(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        SetStatusFlag(Status.Overflow, false);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Compare
    /// </summary>
    private byte CMP(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out bool crossedPage, oopsCycle);

        var result = A - value;

        CheckZero((byte)result);

        SetStatusFlag(Status.Carry, result >= 0);

        CheckNegative((byte)result);

        PC += opcodeSize;

        if (crossedPage)
        {
            cycles++;
        }

        return cycles;
    }

    /// <summary>
    /// Compare X Register
    /// </summary>
    private byte CPX(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out _);

        var result = X - value;

        CheckZero((byte)result);

        SetStatusFlag(Status.Carry, result >= 0);

        CheckNegative((byte)result);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Compare Y Register
    /// </summary>
    private byte CPY(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out _);

        var result = Y - value;

        CheckZero((byte)result);

        SetStatusFlag(Status.Carry, result >= 0);

        CheckNegative((byte)result);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Decrement Memory
    /// </summary>
    private byte DEC(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out bool crossedPage, oopsCycle);

        value--;

        WriteValue(mode, value);

        CheckZero(value);
        CheckNegative(value);

        PC += opcodeSize;

        if (crossedPage)
        {
            cycles++;
        }

        return cycles;
    }

    /// <summary>
    /// Decrement X Memory
    /// </summary>
    private byte DEX(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        X--;

        CheckZero(X);
        CheckNegative(X);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Decrement Y Memory
    /// </summary>
    private byte DEY(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        Y--;

        CheckZero(Y);
        CheckNegative(Y);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Exclusive OR
    /// </summary>
    private byte EOR(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out bool crossedPage, oopsCycle);

        A ^= value;

        CheckZero(A);
        CheckNegative(A);

        PC += opcodeSize;

        if (crossedPage)
        {
            cycles++;
        }

        return cycles;
    }

    /// <summary>
    /// Increment Memory
    /// </summary>
    private byte INC(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out _);

        value++;

        WriteValue(mode, value);

        CheckZero(value);
        CheckNegative(value);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Increment X Register
    /// </summary>
    private byte INX(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        X++;

        CheckZero(X);
        CheckNegative(X);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Incrememt Y Register
    /// </summary>
    private byte INY(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        Y++;

        CheckZero(Y);
        CheckNegative(Y);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Jump
    /// </summary>
    private byte JMP(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadOperandWord();

        if (mode == AddressingMode.Indirect)
        {
            // Hardware bug
            if ((value & LowByteMask) == 0xFF)
            {
                var lo = ReadMemoryByte(value);
                var hi = ReadMemoryByte((ushort)(value & HighByteMask));

                value = (ushort)(hi << ByteLength | lo);
            }
            else
            {
                value = ReadMemoryWord(value);
            }
        }

        PC = value;

        return cycles;
    }

    /// <summary>
    /// Jump to Subroutine
    /// </summary>
    private byte JSR(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        ushort address = (ushort)(PC + opcodeSize - 1);

        PushWordStack(address);

        PC = ReadOperandWord();

        return cycles;
    }

    /// <summary>
    /// Load Accumulator
    /// </summary>
    private byte LDA(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out bool crossedPage, oopsCycle);

        A = value;

        CheckZero(A);
        CheckNegative(A);

        PC += opcodeSize;

        if (crossedPage)
        {
            cycles++;
        }

        return cycles;
    }

    /// <summary>
    /// Load X Register
    /// </summary>
    private byte LDX(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out bool crossedPage, oopsCycle);

        X = value;

        CheckZero(X);
        CheckNegative(X);

        PC += opcodeSize;

        if (crossedPage)
        {
            cycles++;
        }

        return cycles;
    }

    /// <summary>
    /// Load Y Register
    /// </summary>
    private byte LDY(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out bool crossedPage, oopsCycle);

        Y = value;

        CheckZero(Y);
        CheckNegative(Y);

        PC += opcodeSize;

        if (crossedPage)
        {
            cycles++;
        }

        return cycles;
    }

    /// <summary>
    /// Logical Shift Right
    /// </summary>
    private byte LSR(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out _);

        SetStatusFlag(Status.Carry, value.IsBitSet(0));

        value >>= 1;

        WriteValue(mode, value);

        CheckZero(value);
        CheckNegative(value);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// No Operation
    /// </summary>
    private byte NOP(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Logical Inclusive OR
    /// </summary>
    private byte ORA(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out bool crossedPage, oopsCycle);

        A |= value;

        CheckZero(A);
        CheckNegative(A);

        PC += opcodeSize;

        if (crossedPage)
        {
            cycles++;
        }

        return cycles;
    }

    /// <summary>
    /// Push Accumulator
    /// </summary>
    private byte PHA(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        PushByteStack(A);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Push Processor Stack
    /// </summary>
    private byte PHP(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        byte stack = (byte)(P | Status.Bit5 | Status.Bit4);
        PushByteStack(stack);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Pull Accumulator
    /// </summary>
    private byte PLA(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        A = PullByteStack();

        CheckZero(A);
        CheckNegative(A);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Pull Processor Stack
    /// </summary>
    private byte PLP(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        P = ((Status)PullByteStack() & ~(Status.Bit5 | Status.Bit4)) | (P & (Status.Bit4 | Status.Bit5));

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Rotate Left
    /// </summary>
    private byte ROL(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out _);

        var oldCarry = GetStatusFlag(Status.Carry);

        SetStatusFlag(Status.Carry, value.IsBitSet(NegativeBit));

        value <<= 1;
        value |= (byte)oldCarry;

        WriteValue(mode, value);

        CheckZero(A);
        CheckNegative(value);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Rotate Right
    /// </summary>
    private byte ROR(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out _);

        var oldCarry = GetStatusFlag(Status.Carry);

        SetStatusFlag(Status.Carry, value.IsBitSet(0));

        value >>= 1;
        value |= (byte)(oldCarry << NegativeBit);

        WriteValue(mode, value);

        CheckZero(A);
        CheckNegative(value);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Return from Interrupt
    /// </summary>
    private byte RTI(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        P = ((Status)PullByteStack() & ~(Status.Bit5 | Status.Bit4)) |
            (P & (Status.Bit4 | Status.Bit5));

        PC = PullWordStack();

        return cycles;
    }

    /// <summary>
    /// Return from Subroutine
    /// </summary>
    private byte RTS(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        ushort address = PullWordStack();

        PC = (ushort)(address + 1);

        return cycles;
    }

    /// <summary>
    /// Subract with Carry
    /// </summary>
    private byte SBC(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        var value = ReadValue(mode, out bool crossedPage, oopsCycle);
        value = (byte)~value;

        var origA = A;
        var carry = GetStatusFlag(Status.Carry);
        var result = A + value + carry;

        A = (byte)result;

        CheckZero(A);
        CheckCarry(result);
        CheckOverflow(origA, value, result);
        CheckNegative(A);

        PC += opcodeSize;

        if (crossedPage)
        {
            cycles++;
        }

        return cycles;
    }

    /// <summary>
    /// Set Carry Flag
    /// </summary>
    private byte SEC(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        SetStatusFlag(Status.Carry, true);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Set Decimal Flag
    /// </summary>
    private byte SED(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        SetStatusFlag(Status.DecimalMode, true);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Set Interrupt Disable
    /// </summary>
    private byte SEI(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        SetStatusFlag(Status.InterruptDisabled, true);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Store Accumulator
    /// </summary>
    private byte STA(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        WriteValue(mode, A);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Store X Register
    /// </summary>
    private byte STX(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        WriteValue(mode, X);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Store Y Register
    /// </summary>
    private byte STY(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        WriteValue(mode, Y);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Transfer Accumulator to X
    /// </summary>
    private byte TAX(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        X = A;

        CheckZero(X);
        CheckNegative(X);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Transfer Accumulator to Y
    /// </summary>
    private byte TAY(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        Y = A;

        CheckZero(Y);
        CheckNegative(Y);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Transfer Stack Pointer to X
    /// </summary>
    private byte TSX(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        X = S;

        CheckZero(X);
        CheckNegative(X);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Transfer X to Accumulator
    /// </summary>
    private byte TXA(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        A = X;

        CheckZero(A);
        CheckNegative(A);

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Transfer X to Stack Pointer
    /// </summary>
    private byte TXS(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        S = X;

        PC += opcodeSize;

        return cycles;
    }

    /// <summary>
    /// Transfer Y to Accumulator
    /// </summary>
    private byte TYA(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
    {
        A = Y;

        PC += opcodeSize;

        return cycles;
    }

    private void CheckZero(byte result)
    {
        SetStatusFlag(Status.Zero, result == 0);
    }

    private void CheckCarry(int result)
    {
        SetStatusFlag(Status.Carry, (result >> ByteLength) != 0);
    }

    private void CheckOverflow(byte value1, byte value2, int result)
    {
        SetStatusFlag(Status.Overflow, (value1.IsBitSet(NegativeBit) == value2.IsBitSet(NegativeBit)) &&
                                       (value1.IsBitSet(NegativeBit) != result.IsBitSet(NegativeBit)));
    }

    private void CheckNegative(byte result)
    {
        SetStatusFlag(Status.Negative, result.IsBitSet(NegativeBit));
    }

    private static bool IsSamePage(ushort address1, ushort address2)
    {
        return ((address1 ^ address2) & HighByteMask) == 0;
    }

    private byte ReadValue(AddressingMode mode, out bool crossedPage, bool oopsCycle = false)
    {
        ushort address;
        ushort addressWithoutRegister;
        switch (mode)
        {
            case AddressingMode.Accumulator:
                crossedPage = false;
                return A;

            case AddressingMode.Immediate:
                crossedPage = false;
                return ReadOperandByte();

            case AddressingMode.ZeroPage:
                address = (ushort)(ReadOperandByte() & LowByteMask);
                crossedPage = false;
                return ReadMemoryByte(address);

            case AddressingMode.ZeroPageX:
                address = (ushort)((ReadOperandByte() + X) & LowByteMask);
                crossedPage = false;
                return ReadMemoryByte(address);

            case AddressingMode.ZeroPageY:
                address = (ushort)((ReadOperandByte() + Y) & LowByteMask);
                crossedPage = false;
                return ReadMemoryByte(address);

            case AddressingMode.Absolute:
                address = ReadOperandWord();
                crossedPage = false;
                return ReadMemoryByte(address);

            case AddressingMode.AbsoluteX:
                addressWithoutRegister = ReadOperandWord();
                address = (ushort)(addressWithoutRegister + X);
                crossedPage = oopsCycle && !IsSamePage(address, addressWithoutRegister);
                return ReadMemoryByte(address);

            case AddressingMode.AbsoluteY:
                addressWithoutRegister = ReadOperandWord();
                address = (ushort)(addressWithoutRegister + Y);
                crossedPage = oopsCycle && !IsSamePage(address, addressWithoutRegister);
                return ReadMemoryByte(address);

            case AddressingMode.IndirectX:
                var lo = ReadMemoryByte((ushort)((ReadOperandByte() + X) & LowByteMask));
                var hi = ReadMemoryByte((ushort)((ReadOperandByte() + X + 1) & LowByteMask));
                address = (ushort)(hi << ByteLength | lo);
                crossedPage = false;
                return ReadMemoryByte(address);

            case AddressingMode.IndirectY:
                lo = ReadMemoryByte(ReadOperandByte());
                hi = ReadMemoryByte((ushort)((ReadOperandByte() + 1) & LowByteMask));
                addressWithoutRegister = (ushort)(hi << ByteLength | lo);
                address = (ushort)(addressWithoutRegister + Y);
                crossedPage = oopsCycle && !IsSamePage(address, addressWithoutRegister);
                return ReadMemoryByte(address);

            default:
                crossedPage = false;
                return 0;
        }
    }

    private byte ReadOperandByte()
    {
        return ReadMemoryByte((ushort)(PC + 1));
    }

    private ushort ReadOperandWord()
    {
        return ReadMemoryWord((ushort)(PC + 1));
    }

    private void WriteValue(AddressingMode mode, byte value)
    {
        ushort address;
        switch (mode)
        {
            case AddressingMode.Accumulator:
                A = value;
                break;

            case AddressingMode.ZeroPage:
                address = ReadOperandByte();
                WriteMemoryByte(address, value);
                break;

            case AddressingMode.ZeroPageX:
                address = (ushort)((ReadOperandByte() + X) & LowByteMask);
                WriteMemoryByte(address, value);
                break;

            case AddressingMode.ZeroPageY:
                address = (ushort)((ReadOperandByte() + Y) & LowByteMask);
                WriteMemoryByte(address, value);
                break;

            case AddressingMode.Absolute:
                address = ReadOperandWord();
                WriteMemoryByte(address, value);
                break;

            case AddressingMode.AbsoluteX:
                address = (ushort)(ReadOperandWord() + X);
                WriteMemoryByte(address, value);
                break;

            case AddressingMode.AbsoluteY:
                address = (ushort)(ReadOperandWord() + Y);
                WriteMemoryByte(address, value);
                break;

            case AddressingMode.IndirectX:
                var lo = ReadMemoryByte((ushort)((ReadOperandByte() + X) & LowByteMask));
                var hi = ReadMemoryByte((ushort)((ReadOperandByte() + X + 1) & LowByteMask));
                address = (ushort)(hi << 8 | lo);
                WriteMemoryByte(address, value);
                break;

            case AddressingMode.IndirectY:
                lo = ReadMemoryByte(ReadOperandByte());
                hi = ReadMemoryByte((ushort)((ReadOperandByte() + 1) & LowByteMask));
                address = (ushort)((hi << ByteLength | lo) + Y);
                WriteMemoryByte(address, value);
                break;

            default:
                break;
        }
    }

    public byte ReadMemoryByte(ushort address)
    {
        byte value = default;

        if (address <= InternalCpuRamAddressEnd)
        {
            value = internalRam[GetInternalRamAddress(address)];
        }
        else if (address <= PpuRegistersAddressEnd)
        {
            value = emulator.ppu.ReadRegister(GetPPUAddress(address));
        }
        else if (address <= ApuIORegistersAddressEnd)
        {
            if (address == Controller.AddressOne)
            {
                value = emulator.controllerOne.Read();
            }
            else if (address == Controller.AddressTwo)
            {
                value = emulator.controllerTwo.Read();
            }
        }
        else if (address <= TestModeAddressEnd)
        {
            // Not Used
        }
        else if (address >= CartridgeAddress)
        {
            value = emulator.Mapper.Read(address);
        }

        return value;
    }

    private ushort ReadMemoryWord(ushort address)
    {
        var lo = ReadMemoryByte(address++);
        var hi = ReadMemoryByte(address);

        return (ushort)((hi << ByteLength) | lo);
    }

    private void WriteMemoryByte(ushort address, byte value)
    {
        if (address <= InternalCpuRamAddressEnd)
        {
            internalRam[GetInternalRamAddress(address)] = value;
        }
        else if (address <= PpuRegistersAddressEnd)
        {
            emulator.ppu.WriteRegister(GetPPUAddress(address), value);
        }
        else if (address <= ApuIORegistersAddressEnd)
        {
            if (address == Ppu.OamDmaAddress)
            {
                emulator.ppu.WriteRegister(GetPPUAddress(address), value);
            }
        }
        else if (address <= TestModeAddressEnd)
        {
            // Not Used
        }
        else if (address >= CartridgeAddress)
        {
            emulator.Mapper.Write(address, value);
        }
    }

    private static ushort GetInternalRamAddress(ushort address)
    {
        return (ushort)(address % InternalRamSize);
    }

    private static ushort GetPPUAddress(ushort address)
    {
        return (ushort)(((address - PpuRegistersAddressStart) % PpuRegistersSize) + PpuRegistersAddressStart);
    }

    private void PushByteStack(byte value)
    {
        WriteMemoryByte((ushort)(StackOffset | S), value);

        S -= 1;
    }

    private void PushWordStack(ushort value)
    {
        var address = (ushort)(StackOffset | S - 1);

        WriteMemoryByte(address++, (byte)value);
        WriteMemoryByte(address, (byte)(value >> ByteLength));

        S -= 2;
    }

    private byte PullByteStack()
    {
        S += 1;

        return ReadMemoryByte((ushort)(StackOffset | S));
    }

    private ushort PullWordStack()
    {
        S += 2;

        return ReadMemoryWord((ushort)(StackOffset | S - 1));
    }
}
