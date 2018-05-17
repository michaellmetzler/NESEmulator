using System;
using System.Runtime.InteropServices;

using static Nes.Constants;

namespace Nes
{
    public unsafe class Ppu
    {
        [Flags]
        public enum OamTileIndex : byte
        {
            Bank = 1 << 0,
            Priority = 127 << 1
        }

        [Flags]
        public enum OamSpriteAttributes : byte
        {
            Palette = 3 << 0,
            Priority = 1 << 5,
            FlipHorizontal = 1 << 6,
            FlipVertical = 1 << 7,
        }

        [Flags]
        private enum Control : byte
        {
            NameTableSelect = 3 << 0,
            IncrementMode = 1 << 2,
            SpriteTileSelect = 1 << 3,
            BackgroundTileSelect = 1 << 4,
            SpriteHeight = 1 << 5,
            MasterSlave = 1 << 6,
            NMIEnable = 1 << 7,
        }

        [Flags]
        private enum Mask : byte
        {
            Greyscale = 1 << 0,
            BackgroundLeftCol = 1 << 1,
            SpriteLeftCol = 1 << 2,
            BackgroundEnable = 1 << 3,
            SpriteEnable = 1 << 4,
            EmRed = 1 << 5,
            EmGreen = 1 << 6,
            EmBlue = 1 << 7,
        }

        [Flags]
        private enum Status : byte
        {
            SpriteOverflow = 1 << 5,
            SpriteZeroHit = 1 << 6,
            VBlank = 1 << 7
        }

        [StructLayout(LayoutKind.Sequential)]
        public readonly struct OamSprite
        {
            public readonly byte _y;
            public readonly OamTileIndex _tileIndex;
            public readonly OamSpriteAttributes _attributes;
            public readonly byte _x;
            public readonly byte GetTileIndexBit(OamTileIndex flag)
            {
                return (byte)((_tileIndex & flag) > 0 ? 1 : 0);
            }

            public readonly bool IsSpriteAttributesBitSet(OamSpriteAttributes flag)
            {
                return (_attributes & flag) > 0;
            }
        }

        private struct Register
        {
            public ushort Data;

            public byte CoarseX
            {
                get => (byte)(Data & 0x1F);
                set => Data = (ushort)(Data & ~0x1F | value & 0x1F);
            }
            public byte CoarseY
            {
                get => (byte)(Data >> 5 & 0x1F);
                set => Data = (ushort)(Data & ~(0x1F << 5) | (value & 0x1F) << 5);
            }
            public byte NametableX
            {
                get => (byte)(Data >> 10 & 0x01);
                set => Data = (ushort)(Data & ~(0x01 << 10) | (value & 0x01) << 10);
            }
            public byte NametableY
            {
                get => (byte)(Data >> 11 & 0x01);
                set => Data = (ushort)(Data & ~(0x01 << 11) | (value & 0x01) << 11);
            }
            public byte FineY
            {
                get => (byte)(Data >> 12 & 0x07);
                set => Data = (ushort)(Data & ~(0x07 << 12) | (value & 0x07) << 12);
            }

            public byte AddrHi
            {
                set => Data = (ushort)(Data & ~(0x3F << ByteLength) | (value & 0x3F) << ByteLength);
            }
            public byte AddrLo
            {
                set => Data = (ushort)(Data & ~LowByteMask | value);
            }

            public Register(ushort newData)
            {
                Data = newData;
            }

            public static Register operator +(Register r, int i) => new((ushort)(r.Data + i));
        }

        public readonly int ScreenWidth;
        public readonly int ScreenHeight;
        public readonly float PpuDotsPerCpuCycle;

        private readonly int Scanlines;
        private readonly int Cycles;
        private readonly int VisibleScanlines;

        // NTSC
        public const int NtscScreenWidth = 256;
        public const int NtscScreenHeight = 240;
        public const int NtscPpuDotsPerCpuCycle = 3;

        private const int NtscScanlines = 262;
        private const int NtscCycles = 341;
        private const int NtscVisibleScanlines = 240;

        // Register Addresses
        public const ushort OamDmaAddress = 0x4014;

        private const ushort PpuCtrlAddress = 0x2000;
        private const ushort PpuMaskAddress = 0x2001;
        private const ushort PpuStatusAddress = 0x2002;
        private const ushort OamAddrAddress = 0x2003;
        private const ushort OamDataAddress = 0x2004;
        private const ushort PpuScrollAddress = 0x2005;
        private const ushort PpuAddrAddress = 0x2006;
        private const ushort PpuDataAddress = 0x2007;

        // Memory Sizes
        private const ushort VRamSize = 0x1000;
        private const ushort NametableSize = 0x400;
        private const ushort PalleteSize = 0x0020;
        private const int BytesPerSprite = 4;
        private const int OamSize = 64;
        private const int SecondaryOamSize = 8;

        // Memory Addreses
        public const ushort PalleteAddressStart = 0x3F00;

        private const ushort PatternTableAddressEnd = 0x1FFF;
        private const ushort NametableAddressStart = 0x2000;
        private const ushort AttributeTableStart = 0x23C0;
        private const ushort NametableAddressEnd = 0x2FFF;
        private const ushort NametableMirrorAddressStart = 0x3000;
        private const ushort PalleteMirrorAddressStart = 0x3F20;

        // Memory
        private readonly byte[] _vRam;
        private readonly byte[] _paletteRam;
        private readonly byte[] _oam;
        private readonly byte[] _secondaryOam;

        // State
        public int CurrentCycle { get; private set; }
        public int CurrentScanline { get; private set; }
        public int TotalFrames { get; private set; }
        public bool OddFrame { get; private set; }

        // Screen
        public byte[] ScreenPixels { get; private set; }

        // OAM
        public byte OamData;
        public byte OamAddr { get; private set; }

        // Registers
        private Control _ppuCtrl;
        private Mask _ppuMask;
        private Status _ppuStatus;

        // Internal Registers
        private Register _v; // VRAM Address
        private Register _t; // Temporary VRAM Address
        private byte _x;     // Fine X Scroll
        private bool _w;     // First/Second Write Toggle

        // Internal Buffers
        private byte _nametableBuffer;
        private byte _attributeTableByte;
        private byte _patternTableLSBBuffer;
        private byte _patternTableMSBBuffer;
        private byte _readbuffer;
        private byte _ioBus;

        // Background Shift Registers
        private ushort _backgroundShifterPatternLo;
        private ushort _backgroundShifterPatternHi;
        private ushort _backgroundShifterAttributeLo;
        private ushort _backgroundShifterAttributeHi;
        private bool _backgroundLatchAttributeLo;
        private bool _backgroundLatchAttributeHi;

        // Sprite Shift Registers
        private byte[] _spriteShifterPatternLo;
        private byte[] _spriteShifterPatternHi;
        private byte[] _spriteXCounter;
        private OamSpriteAttributes[] _spriteAttributeLatches;

        // Secondary OAM Clear
        private bool _clearingSecondaryOAM;

        // Sprite Evaluation
        private int _n, _m;
        private int _spriteCount;
        private bool _copySprite;
        private bool _secondaryOamFull;
        private bool _oamChecked;

        // Sprite Zero
        private bool _spriteZeroHitPossible;
        private bool _spriteZeroRendering;

        // Emulator
        private readonly Emulator _emulator;

        public Ppu(Emulator emulator)
        {
            _emulator = emulator;

            ScreenWidth = NtscScreenWidth;
            ScreenHeight = NtscScreenHeight;
            PpuDotsPerCpuCycle = NtscPpuDotsPerCpuCycle;
            Scanlines = NtscScanlines;
            Cycles = NtscCycles;
            VisibleScanlines = NtscVisibleScanlines;

            ScreenPixels = new byte[ScreenWidth * ScreenHeight];

            _v = new Register();
            _t = new Register();

            _vRam = new byte[VRamSize];
            _paletteRam = new byte[PalleteSize];

            _oam = new byte[OamSize * BytesPerSprite];
            _secondaryOam = new byte[SecondaryOamSize * BytesPerSprite];

            _spriteShifterPatternLo = new byte[8];
            _spriteShifterPatternHi = new byte[8];
            _spriteXCounter = new byte[8];
            _spriteAttributeLatches = new OamSpriteAttributes[8];

            Reset();
        }

        public void Reset()
        {
            _ppuCtrl = 0b00000000;
            _ppuMask = 0b00000000;

            _w = false;
            _t.Data = 0x0000;

            _readbuffer = 0x00;

            OddFrame = false;

            CurrentCycle = 21;
            CurrentScanline = 0;
        }

        public OamSprite GetOamSprite(int i) => MemoryMarshal.Cast<byte, OamSprite>(_oam)[i];

        public OamSprite GetSecondaryOamSprite(int i) => MemoryMarshal.Cast<byte, OamSprite>(_secondaryOam)[i];

        private bool IsControlFlagSet(Control flag) => (_ppuCtrl & flag) > 0;

        private ushort GetControlFlag(Control flag) => (ushort)(IsControlFlagSet(flag) ? 1 : 0);

        private bool IsMaskFlagSet(Mask flag) => (_ppuMask & flag) > 0;

        private void SetStatusFlag(Status flag, bool value) => _ppuStatus = value ? _ppuStatus | flag : _ppuStatus & ~flag;

        private bool IsStatusFlagSet(Status flag) => (_ppuStatus & flag) > 0;

        public void Step()
        {
            // Pre-render Scanline
            if (CurrentScanline == -1)
            {
                // Reset Emulator State
                if (CurrentCycle == 1)
                {
                    SetStatusFlag(Status.SpriteOverflow, false);
                    SetStatusFlag(Status.SpriteZeroHit, false);
                    SetStatusFlag(Status.VBlank, false);

                    _spriteZeroHitPossible = false;

                    for (int i = 0; i < 8; i++)
                    {
                        _spriteShifterPatternLo[i] = 0;
                        _spriteShifterPatternHi[i] = 0;
                        _spriteXCounter[i] = 0;
                        _spriteAttributeLatches[i] = 0;
                    }
                }
                // Copy Y
                else if (CurrentCycle >= 280 && CurrentCycle <= 304)
                {
                    if (ShouldRender())
                    {
                        _v.CoarseY = _t.CoarseY;
                        _v.FineY = _t.FineY;
                        _v.NametableY = _t.NametableY;
                    }
                }
            }

            // Pre-Render and Visible Scanlines
            if (CurrentScanline < VisibleScanlines)
            {
                // Odd Frame Idle
                if (CurrentCycle == 0 && CurrentScanline == 0 && OddFrame && ShouldRender())
                {
                    CurrentCycle = 1;
                }

                // Background Rendering
                {
                    if (CurrentCycle > 0 && CurrentCycle <= 256)
                    {
                        UpdateBackgroundShifters();

                        FetchTile((byte)((CurrentCycle - 1) % 8));

                        // Scroll Y
                        if (CurrentCycle == 256)
                        {
                            if (ShouldRender())
                            {
                                if (_v.FineY < 7)
                                {
                                    _v.FineY++;
                                }
                                else
                                {
                                    _v.FineY = 0;
                                    if (_v.CoarseY == 29) // Start of Attribute Table
                                    {
                                        _v.CoarseY = 0;
                                        _v.NametableY ^= 1;
                                    }
                                    else if (_v.CoarseY == 31) // In Attribute Table
                                    {
                                        _v.CoarseY = 0;
                                    }
                                    else
                                    {
                                        _v.CoarseY++;
                                    }
                                }
                            }
                        }
                    }
                    else if (CurrentCycle == 257)
                    {
                        LoadBackgroundShifters();

                        // Copy X
                        if (ShouldRender())
                        {
                            _v.CoarseX = _t.CoarseX;
                            _v.NametableX = _t.NametableX;
                        }
                    }
                    else if (CurrentCycle > 320 && CurrentCycle <= 336)
                    {
                        UpdateBackgroundShifters();
                        FetchTile((byte)((CurrentCycle - 1) % 8));
                    }
                    else if (CurrentCycle == 337 || CurrentCycle == 339)
                    {
                        _nametableBuffer = FetchNametable();
                    }
                }

                // Sprite Rendering
                {
                    if (CurrentScanline >= 0 && CurrentCycle >= 2)
                    {
                        UpdateSpriteShifters();
                    }

                    // Secondary OAM Clear
                    if (CurrentScanline >= 0 && CurrentCycle >= 1 && CurrentCycle <= 64)
                    {
                        if (CurrentCycle == 1)
                        {
                            _clearingSecondaryOAM = true;
                        }

                        // Read
                        if (CurrentCycle % 2 == 0)
                        {
                            OamData = ReadRegister(OamDataAddress);
                        }
                        // Write
                        else
                        {
                            _secondaryOam[CurrentCycle / 2] = 0XFF;
                        }

                        if (CurrentCycle == 64)
                        {
                            _clearingSecondaryOAM = false;
                        }
                    }
                    // Sprite Evaluation
                    else if (CurrentScanline >= 0 && CurrentCycle > 64 && CurrentCycle <= 256)
                    {
                        // Reset Evaluation State
                        if (CurrentCycle == 65)
                        {
                            _n = 0;
                            _m = 0;
                            _spriteCount = 0;
                            _copySprite = false;
                            _secondaryOamFull = false;
                            _oamChecked = false;
                            _spriteZeroRendering = false;
                        }

                        // Read
                        if (CurrentCycle % 2 == 1)
                        {
                            OamData = _oam[_n * BytesPerSprite + _m];
                        }
                        // Write
                        else
                        {
                            if (!_oamChecked)
                            {
                                int difference = CurrentScanline - OamData;

                                if (!_secondaryOamFull)
                                {
                                    _secondaryOam[BytesPerSprite * _spriteCount + _m] = OamData;

                                    if (difference >= 0 && difference < (IsControlFlagSet(Control.SpriteHeight) ? 16 : 8))
                                    {
                                        _copySprite = true;
                                    }
                                }
                                else
                                {
                                    if (_spriteCount == 8 && !IsStatusFlagSet(Status.SpriteOverflow))
                                    {
                                        if (difference >= 0 && difference < (IsControlFlagSet(Control.SpriteHeight) ? 16 : 8))
                                        {
                                            _copySprite = true;
                                            SetStatusFlag(Status.SpriteOverflow, true);
                                        }
                                        else
                                        {
                                            _n++;
                                            if (_n == 64)
                                            {
                                                _n = 0;
                                                _oamChecked = true;
                                            }
                                            _m++;
                                            if (_m == 3)
                                            {
                                                _m = 0;
                                            }
                                        }
                                    }
                                }

                                if (_copySprite)
                                {
                                    if (_m < 3)
                                    {
                                        _m++;
                                    }
                                    else
                                    {
                                        _m = 0;
                                        _copySprite = false;
                                        if (!IsStatusFlagSet(Status.SpriteOverflow))
                                        {
                                            _spriteCount++;
                                        }

                                        if (_n == 0)
                                        {
                                            _spriteZeroHitPossible = true;
                                        }
                                    }
                                }

                                if (!_copySprite)
                                {
                                    _n++;

                                    if (_n == 64)
                                    {
                                        _n = 0;
                                        _oamChecked = true;
                                    }

                                    if (_spriteCount == 8)
                                    {
                                        _secondaryOamFull = true;
                                    }
                                }
                            }
                            else
                            {
                                _n++;
                                if (_n == 64)
                                {
                                    _n = 0;
                                }
                            }
                        }
                    }
                    // Fetch Sprite
                    else if (CurrentCycle > 256 && CurrentCycle <= 320)
                    {
                        if (CurrentCycle == 257)
                        {
                            OamAddr = 0;
                        }
                    }
                    // Sprite render pipe initialization
                    else if (CurrentCycle == 340)
                    {
                        for (int i = 0; i < _spriteCount; i++)
                        {
                            byte spritePatternLo;
                            byte spritePatternHi;

                            var sprite = GetSecondaryOamSprite(i);

                            var tileIndex = (byte)(sprite._tileIndex);

                            if (!IsControlFlagSet(Control.SpriteHeight))
                            {
                                // 8x8
                                var row = (byte)(sprite.IsSpriteAttributesBitSet(OamSpriteAttributes.FlipVertical) ? 7 - (CurrentScanline - sprite._y) :
                                                                                                                          CurrentScanline - sprite._y);
                                spritePatternLo = ReadPatternTableSprite(tileIndex, row, false);
                                spritePatternHi = ReadPatternTableSprite(tileIndex, row, true);
                            }
                            else
                            {
                                // 8x16
                                var bank = sprite.GetTileIndexBit(OamTileIndex.Bank);
                                var row = (byte)((sprite.IsSpriteAttributesBitSet(OamSpriteAttributes.FlipVertical) ? 7 - (CurrentScanline - sprite._y) :
                                                                                                                           CurrentScanline - sprite._y) & 0x07);
                                var topTile = CurrentScanline - sprite._y < 8;

                                spritePatternLo = ReadPatternTableLargeSprites(bank, tileIndex, row, topTile, false);
                                spritePatternHi = ReadPatternTableLargeSprites(bank, tileIndex, row, topTile, true);
                            }

                            if (sprite.IsSpriteAttributesBitSet(OamSpriteAttributes.FlipHorizontal))
                            {
                                spritePatternLo = spritePatternLo.ReverseBits();
                                spritePatternHi = spritePatternHi.ReverseBits();
                            }

                            _spriteShifterPatternLo[i] = spritePatternLo;
                            _spriteShifterPatternHi[i] = spritePatternHi;
                            _spriteXCounter[i] = sprite._x;
                            _spriteAttributeLatches[i] = sprite._attributes;
                        }
                    }
                }
            }
            // Post Render
            else if (CurrentScanline == VisibleScanlines)
            {
                // Idle
            }
            // VBlank
            else if (CurrentScanline == VisibleScanlines + 1)
            {
                if (CurrentCycle == 1)
                {
                    SetStatusFlag(Status.VBlank, true);

                    if (IsControlFlagSet(Control.NMIEnable))
                    {
                        _emulator.Cpu.RaiseNmi();
                    }
                }
            }

            // Background Rendering
            var bgPixel = default(byte);
            var bgPalette = default(byte);
            if (ShouldRenderBackground())
            {
                byte mux = (byte)(0x80 >> _x);

                byte pixelLSB = (byte)(((_backgroundShifterPatternLo >> 8) & mux) > 0 ? 1 : 0);
                byte pixelMSB = (byte)(((_backgroundShifterPatternHi >> 8) & mux) > 0 ? 1 : 0);

                bgPixel = (byte)(pixelMSB << 1 | pixelLSB);

                byte paletteLSB = (byte)(((_backgroundShifterAttributeLo >> 8) & mux) > 0 ? 1 : 0);
                byte paletteMSB = (byte)(((_backgroundShifterAttributeHi >> 8) & mux) > 0 ? 1 : 0);

                bgPalette = (byte)(paletteMSB << 1 | paletteLSB);
            }

            // Sprite Rendering
            var spPixel = default(byte);
            var spPalette = default(byte);
            var spPriority = default(bool);
            if (ShouldRenderSprites())
            {
                _spriteZeroRendering = false;

                for (int i = 0; i < _spriteXCounter.Length; i++)
                {
                    if (_spriteXCounter[i] == 0)
                    {
                        byte mux = (byte)0x80;

                        byte pixelLSB = (byte)((_spriteShifterPatternLo[i] & mux) > 0 ? 1 : 0);
                        byte pixelMSB = (byte)((_spriteShifterPatternHi[i] & mux) > 0 ? 1 : 0);
                        spPixel = (byte)(pixelMSB << 1 | pixelLSB);

                        spPalette = (byte)((_spriteAttributeLatches[i] & OamSpriteAttributes.Palette) + 4);

                        spPriority = (_spriteAttributeLatches[i] & OamSpriteAttributes.Priority) == 0;

                        if (spPixel != 0)
                        {
                            if (i == 0)
                            {
                                _spriteZeroRendering = true;
                            }
                            break;
                        }
                    }
                }
            }

            // Priority
            var pixel = default(byte);
            var palette = default(byte);

            if(CurrentCycle <= 8)
            {
                if(!IsMaskFlagSet(Mask.BackgroundLeftCol))
                {
                    bgPixel = 0;
                }
                if(!IsMaskFlagSet(Mask.SpriteLeftCol))
                {
                    spPixel = 0;
                }
            }

            if (bgPixel == 0 && spPixel == 0)
            {
                pixel = 0;
                palette = 0;
            }
            else if (bgPixel == 0 && spPixel > 0)
            {
                pixel = spPixel;
                palette = spPalette;
            }
            else if (bgPixel > 0 && spPixel == 0)
            {
                pixel = bgPixel;
                palette = bgPalette;
            }
            else if (bgPixel > 0 && spPixel > 0)
            {
                if (spPriority)
                {
                    pixel = spPixel;
                    palette = spPalette;
                }
                else
                {
                    pixel = bgPixel;
                    palette = bgPalette;
                }

                // Sprite Zero Hit
                if (_spriteZeroHitPossible && _spriteZeroRendering)
                {
                    if (ShouldRenderBackground() && ShouldRenderSprites())
                    {
                        if (IsMaskFlagSet(Mask.BackgroundLeftCol) && IsMaskFlagSet(Mask.SpriteLeftCol))
                        {
                            if (CurrentCycle >= 1 && CurrentCycle < 256)
                            {
                                SetStatusFlag(Status.SpriteZeroHit, true);
                            }
                        }
                        else
                        {
                            if (CurrentCycle >= 9 && CurrentCycle < 256)
                            {
                                SetStatusFlag(Status.SpriteZeroHit, true);
                            }
                        }
                    }
                }
            }

            if (CurrentCycle > 0 && CurrentCycle <= ScreenWidth &&
                CurrentScanline >= 0 && CurrentScanline < VisibleScanlines)
            {
                ScreenPixels[CurrentCycle - 1 + CurrentScanline * ScreenWidth] = ReadMemory((ushort)(PalleteAddressStart + palette * 4 + pixel));
            }

            CurrentCycle++;
            if (CurrentCycle >= Cycles)
            {
                CurrentCycle = 0;
                CurrentScanline++;
                if (CurrentScanline >= Scanlines - 1)
                {
                    CurrentScanline = -1;
                    OddFrame = !OddFrame;
                    TotalFrames++;
                }
            }
        }

        private byte ReadPatternTableBackground(byte tileIndex, byte row, bool upper)
        {
            return ReadMemory((ushort)(GetControlFlag(Control.BackgroundTileSelect) << 12 |
                                       tileIndex << 4 |
                                       (byte)(upper ? 8 : 0) |
                                       row));
        }

        private byte ReadPatternTableSprite(byte tileIndex, byte row, bool upper)
        {
            return ReadMemory((ushort)(GetControlFlag(Control.SpriteTileSelect) << 12 |
                                       tileIndex << 4 |
                                       (byte)(upper ? 8 : 0) |
                                       row));
        }

        private byte ReadPatternTableLargeSprites(byte bank, byte tileIndex, byte row, bool topTile, bool upper)
        {
            byte tile = (byte)(topTile ? (tileIndex & 0xFE) : ((tileIndex & 0xFE) | 0x01));

            return ReadMemory((ushort)(bank << 12 |
                                      tile << 4 |
                                      (byte)(upper ? 8 : 0) |
                                      row));
        }

        private bool ShouldRender() => ShouldRenderBackground() || ShouldRenderSprites();

        private bool ShouldRenderBackground() => (_ppuMask & Mask.BackgroundEnable) > 0;

        private bool ShouldRenderSprites() => (_ppuMask & Mask.SpriteEnable) > 0;

        private void UpdateBackgroundShifters()
        {
            if (!ShouldRenderBackground())
            {
                return;
            }

            _backgroundShifterPatternLo <<= 1;
            _backgroundShifterPatternHi <<= 1;
            _backgroundShifterAttributeLo = (ushort)(_backgroundShifterAttributeLo << 1);
            _backgroundShifterAttributeHi = (ushort)(_backgroundShifterAttributeHi << 1);
        }

        private void UpdateSpriteShifters()
        {
            if (!ShouldRenderSprites())
            {
                return;
            }
            for (int i = 0; i < _spriteXCounter.Length; i++)
            {
                if (_spriteXCounter[i] > 0)
                {
                    _spriteXCounter[i]--;
                }
                else
                {
                    _spriteShifterPatternLo[i] <<= 1;
                    _spriteShifterPatternHi[i] <<= 1;
                }
            }
        }

        private void FetchTile(int cycle)
        {
            // Nametable Byte
            if (cycle == 0)
            {
                LoadBackgroundShifters();

                _nametableBuffer = FetchNametable();
            }
            // Attribute Byte
            else if (cycle == 2)
            {
                _attributeTableByte = ReadMemory((ushort)(AttributeTableStart |
                                                         _v.Data & 0x0C00 |     // Nametable
                                                         _v.Data >> 4 & 0x38 |  // Coarse Y / 4
                                                         _v.Data >> 2 & 0x07)); // Coarse X / 4
                if ((_v.CoarseY & 0x02) > 0)
                {
                    _attributeTableByte >>= 4;
                }

                if ((_v.CoarseX & 0x02) > 0)
                {
                    _attributeTableByte >>= 2;
                }

                _attributeTableByte &= 0x03;
            }
            // Pattern Table Tile LSB
            else if (cycle == 4)
            {
                _patternTableLSBBuffer = ReadPatternTableBackground(_nametableBuffer, _v.FineY, false);
            }
            // Pattern Table Tile MSB
            else if (cycle == 6)
            {
                _patternTableMSBBuffer = ReadPatternTableBackground(_nametableBuffer, _v.FineY, true);
            }
            // Scroll Coarse X
            else if (cycle == 7)
            {
                if ((_ppuMask & Mask.BackgroundEnable) > 0 || (_ppuMask & Mask.SpriteEnable) > 0)
                {
                    if (_v.CoarseX == 31)
                    {
                        _v.CoarseX = 0;
                        _v.NametableX ^= 1;
                    }
                    else
                    {
                        _v.CoarseX++;
                    }
                }
            }
        }

        private byte FetchNametable() => ReadMemory((ushort)(NametableAddressStart | _v.Data & 0x0FFF));

        private void LoadBackgroundShifters()
        {
            _backgroundShifterPatternLo = (ushort)(_backgroundShifterPatternLo & HighByteMask | _patternTableLSBBuffer);
            _backgroundShifterPatternHi = (ushort)(_backgroundShifterPatternHi & HighByteMask | _patternTableMSBBuffer);

            _backgroundLatchAttributeLo = (_attributeTableByte & 0x01) > 0;
            _backgroundLatchAttributeHi = (_attributeTableByte & 0x02) > 0;

            _backgroundShifterAttributeLo = (ushort)(_backgroundShifterAttributeLo & HighByteMask | (_backgroundLatchAttributeLo ? 0xFF : 0x00));
            _backgroundShifterAttributeHi = (ushort)(_backgroundShifterAttributeHi & HighByteMask | (_backgroundLatchAttributeHi ? 0xFF : 0x00));
        }

        public byte ReadRegister(ushort address)
        {
            if (address == PpuCtrlAddress)
            {
                // Write Only
            }
            else if (address == PpuMaskAddress)
            {
                // Write Only
            }
            else if (address == PpuStatusAddress)
            {
                _ioBus = (byte)((_ioBus & 0x1F) | ((byte)_ppuStatus & 0xE0));
                SetStatusFlag(Status.VBlank, false);
                _w = false;
            }
            else if (address == OamAddrAddress)
            {
                // Write Only
            }
            else if (address == OamDataAddress)
            {
                if (_clearingSecondaryOAM)
                {
                    _ioBus = 0XFF;
                }
                else
                {
                    _ioBus = _oam[OamAddr];
                }
            }
            else if (address == PpuScrollAddress)
            {
                // Write Only
            }
            else if (address == PpuAddrAddress)
            {
                // Write Only
            }
            else if (address == PpuDataAddress)
            {
                _ioBus = _readbuffer;
                _readbuffer = ReadMemory(_v.Data);

                if (_v.Data >= PalleteAddressStart)
                {
                    _ioBus = _readbuffer;
                }

                IncrementPpuAddr();
            }
            else if (address == OamDmaAddress)
            {
                // Write Only
            }

            return _ioBus;
        }

        public void WriteRegister(ushort address, byte value)
        {
            _ioBus = value;

            if (address == PpuCtrlAddress)
            {
                bool prevNmi = IsControlFlagSet(Control.NMIEnable);
                _ppuCtrl = (Control)_ioBus;
                _t.NametableX = (byte)((byte)_ppuCtrl & 0x0001);
                _t.NametableY = (byte)(((byte)_ppuCtrl >> 1) & 0x0001);
                if (!prevNmi && IsStatusFlagSet(Status.VBlank) && IsControlFlagSet(Control.NMIEnable))
                {
                    _emulator.Cpu.RaiseNmi();
                }
            }
            else if (address == PpuMaskAddress)
            {
                _ppuMask = (Mask)_ioBus;
            }
            else if (address == PpuStatusAddress)
            {
                // Read Only
            }
            else if (address == OamAddrAddress)
            {
                OamAddr = _ioBus;
            }
            else if (address == OamDataAddress)
            {
                if(!(CurrentScanline >= 0 && CurrentScanline < VisibleScanlines && ShouldRender()))
                {
                    WriteOam(_ioBus);
                }
            }
            else if (address == PpuScrollAddress)
            {
                if (!_w) // First Write
                {
                    _t.CoarseX = (byte)(_ioBus >> 3);
                    _x = (byte)(_ioBus & 0x7);
                }
                else  // Second Write
                {
                    _t.CoarseY = (byte)(_ioBus >> 3);
                    _t.FineY = (byte)(_ioBus & 0x7);
                }

                _w = !_w;
            }
            else if (address == PpuAddrAddress)
            {
                if (!_w) // First Write
                {
                    _t.AddrHi = _ioBus;
                }
                else   // Second Write
                {
                    _t.AddrLo = _ioBus;
                    _v = _t;
                }

                _w = !_w;
            }
            else if (address == PpuDataAddress)
            {
                WriteMemory(_v.Data, _ioBus);

                IncrementPpuAddr();
            }
            else if (address == OamDmaAddress)
            {
                _emulator.Cpu.StartOamDma(_ioBus);
            }
        }

        public void WriteOam(byte oamValue)
        {
            _oam[OamAddr] = oamValue;
            OamAddr++;
        }

        private void IncrementPpuAddr() => _v += IsControlFlagSet(Control.IncrementMode) ? 32 : 1;

        public byte ReadMemory(ushort address)
        {
            var value = default(byte);

            if (address <= PatternTableAddressEnd)
            {
                value = _emulator.Mapper.Read(address);
            }
            else if (address <= NametableAddressEnd)
            {
                value = _vRam[GetNametableAddress(address)];
            }
            else if (address >= PalleteAddressStart)
            {
                value = _paletteRam[GetPaletteRamAddress(address)];
            }

            return value;
        }

        private void WriteMemory(ushort address, byte value)
        {
            if (address <= PatternTableAddressEnd)
            {
                _emulator.Mapper.Write(address, value);
            }
            else if (address <= NametableAddressEnd)
            {
                _vRam[GetNametableAddress(address)] = value;
            }
            else if (address >= PalleteAddressStart)
            {
                _paletteRam[GetPaletteRamAddress(address)] = value;
            }
        }

        private ushort GetNametableAddress(ushort address)
        {
            if (address >= NametableMirrorAddressStart && address < PalleteAddressStart)
            {
                address = (ushort)(address - VRamSize);
            }

            var mode = _emulator.Mapper.Mode;
            ushort nametableAddress = (ushort)(address - NametableAddressStart);
            if (mode == Mapper.MirrorMode.Horizontal)
            {
                if (nametableAddress >= NametableSize && nametableAddress < NametableSize * 2 ||
                    nametableAddress >= NametableSize * 3)
                {
                    nametableAddress -= NametableSize;
                }
            }
            else if (mode == Mapper.MirrorMode.Vertical)
            {
                if (nametableAddress >= NametableSize * 2)
                {
                    nametableAddress -= NametableSize * 2;
                }
            }

            return nametableAddress;
        }

        private static ushort GetPaletteRamAddress(ushort address)
        {
            if (address >= PalleteMirrorAddressStart)
            {
                address = (ushort)((address - PalleteAddressStart) % PalleteSize + PalleteAddressStart);
            }

            if (address == 0x3F10 || address == 0x3F14 || address == 0x3F18 || address == 0x3F1C)
            {
                address = (ushort)(address - 0x0010);
            }

            return (ushort)(address - PalleteAddressStart);
        }
    }
}
