using System;
using System.Collections;

public class NesEmulator
{
    // NTSC Constants
    public const int ScreenWidth = 256;
    public const int ScreenHeight = 240;
    public const int ApuFrameCounterRate = 60;
    public const int PpuDotsPerCpuCycle = 3;

    // Components
    public Cpu cpu;
    public Ppu ppu;
    public Apu apu;
    public Controller controllerOne;
    public Controller controllerTwo;
    public Mapper Mapper { get; private set; }

    public Action OnStep;

    public bool IsValid { get; private set; }

    private bool isRunning;
    private bool shouldReset;
    private bool stepMode;
    private bool shouldStep;

    public NesEmulator(Cartridge cartridge)
    {
        // Load Mapper
        LoadMapper(cartridge);
        if(Mapper == null)
        {
            return;
        }

        // Initialize Components
        cpu = new Cpu(this);
        ppu = new Ppu(this);
        apu = new Apu(this);
        controllerOne = new Controller();
        controllerTwo = new Controller();

        // Ready to Start
        IsValid = true;
    }

    public void Init()
    {
        // Check if emulator is ready to start
        if (!IsValid)
        {
            return;
        }

        isRunning = true;
    }

    public void Tick()
    {
        // Store what frame we are one
        var originalOddFrame = ppu.OddFrame;

        // Check to see if we are still running, or have finished rendering a frame
        while (isRunning && originalOddFrame == ppu.OddFrame)
        {
            // Check if we are running in single step
            if (stepMode & !shouldStep)
            {
                return;
            }

            shouldStep = false;

            // Check for reset
            if (shouldReset)
            {
                cpu.Reset();
                ppu.Reset();
                apu.Reset();

                originalOddFrame = ppu.OddFrame;

                shouldReset = false;
            }

            // Execute waiting callbacks
            OnStep?.Invoke();

            // Execute one CPU instruction
            var cycles = cpu.Step();

            // Catch-up PPU
            for (var i = 0; i < cycles * PpuDotsPerCpuCycle; i++)
            {
                ppu.Step();
            }

            // Catch-up APU
            for (var i = 0; i < cycles; i++)
            {
                apu.Step();
            }
        }
    }

    private void LoadMapper(Cartridge cartridge)
    {
        var mapper = cartridge.GetMapper();
        switch (mapper)
        {
            // NROM
            case 0:
                Mapper = new Nrom(cartridge);
                return;
            default:
                return;
        }
    }

    public void StepMode(bool newStepMode)
    {
        stepMode = newStepMode;
    }

    public void Step()
    {
        shouldStep = true;
    }

    public void Reset()
    {
        shouldReset = true;
    }

    public void Stop()
    {
        isRunning = false;
    }
}
