using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

using static Nes.Constants;

namespace Nes
{
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

        public class Opcode
        {
            public Func<AddressingMode, byte, byte, bool, byte> Instruction;
            public readonly AddressingMode Mode;
            public readonly byte OpcodeSize;
            public readonly byte Cycles;
            public readonly bool OopsCycle;

            public Opcode(Func<AddressingMode, byte, byte, bool, byte> instruction,
                          AddressingMode mode,
                          byte opcodeSize,
                          byte cycles,
                          bool oopsCycle = false)
            {
                Instruction = instruction;
                Mode = mode;
                OpcodeSize = opcodeSize;
                Cycles = cycles;
                OopsCycle = oopsCycle;
            }
        }

        // Opcodes
        public ReadOnlyDictionary<byte, Opcode> OpcodeLookup { get; private set; }

        private void InitializeOpcodes()
        {
            OpcodeLookup = new ReadOnlyDictionary<byte, Opcode>(new Dictionary<byte, Opcode>
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
                { 0xAE, new Opcode(LDX, AddressingMode.Absolute,    3, 4) },
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

        // Clock Speed
        public readonly int ClockSpeed;
        private const int NtscClockSpeed = 1789773; // Hz

        // Reset
        private const Status StatusReset = (Status)0x24;

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

        // Memory
        private readonly byte[] _internalRam;

        // Emulator
        private readonly Emulator _emulator;

        // Registers
        private byte _a;      // Accumulator
        public byte A => _a;

        private byte _x;      // X Index
        public byte X => _x;

        private byte _y;      // Y Index
        public byte Y => _y;

        private ushort _pc;   // Program Counter
        public ushort Pc => _pc;

        private byte _s;      // Stack Pointer
        public byte S => _s;

        private Status _p;    // Processor Status
        public Status P => _p;

        // State
        private int _totalCycles;
        public int TotalCycles => _totalCycles;

        // Interrupts
        private bool _nmiInterrupt;
        private bool _irqInterrupt;

        // DMA Transfer
        private int _currentDmaCycle;
        private byte _dmaPage;
        private byte _dmaIndex;

        public Cpu(Emulator emulator)
        {
            _emulator = emulator;

            ClockSpeed = NtscClockSpeed;

            InitializeOpcodes();

            _internalRam = new byte[InternalRamSize];

            _p = StatusReset;

            Reset();

            // Nestest
            //_pc = 0xC000;
        }

        public void Reset()
        {
            _pc = ReadMemoryWord(ResetVector);
            _s -= 3;

            SetStatusFlag(Status.InterruptDisabled, true);

            _totalCycles = 7;
        }

        public int Step()
        {
            // OAMDMA Transfer
            if (_currentDmaCycle > 0)
            {
                // Skip if on dummy cycle
                if (_currentDmaCycle < TotalDmaCycles)
                {
                    // Read
                    if (_currentDmaCycle % 2 == 0)
                    {
                        _emulator.Ppu.OamData = ReadMemoryByte((ushort)((_dmaPage << ByteLength) + _dmaIndex++));
                    }
                    // Write
                    else
                    {
                        _emulator.Ppu.WriteOam(_emulator.Ppu.OamData);
                    }
                }

                _currentDmaCycle--;

                _totalCycles += DmaCycles;
                return DmaCycles;
            }

            // Interupts
            if (_nmiInterrupt ||
               (_irqInterrupt && !IsStatusFlagSet(Status.InterruptDisabled)))
            {
                PushWordStack(_pc);
                PushByteStack((byte)((_p & ~Status.Bit4) | Status.Bit5));

                SetStatusFlag(Status.InterruptDisabled, true);

                // NMI
                if (_nmiInterrupt)
                {
                    _pc = ReadMemoryWord(NmiVector);

                    _nmiInterrupt = false;
                }
                // IRQ
                else
                {
                    _pc = ReadMemoryWord(IrqBrkVector);

                    _irqInterrupt = false;
                }

                _totalCycles += InterruptCycles;

                return InterruptCycles;
            }

            // Fetch
            var opcode = ReadMemoryByte(_pc);

            // Decode
            if (!OpcodeLookup.TryGetValue(opcode, out var opcodeToExecute))
            {
                // Illegal opcode
                // Treat as NOP
                _pc++;
                _totalCycles += 2;
                return 2;
            }

            if(opcodeToExecute.OpcodeSize == 1)
            {
                ReadMemoryByte((ushort)(_pc + 1)); // Dummy read of next opcode
            }

            // Execute
            var cycles = opcodeToExecute.Instruction(opcodeToExecute.Mode,
                                                     opcodeToExecute.OpcodeSize,
                                                     opcodeToExecute.Cycles,
                                                     opcodeToExecute.OopsCycle);

            _totalCycles += cycles;

            return cycles;
        }

        public void StartOamDma(byte page)
        {
            _dmaPage = page;

            _currentDmaCycle = TotalDmaCycles + _totalCycles % 2; // Add one dummy cycle if odd

            _dmaIndex = 0;
        }

        public void RaiseNmi() => _nmiInterrupt = true;

        private bool IsStatusFlagSet(Status flag) => (_p & flag) > 0;

        private byte GetStatusFlag(Status flag) => (byte)(IsStatusFlagSet(flag) ? 1 : 0);

        private void SetStatusFlag(Status flag, bool value) => _p = value ? (_p | flag) : (_p & ~flag);

        /// <summary>
        /// Add With Carry
        /// </summary>
        private byte ADC(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            var value = ReadValue(mode, out bool crossedPage, oopsCycle);

            var origA = _a;
            var carry = GetStatusFlag(Status.Carry);
            var result = _a + value + carry;

            _a = (byte)result;

            CheckCarry(result);
            CheckZero(_a);
            CheckOverflow(origA, value, result);
            CheckNegative(_a);

            _pc += opcodeSize;

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

            _a &= value;

            CheckZero(_a);
            CheckNegative(_a);

            _pc += opcodeSize;

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

            _pc += opcodeSize;

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

            byte result = (byte)(value & _a);

            CheckZero(result);

            SetStatusFlag(Status.Overflow, value.IsBitSet(6));
            SetStatusFlag(Status.Negative, value.IsBitSet(7));

            _pc += opcodeSize;

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
            PushWordStack((ushort)(_pc + opcodeSize + 1));

            byte status = (byte)(_p | Status.Bit5 | Status.Bit4);
            PushByteStack(status);

            SetStatusFlag(Status.InterruptDisabled, true);

            _pc = ReadMemoryWord(IrqBrkVector);

            return cycles;
        }

        /// <summary>
        /// Brach if Overflow Clear
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

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Clear Decimal Mode
        /// </summary>
        private byte CLD(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            SetStatusFlag(Status.DecimalMode, false);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Clear Interrupte Disable
        /// </summary>
        private byte CLI(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            SetStatusFlag(Status.InterruptDisabled, false);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Clear Overflow Flag
        /// </summary>
        private byte CLV(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            SetStatusFlag(Status.Overflow, false);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Compare
        /// </summary>
        private byte CMP(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            var value = ReadValue(mode, out bool crossedPage, oopsCycle);

            var result = _a - value;

            SetStatusFlag(Status.Carry, result >= 0);

            CheckZero((byte)result);
            CheckNegative((byte)result);

            _pc += opcodeSize;

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

            var result = _x - value;

            SetStatusFlag(Status.Carry, result >= 0);

            CheckZero((byte)result);
            CheckNegative((byte)result);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Compare Y Register
        /// </summary>
        private byte CPY(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            var value = ReadValue(mode, out _);

            var result = _y - value;

            SetStatusFlag(Status.Carry, result >= 0);

            CheckZero((byte)result);
            CheckNegative((byte)result);

            _pc += opcodeSize;

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

            _pc += opcodeSize;

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
            _x--;

            CheckZero(_x);
            CheckNegative(_x);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Decrement Y Memory
        /// </summary>
        private byte DEY(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            _y--;

            CheckZero(_y);
            CheckNegative(_y);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Exclusive OR
        /// </summary>
        private byte EOR(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            var value = ReadValue(mode, out bool crossedPage, oopsCycle);

            _a ^= value;

            CheckZero(_a);
            CheckNegative(_a);

            _pc += opcodeSize;

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

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Increment X Register
        /// </summary>
        private byte INX(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            _x++;

            CheckZero(_x);
            CheckNegative(_x);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Incrememt Y Register
        /// </summary>
        private byte INY(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            _y++;

            CheckZero(_y);
            CheckNegative(_y);

            _pc += opcodeSize;

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
                // Hardware bug if the address would cross a page
                if ((value & LowByteMask) == 0xFF)
                {
                    var lo = ReadMemoryByte(value);
                    var hi = ReadMemoryByte((ushort)(value & HighByteMask)); // Does not increment the page before reading

                    value = (ushort)(hi << ByteLength | lo);
                }
                else
                {
                    value = ReadMemoryWord(value);
                }
            }

            _pc = value;

            return cycles;
        }

        /// <summary>
        /// Jump to Subroutine
        /// </summary>
        private byte JSR(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            ushort address = (ushort)(_pc + opcodeSize - 1);

            PushWordStack(address);

            _pc = ReadOperandWord();

            return cycles;
        }

        /// <summary>
        /// Load Accumulator
        /// </summary>
        private byte LDA(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            var value = ReadValue(mode, out bool crossedPage, oopsCycle);

            _a = value;

            CheckZero(_a);
            CheckNegative(_a);

            _pc += opcodeSize;

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

            _x = value;

            CheckZero(_x);
            CheckNegative(_x);

            _pc += opcodeSize;

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

            _y = value;

            CheckZero(_y);
            CheckNegative(_y);

            _pc += opcodeSize;

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

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// No Operation
        /// </summary>
        private byte NOP(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Logical Inclusive OR
        /// </summary>
        private byte ORA(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            var value = ReadValue(mode, out bool crossedPage, oopsCycle);

            _a |= value;

            CheckZero(_a);
            CheckNegative(_a);

            _pc += opcodeSize;

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
            PushByteStack(_a);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Push Processor Stack
        /// </summary>
        private byte PHP(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            byte stack = (byte)(_p | Status.Bit5 | Status.Bit4);
            PushByteStack(stack);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Pull Accumulator
        /// </summary>
        private byte PLA(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            _a = PullByteStack();

            CheckZero(_a);
            CheckNegative(_a);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Pull Processor Stack
        /// </summary>
        private byte PLP(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            _p = (Status)((PullByteStack() & ~(byte)(Status.Bit5 | Status.Bit4)) | (byte)Status.Bit5);

            _pc += opcodeSize;

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

            CheckZero(value);
            CheckNegative(value);

            _pc += opcodeSize;

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

            CheckZero(value);
            CheckNegative(value);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Return from Interrupt
        /// </summary>
        private byte RTI(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            _p = (Status)((PullByteStack() & ~(byte)(Status.Bit5 | Status.Bit4)) | (byte)Status.Bit5);

            _pc = PullWordStack();

            return cycles;
        }

        /// <summary>
        /// Return from Subroutine
        /// </summary>
        private byte RTS(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            ushort address = PullWordStack();

            _pc = (ushort)(address + 1);

            return cycles;
        }

        /// <summary>
        /// Subract with Carry
        /// </summary>
        private byte SBC(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            var value = ReadValue(mode, out bool crossedPage, oopsCycle);
            value = (byte)~value;

            var origA = _a;
            var carry = GetStatusFlag(Status.Carry);
            var result = _a + value + carry;

            _a = (byte)result;

            CheckCarry(result);
            CheckZero(_a);
            CheckOverflow(origA, value, result);
            CheckNegative(_a);

            _pc += opcodeSize;

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

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Set Decimal Flag
        /// </summary>
        private byte SED(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            SetStatusFlag(Status.DecimalMode, true);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Set Interrupt Disable
        /// </summary>
        private byte SEI(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            SetStatusFlag(Status.InterruptDisabled, true);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Store Accumulator
        /// </summary>
        private byte STA(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            WriteValue(mode, _a);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Store X Register
        /// </summary>
        private byte STX(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            WriteValue(mode, _x);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Store Y Register
        /// </summary>
        private byte STY(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            WriteValue(mode, _y);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Transfer Accumulator to X
        /// </summary>
        private byte TAX(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            _x = _a;

            CheckZero(_x);
            CheckNegative(_x);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Transfer Accumulator to Y
        /// </summary>
        private byte TAY(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            _y = _a;

            CheckZero(_y);
            CheckNegative(_y);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Transfer Stack Pointer to X
        /// </summary>
        private byte TSX(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            _x = _s;

            CheckZero(_x);
            CheckNegative(_x);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Transfer X to Accumulator
        /// </summary>
        private byte TXA(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            _a = _x;

            CheckZero(_a);
            CheckNegative(_a);

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Transfer X to Stack Pointer
        /// </summary>
        private byte TXS(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            _s = _x;

            _pc += opcodeSize;

            return cycles;
        }

        /// <summary>
        /// Transfer Y to Accumulator
        /// </summary>
        private byte TYA(AddressingMode mode, byte opcodeSize, byte cycles, bool oopsCycle)
        {
            _a = _y;

            CheckZero(_a);
            CheckNegative(_a);

            _pc += opcodeSize;

            return cycles;
        }

        private byte Branch(byte opcodeSize, byte cycles, bool condition)
        {
            if (condition)
            {
                var originalPc = _pc;

                sbyte offset = (sbyte)ReadOperandByte();

                _pc = (ushort)(_pc + opcodeSize + offset);

                if (!IsSamePage(_pc, (ushort)(originalPc + opcodeSize)))
                {
                    cycles++;
                }

                cycles++;
            }
            else
            {
                _pc += opcodeSize;
            }

            return cycles;
        }
        private void CheckCarry(int result)
        {
            SetStatusFlag(Status.Carry, result >> ByteLength != 0);
        }

        private void CheckZero(byte result)
        {
            SetStatusFlag(Status.Zero, result == 0);
        }

        private void CheckOverflow(byte value1, byte value2, int result)
        {
            SetStatusFlag(Status.Overflow, value1.IsBitSet(NegativeBit) == value2.IsBitSet(NegativeBit) &&
                                           value1.IsBitSet(NegativeBit) != result.IsBitSet(NegativeBit));
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
            byte baseAddress;
            ushort addressWithoutRegister;
            switch (mode)
            {
                case AddressingMode.Accumulator:
                    crossedPage = false;
                    return _a;

                case AddressingMode.Immediate:
                    crossedPage = false;
                    return ReadOperandByte();

                case AddressingMode.ZeroPage:
                    address = (ushort)(ReadOperandByte() & LowByteMask);
                    crossedPage = false;
                    return ReadMemoryByte(address);

                case AddressingMode.ZeroPageX:
                    address = (ushort)(ReadOperandByte() + _x & LowByteMask);
                    crossedPage = false;
                    return ReadMemoryByte(address);

                case AddressingMode.ZeroPageY:
                    address = (ushort)(ReadOperandByte() + _y & LowByteMask);
                    crossedPage = false;
                    return ReadMemoryByte(address);

                case AddressingMode.Absolute:
                    address = ReadOperandWord();
                    crossedPage = false;
                    return ReadMemoryByte(address);

                case AddressingMode.AbsoluteX:
                    addressWithoutRegister = ReadOperandWord();
                    address = (ushort)(addressWithoutRegister + _x);
                    crossedPage = oopsCycle && !IsSamePage(address, addressWithoutRegister);
                    return ReadMemoryByte(address);

                case AddressingMode.AbsoluteY:
                    addressWithoutRegister = ReadOperandWord();
                    address = (ushort)(addressWithoutRegister + _y);
                    crossedPage = oopsCycle && !IsSamePage(address, addressWithoutRegister);
                    return ReadMemoryByte(address);

                case AddressingMode.IndirectX:
                    baseAddress = ReadOperandByte();
                    var lo = ReadMemoryByte((ushort)((baseAddress + _x) & LowByteMask));
                    var hi = ReadMemoryByte((ushort)((baseAddress + _x + 1) & LowByteMask));
                    address = (ushort)(hi << ByteLength | lo);
                    crossedPage = false;
                    return ReadMemoryByte(address);

                case AddressingMode.IndirectY:
                    baseAddress = ReadOperandByte();
                    lo = ReadMemoryByte(baseAddress);
                    hi = ReadMemoryByte((ushort)((baseAddress + 1) & LowByteMask));
                    addressWithoutRegister = (ushort)(hi << ByteLength | lo);
                    address = (ushort)(addressWithoutRegister + _y);
                    crossedPage = oopsCycle && !IsSamePage(address, addressWithoutRegister);
                    return ReadMemoryByte(address);

                default:
                    crossedPage = false;
                    return 0;
            }
        }

        private byte ReadOperandByte()
        {
            return ReadMemoryByte((ushort)(_pc + 1));
        }

        private ushort ReadOperandWord()
        {
            return ReadMemoryWord((ushort)(_pc + 1));
        }

        private void WriteValue(AddressingMode mode, byte value)
        {
            ushort address;
            byte baseAddress;
            switch (mode)
            {
                case AddressingMode.Accumulator:
                    _a = value;
                    break;

                case AddressingMode.ZeroPage:
                    address = ReadOperandByte();
                    WriteMemory(address, value);
                    break;

                case AddressingMode.ZeroPageX:
                    address = (ushort)(ReadOperandByte() + _x & LowByteMask);
                    WriteMemory(address, value);
                    break;

                case AddressingMode.ZeroPageY:
                    address = (ushort)(ReadOperandByte() + _y & LowByteMask);
                    WriteMemory(address, value);
                    break;

                case AddressingMode.Absolute:
                    address = ReadOperandWord();
                    WriteMemory(address, value);
                    break;

                case AddressingMode.AbsoluteX:
                    address = (ushort)(ReadOperandWord() + _x);
                    WriteMemory(address, value);
                    break;

                case AddressingMode.AbsoluteY:
                    address = (ushort)(ReadOperandWord() + _y);
                    WriteMemory(address, value);
                    break;

                case AddressingMode.IndirectX:
                    baseAddress = ReadOperandByte();
                    var lo = ReadMemoryByte((ushort)((baseAddress + _x) & LowByteMask));
                    var hi = ReadMemoryByte((ushort)((baseAddress + _x + 1) & LowByteMask));
                    address = (ushort)(hi << ByteLength | lo);
                    WriteMemory(address, value);
                    break;

                case AddressingMode.IndirectY:
                    baseAddress = ReadOperandByte();
                    lo = ReadMemoryByte(baseAddress);
                    hi = ReadMemoryByte((ushort)((baseAddress + 1) & LowByteMask));
                    address = (ushort)((hi << ByteLength | lo) + _y);
                    WriteMemory(address, value);
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
                value = _internalRam[GetInternalRamAddress(address)];
            }
            else if (address <= PpuRegistersAddressEnd)
            {
                value = _emulator.Ppu.ReadRegister(GetPPUAddress(address));
            }
            else if (address <= ApuIORegistersAddressEnd)
            {
                if (address == Controller.AddressOne)
                {
                    value = _emulator.ControllerOne.Read();
                }
                else if (address == Controller.AddressTwo)
                {
                    value = _emulator.ControllerTwo.Read();
                }
            }
            else if (address <= TestModeAddressEnd)
            {
                // Not Used
            }
            else if (address >= CartridgeAddress)
            {
                value = _emulator.Mapper.Read(address);
            }

            return value;
        }

        private ushort ReadMemoryWord(ushort address)
        {
            var lo = ReadMemoryByte(address++);
            var hi = ReadMemoryByte(address);
          
            return (ushort)(hi << ByteLength | lo);
        }

        private void WriteMemory(ushort address, byte value)
        {
            if (address <= InternalCpuRamAddressEnd)
            {
                _internalRam[GetInternalRamAddress(address)] = value;
            }
            else if (address <= PpuRegistersAddressEnd)
            {
                _emulator.Ppu.WriteRegister(GetPPUAddress(address), value);
            }
            else if (address <= ApuIORegistersAddressEnd)
            {
                if (address == Ppu.OamDmaAddress)
                {
                    _emulator.Ppu.WriteRegister(address, value);
                }
                else if (address == Controller.AddressOne)
                {
                    byte strobe = (byte)(value & 1);

                    _emulator.ControllerOne.Write(strobe);
                    _emulator.ControllerTwo.Write(strobe);
                }
                else
                {
                    _emulator.Apu.WriteRegister(address, value);
                }
            }
            else if (address <= TestModeAddressEnd)
            {
                // Not Used
            }
            else if (address >= CartridgeAddress)
            {
                _emulator.Mapper.Write(address, value);
            }
        }

        private static ushort GetInternalRamAddress(ushort address)
        {
            return (ushort)(address % InternalRamSize);
        }

        private static ushort GetPPUAddress(ushort address)
        {
            return (ushort)((address - PpuRegistersAddressStart) % PpuRegistersSize + PpuRegistersAddressStart);
        }

        private void PushByteStack(byte value)
        {
            WriteMemory((ushort)(StackOffset | _s), value);
            _s--;
        }

        private void PushWordStack(ushort value)
        {
            PushByteStack((byte)(value >> ByteLength)); // Hi
            PushByteStack((byte)value); // Lo
        }

        private byte PullByteStack()
        {
            _s++;
            return ReadMemoryByte((ushort)(StackOffset | _s));
        }

        private ushort PullWordStack()
        {
            byte lo = PullByteStack();
            byte hi = PullByteStack();
            return (ushort)(hi << ByteLength | lo);
        }
    }
}
