using Microsoft.Extensions.Logging;
using System;

namespace Nes
{
    [Serializable]
    public class Emulator
    {
        private const int NtscFrameRate = 60;

        public int frameRate;

        private Cpu _cpu;
        public Cpu Cpu => _cpu;

        private Ppu _ppu;
        public Ppu Ppu => _ppu;

        private Apu _apu;
        public Apu Apu => _apu;

        private Controller _controllerOne;
        public Controller ControllerOne => _controllerOne;

        private Controller _controllerTwo;
        public Controller ControllerTwo => _controllerTwo;

        private Mapper _mapper;
        public Mapper Mapper => _mapper;

        private Action _onBeforeStep;

        private bool _isRunning;
        private bool _shouldReset;
        private bool _stepMode;
        private bool _shouldStep;

        private ILogger _logger;

        private Emulator(ILogger logger)
        {
            _logger = logger;
        }

        public static Emulator Create(Cartridge cartridge, ILoggerFactory loggerFactory)
        {
            var logger = loggerFactory.CreateLogger<Emulator>();

            var emulator = new Emulator(logger);

            // Load Mapper
            emulator._mapper = cartridge.Mapper switch
            {
                000 => new Nrom(cartridge),
                001 or 105 or 155 => new Mmc1(cartridge),
                002 or 094 or 180 => new UxRom(cartridge),
                003 or 185 => new CnRom(cartridge),
                _ => null,
            };

            if (emulator._mapper == null)
            {
                return null;
            }

            emulator.frameRate = NtscFrameRate;

            emulator._cpu = new (emulator);
            emulator._ppu = new (emulator);
            emulator._apu = new (emulator);
            emulator._controllerOne = new ();
            emulator._controllerTwo = new ();

            emulator._isRunning = true;

            return emulator;
        }

        public void Frame()
        {
            var originalOddFrame = _ppu.OddFrame;

            while (_isRunning && originalOddFrame == _ppu.OddFrame)
            {
                if (_stepMode && !_shouldStep)
                {
                    return;
                }

                _shouldStep = false;

                if (_shouldReset)
                {
                    _cpu.Reset();
                    _ppu.Reset();
                    _apu.Reset();

                    originalOddFrame = _ppu.OddFrame;

                    _shouldReset = false;
                }

                _onBeforeStep?.Invoke();

                var cycles = _cpu.Step();

                for (var i = 0; i < cycles * _ppu.PpuDotsPerCpuCycle; i++)
                {
                    _ppu.Step();
                }

                for (var i = 0; i < cycles; i++)
                {
                    _apu.Step();
                }
            }
        }

        public void StepMode(bool newStepMode)
        {
            _stepMode = newStepMode;
        }

        public void Step()
        {
            _shouldStep = true;
        }

        public void Reset()
        {
            _shouldReset = true;
        }

        public void Stop()
        {
            _isRunning = false;
        }

        public void RegisterOnBeforeStep(Action newOnBeforeStep)
        {
            _onBeforeStep += newOnBeforeStep;
        }

        public void UnregisterOnBeforeStep(Action newOnBeforeStep)
        {
            _onBeforeStep -= newOnBeforeStep;
        }
    }
}
