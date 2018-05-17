using System;
using static Constants;

public class Ppu
{
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

    private struct Register 
    {
        private ushort data;
        public ushort Data => data;

        public byte CoarseX
        {
            get
            {
                return (byte)(data & 0x1F);
            }
            set
            {
                data = (ushort)((data & ~0x1F) | (value & 0x1F));
            }
        }
        public byte CoarseY
        {
            get
            {
                return (byte)(data >> 5 & 0x1F);
            }
            set
            {
                data = (ushort)((data & ~(0x1F << 5 ))| ((value & 0x1F) << 5));
            }
        }
        public byte NametableX
        {
            get
            {
                return (byte)(data >> 10 & 0x01);
            }
            set
            {
                data = (ushort)((data & ~(0x01 << 10)) | ((value & 0x01) << 10));
            }
        }
        public byte NametableY
        {
            get
            {
                return (byte)(data >> 11 & 0x01);
            }
            set
            {
                data = (ushort)((data & ~(0x01 << 11)) | ((value & 0x01) << 11));
            }
        }
        public byte FineY
        {
            get
            {
                return (byte)(data >> 12 & 0x07);
            }
            set
            {
                data = (ushort)((data & ~(0x07 << 12)) | ((value & 0x07) << 12));
            }
        }

        public byte AddrHi
        {
            set
            {
                data = (ushort)((data & ~(0x3F << ByteLength)) | ((value & 0x3F) << ByteLength));
            }
        }
        public byte AddrLo
        {
            set
            {
                data = (ushort)((data & ~LowByteMask) | value);
            }
        }

        public Register(ushort newData)
        {
            data = newData;
        }

        public static Register operator +(Register r, int i)
        {
            return new Register((ushort)(r.Data + i));
        }
    }

    private readonly byte[] screenPixels;   public byte[] ScreenPixels => screenPixels;

    private int currentCycle;               public int CurrentCycle => currentCycle;
    private int currentScanline;            public int CurrentScanline => currentScanline;
    private bool oddFrame;                  public bool OddFrame => oddFrame;

    // NTSC
    private const int Scanlines = 262;
    private const int Cycles = 341;
    private const int VisibleScanlines = 240;

    // Register Addresses
    private const ushort PpuCtrlAddress = 0x2000;
    private const ushort PpuMaskAddress = 0x2001;
    private const ushort PpuStatusAddress = 0x2002;
    private const ushort OamAddrAddress = 0x2003;
    private const ushort OamDataAddress = 0x2004;
    private const ushort PpuScrollAddress = 0x2005;
    private const ushort PpuAddrAddress = 0x2006;
    private const ushort PpuDataAddress = 0x2007;
    public  const ushort OamDmaAddress = 0x4014;

    // Memory
    private readonly byte[] vRam;
    private readonly byte[] paletteRam;
    private readonly byte[] oam;
    private readonly byte[] secondaryOAM;

    // Memory Sizes
    private const ushort VRamSize = 0x1000;
    private const ushort NametableSize = 0x400;
    private const ushort PalleteSize = 0x0020;
    private const int OamSize = 256;
    private const int SecondaryOamSize = 8;
    private const int BytesPerSprite = 4;

    // Memory Addreses
    private const ushort PatternTableAddressEnd = 0x1FFF;
    private const ushort NametableAddressStart = 0x2000;
    private const ushort AttributeTableStart = 0x23C0;
    private const ushort NametableAddressEnd = 0x2FFF;
    private const ushort NametableMirrorAddressStart = 0x3000;
    public  const ushort PalleteAddressStart = 0x3F00;
    private const ushort PalleteMirrorAddressStart = 0x3F20;

    // Registers
    private Control ppuCtrl;
    private Mask ppuMask;
    private Status ppuStatus;

    // Internal Registers
    private Register v; // VRAM Address
    private Register t; // Temporary VRAM Address
    private byte x;     // Fine X Scroll
    private bool w;     // First/Second Write Toggle

    // OAMDMA
    public byte OamData { get; set; }
    private byte oamAddr; public byte OAMAddr => oamAddr;

    // Internal Buffers
    private byte nametableBuffer;
    private byte attributeTableByte;
    private byte patternTableLSBBuffer;
    private byte patternTableMSBBuffer;
    private byte readbuffer;

    // Shift Registers
    private ushort BackgroundShifterPatternLo;
    private ushort BackgroundShifterPatternHi;
    private byte BackgroundShifterAttributeLo;
    private byte BackgroundShifterAttributeHi;

    // Sprite Evaluation
    private int n, m;
    private int spriteCount;
    private bool copySprite;
    private bool secondaryOamFull;
    private bool oamChecked;
    private bool spriteZero;

    // Emulator
    private readonly NesEmulator emulator;

    public Ppu(NesEmulator emulator)
    {
        this.emulator = emulator;

        screenPixels = new byte[NesEmulator.ScreenWidth * NesEmulator.ScreenHeight];

        v = new Register();
        t = new Register();

        vRam = new byte[VRamSize];
        paletteRam = new byte[PalleteSize];
        oam = new byte[OamSize * BytesPerSprite];
        secondaryOAM = new byte[SecondaryOamSize * BytesPerSprite];

        currentCycle = 0;
    }

    public void Reset()
    {
        
    }

    private bool IsControlFlagSet(Control flag)
    {
        return (ppuCtrl & flag) > 0;
    }

    private ushort GetControlFlag(Control flag)
    {
        return (ushort)(IsControlFlagSet(flag) ? 1: 0);
    }

    private bool IsMaskFlagSet(Mask flag)
    {
        return (ppuMask & flag) > 0;
    }

    private void SetStatusFlag(Status flag, bool value)
    {
        ppuStatus = value ? ppuStatus | flag : ppuStatus & ~flag;
    }

    public void Step()
    {
        // Pre-render Scanline
        if (currentScanline == -1)
        {
            // Clear Status Flags
            if (currentCycle == 1)
            {
                SetStatusFlag(Status.SpriteOverflow, false);
                SetStatusFlag(Status.SpriteZeroHit, false);
                SetStatusFlag(Status.VBlank, false);
            }
            // Copy Y
            else if (currentCycle >= 220 && currentCycle <= 304)
            {
                if (ShouldRender())
                {
                    v.CoarseY = t.CoarseY;
                    v.FineY = t.FineY;
                    v.NametableY = t.NametableY;
                }
            }         
        }

        // Pre-Render and Visible Scanlines
        if (currentScanline < VisibleScanlines)
        {
            // Odd Frame Idle
            if (currentCycle == 0)
            {
                // Idle
            }
            else if (currentCycle > 0 && currentCycle <= 256)
            {
                FetchTile((byte)((currentCycle - 1) % 8));

                // Scroll Y
                if (currentCycle == 256)
                {
                    if (ShouldRender())
                    {
                        if (v.FineY < 7)
                        {
                            v.FineY++;
                        }
                        else
                        {
                            v.FineY = 0;
                            if (v.CoarseY == 29) // Start of Attribute Table
                            {
                                v.CoarseY = 0;
                                v.NametableY ^= 1;
                            }
                            else if (v.CoarseY == 31) // In Attribute Table
                                v.CoarseY = 0;
                            else
                                v.CoarseY++;
                        }
                    }
                }
            }
            else if (currentCycle == 257)
            {
                LoadBackgroundShifters();

                // Copy X
                if (ShouldRender())
                {
                    v.CoarseX = t.CoarseX;
                    v.NametableX = t.NametableX;
                }
            }
            else if (currentCycle >= 321 && currentCycle <= 336)
            {
                FetchTile((byte)((currentCycle - 1) % 8));
            }
            else if (currentCycle == 337 || currentCycle == 339)
            {
                nametableBuffer = FetchNametable();
            }

            // Secondary OAM Clear
            if (currentScanline > 0 && CurrentCycle >= 1 && CurrentCycle <= 64)
            {
                // Read
                if (CurrentCycle % 2 == 0)
                {
                    OamData = 0xFF;
                }
                // Write
                else
                {
                    secondaryOAM[currentCycle / 2] = OamData;
                }
            }
            // Sprite Evaluation
            else if (currentScanline > 0 && CurrentCycle >= 65 && CurrentCycle <= 256)
            {
                // Reset Evaluation State
                if (CurrentCycle == 65)
                {
                    n = 0;
                    m = 0;
                    spriteCount = 0;
                    copySprite = false;
                    secondaryOamFull = false;
                    oamChecked = false;
                    spriteZero = false;
                }

                // Read
                if (CurrentCycle % 2 == 1)
                {
                    OamData = oam[BytesPerSprite * n + m];
                }
                // Write
                else if (!oamChecked)
                {
                    if (!secondaryOamFull)
                    {
                        secondaryOAM[BytesPerSprite * spriteCount] = OamData;

                        if (OamData >= currentScanline && OamData < CurrentScanline + 8)
                        {
                            if (m < 3)
                            {
                                m++;
                                copySprite = true;
                            }
                            else
                            {
                                m = 0;
                                copySprite = false;
                                spriteCount++;

                                if (!spriteZero)
                                {
                                    spriteZero = n == 0;
                                }
                            }
                        }
                    }
                    else
                    {
                        if (OamData >= currentScanline && OamData < CurrentScanline + 8 && spriteCount == 8)
                        {
                            if (m < 3)
                            {
                                m++;
                                copySprite = true;
                            }
                            else
                            {
                                m = 0;
                                copySprite = false;
                                spriteCount++;
                            }
                        }
                        else
                        {
                            n++;
                            m++;
                            if (m == 3)
                            {
                                m = 0;
                            }
                        }
                    }

                    if (!copySprite)
                    {
                        n++;

                        if (n == 64)
                        {
                            n = 0;
                            oamChecked = true;
                        }

                        if (spriteCount == 8)
                        {
                            secondaryOamFull = true;
                        }
                    }
                }

                if(CurrentCycle == 256 && spriteCount > 8)
                {
                    SetStatusFlag(Status.SpriteOverflow, true);
                }
            }
            // Fetch Sprite
            else if (CurrentCycle >= 257 && CurrentCycle <= 320)
            {

            }
            // Background render pipe initialization
            else if ((CurrentCycle >= 321 && CurrentCycle <= 340) || CurrentCycle == 0)
            {

            }
        }
        // Post Render
        else if (currentScanline == VisibleScanlines)
        {
            // Idle
        }
        // VBlank
        else if (currentScanline == VisibleScanlines + 1)
        {
            if (currentCycle == 1)
            {
                SetStatusFlag(Status.VBlank, true);

                if (IsControlFlagSet(Control.NMIEnable))
                {
                    emulator.cpu.RaiseNmi();
                }
            }
        }

        // Background Rendering
        var bgPixel = default(byte);
        var bgPalette = default(byte);
        if (ShouldRenderBackground())
        {
            byte mux = (byte)(0x80 >> x);

            byte pixelLSB = (byte)((BackgroundShifterPatternLo & mux) > 0 ? 1 : 0);
            byte pixelMSB = (byte)((BackgroundShifterPatternHi & mux) > 0 ? 1 : 0);
            bgPixel = (byte)((pixelMSB << 1) | pixelLSB);

            byte paletteLSB = (byte)((BackgroundShifterAttributeLo & mux) > 0 ? 1 : 0);
            byte paletteMSB = (byte)((BackgroundShifterAttributeHi & mux) > 0 ? 1 : 0);
            bgPalette = (byte)((paletteMSB << 1) | paletteLSB);
        }

        // Sprite Rendering
        var spPixel = default(byte);
        var spPalette = default(byte);
        var spPriority = default(byte);
        if (ShouldRenderSprites())
        {
          // TODO
        }

        // Priority
        var pixel = default(byte);
        var palette = default(byte);
        if(bgPixel == 0 && spPixel == 0)
        {
            pixel = 0x00;
            palette = 0x00;
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
            if (spPriority > 0)
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
            if(ShouldRenderBackground() && ShouldRenderSprites())
            {
                if(IsMaskFlagSet(Mask.BackgroundLeftCol) && IsMaskFlagSet(Mask.SpriteLeftCol))
                {
                   if(currentCycle >=1 && currentCycle < 258)
                    {
                        SetStatusFlag(Status.SpriteZeroHit, true);
                    }
                }
                else
                {
                    if (currentCycle >= 9 && currentCycle < 258)
                    {
                        SetStatusFlag(Status.SpriteZeroHit, true);
                    }
                }
            }
        }

        if (currentCycle > 0 && currentCycle <= NesEmulator.ScreenWidth && currentScanline >= 0 && currentScanline < VisibleScanlines)
        {
            screenPixels[currentCycle - 1 + currentScanline * NesEmulator.ScreenWidth] = ReadMemory((ushort)(PalleteAddressStart + (palette * 4) + pixel));
        }

        currentCycle++;
        if(currentCycle > Cycles)
        {
            currentCycle = 0;
            currentScanline++;
            if (currentScanline >= Scanlines - 1)
            {
                currentScanline = -1;
                if (oddFrame)
                {
                    currentCycle = 1;
                }
                oddFrame = !oddFrame;
            }
        }
    }

    private bool ShouldRender()
    {
        return ShouldRenderBackground() || ShouldRenderSprites();
    }
    private bool ShouldRenderBackground()
    {
        return (ppuMask & Mask.BackgroundEnable) > 0;
    }

    private bool ShouldRenderSprites()
    {
        return (ppuMask & Mask.SpriteEnable) > 0;
    }

    private void FetchTile(byte cycle)
    {
        // Update Shifters
        if (ShouldRender())
        {
            BackgroundShifterPatternLo <<= 1;
            BackgroundShifterPatternHi <<= 1;
            BackgroundShifterAttributeLo <<= 1;
            BackgroundShifterAttributeHi <<= 1;
        }

        // Nametable Byte
        if (cycle == 0)
        {
            LoadBackgroundShifters();

            nametableBuffer = FetchNametable();
        }
        // Attribute Byte
        else if (cycle == 2)
        {
            attributeTableByte = ReadMemory((ushort)(AttributeTableStart |
                                                    (v.Data & 0x0C00) |      // Nametable
                                                   ((v.Data >> 4) & 0x38) |  // Coarse Y / 4
                                                   ((v.Data >> 2) & 0x07))); // Coarse X / 4
            if((v.CoarseY & 0x02) > 0)
            {
                attributeTableByte >>= 4;
            }
            if ((v.CoarseX & 0x02) > 0)
            {
                attributeTableByte >>= 2;
            }
            attributeTableByte &= 0x03;
        }
        // Pattern Table Tile LSB
        else if (cycle == 4)
        {
            patternTableLSBBuffer = ReadMemory((ushort)(GetControlFlag(Control.BackgroundTileSelect) << 12 |
                                                       nametableBuffer << 4 |
                                                       0 | // LSB Plane
                                                       v.FineY));
        }
        // Pattern Table Tile MSB
        else if (cycle == 6)
        {
            patternTableMSBBuffer = ReadMemory((ushort)(GetControlFlag(Control.BackgroundTileSelect) << 12 |
                                                       nametableBuffer << 4 |
                                                       8 | // MSB Plane
                                                       v.FineY));
        }
        // Scroll Coarse X
        else if (cycle == 7)
        {
            if ((ppuMask & Mask.BackgroundEnable) > 0 || (ppuMask & Mask.SpriteEnable) > 0)
            {
                if (v.CoarseX == 31)
                {
                    v.CoarseX = 0;
                    v.NametableX ^= 1;
                }
                else
                {
                    v.CoarseX++;
                }
            }
        }
    }

    private byte FetchNametable()
    {
        return ReadMemory((ushort)(NametableAddressStart | (v.Data & 0x0FFF))); 
    }

    private void LoadBackgroundShifters()
    {
        BackgroundShifterPatternLo = (ushort)(BackgroundShifterPatternLo & HighByteMask | patternTableLSBBuffer);
        BackgroundShifterPatternHi = (ushort)(BackgroundShifterPatternHi & HighByteMask | patternTableMSBBuffer);
        BackgroundShifterAttributeLo = (byte)(((attributeTableByte & 0x01) > 0) ? 0xFF : 0x00);
        BackgroundShifterAttributeHi = (byte)(((attributeTableByte & 0x02) > 0) ? 0xFF : 0x00);
    }  

    public byte ReadRegister(ushort address)
    {
        var value = default(byte);

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
            value = (byte)ppuStatus;
            SetStatusFlag(Status.VBlank, false);
            w = false;
        }
        else if (address == OamAddrAddress)
        {
            // Write Only
        }
        else if (address == OamDataAddress)
        {
            value = oam[oamAddr];
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
            value = readbuffer;
            readbuffer = ReadMemory(v.Data);

            if (v.Data >= PalleteAddressStart)
            {
                value = readbuffer;
            }

            IncrementPpuAddr();
        }
        else if (address == OamDmaAddress)
        {
            // Write Only
        }

        return value;
    }

    public void WriteRegister(ushort address, byte value)
    {
        if (address == PpuCtrlAddress)
        {
            ppuCtrl = (Control)value;
            t.NametableX = (byte)ppuCtrl;
            t.NametableY = (byte)((byte)ppuCtrl >> 1);
        }
        else if (address == PpuMaskAddress)
        {
            ppuMask = (Mask)value;
        }
        else if (address == PpuStatusAddress)
        {
            // Read Only
        }
        else if (address == OamAddrAddress)
        {
            oamAddr = value;
        }
        else if (address == OamDataAddress)
        {
            WriteOam(value);
        }
        else if (address == PpuScrollAddress)
        {
            if (w) // Second Write
            {
                t.CoarseY = (byte)(value >> 3);
                t.FineY = (byte)(value & 0x7);
            }
            else  // First Write
            {
                t.CoarseX = (byte)(value >> 3);
                x = (byte)(value & 0x7);
            }

            w = !w;
        }
        else if (address == PpuAddrAddress)
        {
            if (w) // Second Write
            {
                t.AddrLo = value;
                v = t;
            }
            else  // First Write
            {
                t.AddrHi = value;
            }

            w = !w;
        }
        else if (address == PpuDataAddress)
        {
            WriteMemory(v.Data, value);

            IncrementPpuAddr();
        }
        else if (address == OamDmaAddress)
        {
            emulator.cpu.StartOamDma(value);
        }
    }

    public void WriteOam(byte oamValue)
    {
        oam[oamAddr] = oamValue;
        oamAddr++;
    }

    private void IncrementPpuAddr()
    {
        v += (IsControlFlagSet(Control.IncrementMode) ? 32 : 1);
    }

    public byte ReadMemory(ushort address)
    {
        var value = default(byte);

        if (address <= PatternTableAddressEnd)
        {
            value = emulator.Mapper.Read(address);
        }
        else if (address <= NametableAddressEnd)
        {
            value = vRam[GetNametableAddress(address)];
        }
        else if (address >= PalleteAddressStart)
        {
            value = paletteRam[GetPaletteRamAddress(address)];
        }

        return value;
    }

    private void WriteMemory(ushort address, byte value)
    {
        if (address <= PatternTableAddressEnd)
        {
            emulator.Mapper.Write(address, value);
        }
        else if (address <= NametableAddressEnd)
        {
            vRam[GetNametableAddress(address)] = value;
        }
        else if (address >= PalleteAddressStart)
        {
            paletteRam[GetPaletteRamAddress(address)] = value;
        }
    }

    private ushort GetNametableAddress(ushort address)
    {
        if ((address >= NametableMirrorAddressStart && address < PalleteAddressStart))
        {
            address = (ushort)(address - VRamSize);
        }

        Mapper.MirrorMode mode = emulator.Mapper.GetMirrorMode();
        ushort nametableAddress = (ushort)(address - NametableAddressStart);
        if (mode == Mapper.MirrorMode.Horizontal)
        {
            if((nametableAddress >= NametableSize && nametableAddress < NametableSize * 2) ||
                nametableAddress >= NametableSize * 3)
            {
                nametableAddress -= NametableSize;
            }
        }
        else if(mode == Mapper.MirrorMode.Vertical)
        {
            if (nametableAddress >= NametableSize*2)
            {
                nametableAddress -= NametableSize * 2;
            }
        }

        return nametableAddress;
    }

    private static ushort GetPaletteRamAddress(ushort address)
    {
        if ((address >= PalleteMirrorAddressStart))
        {
            address = (ushort)(((address - PalleteAddressStart) % PalleteSize) + PalleteAddressStart);
        }

        if (address == 0x3F10 || address == 0x3F14 || address == 0x3F18 || address == 0x3F1C)
        {
            address = (ushort)(address - 0x0010);
        }

        return (ushort)(address - PalleteAddressStart);
    }
}
