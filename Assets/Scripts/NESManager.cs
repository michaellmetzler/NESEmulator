using Microsoft.Extensions.Logging;
using Nes;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NESEmulator
{
    public class NESManager : MonoBehaviour
    {
        [Serializable]
        private class LogFilter
        {
            public string category;
            public LogLevel logLevel;
        }

        [Serializable]
        private class LogLevelColor
        {
            public LogLevel logLevel;
            public Color color;
        }

        [SerializeField]
        private NESScreen _screen;

        [SerializeField]
        private NESAudio _audio;

        [SerializeField]
        private StandardController _controller;

        [SerializeField]
        private PaletteScriptableObject _palletteSO;
        public Color32[] Palette => _palletteSO.Palette;

        [Header("Logging")]

        [SerializeField]
        private List<LogFilter> logFilters;

        [SerializeField]
        private List<LogLevelColor> logLevelColors;

#if UNITY_EDITOR
        [Header("Debugging")]

        [SerializeField]
        private CPUDebug _cpuDebug;

        [SerializeField]
        private PPUDebug _ppuDebug;

        [SerializeField]
        private Framerate _framerate;

        [SerializeField]
        private Log _log;

        [SerializeField]
        private bool _stepMode;
#endif

        private long _frameCount;
        private double _nextUpdate;

        private Emulator _emulator;
        public Emulator Emulator => _emulator;

        private ILoggerFactory _loggerFactory;
        private Microsoft.Extensions.Logging.ILogger _logger;

        private void Awake()
        {
            _loggerFactory = LoggerFactory.Create(builder =>
            {
                foreach (var filter in logFilters)
                {
                    builder.AddFilter(filter.category, filter.logLevel);
                }

                builder.AddUnityConsoleLogger(options =>
                {
                    foreach (var levelColor in logLevelColors)
                    {
                        options.LogLevelToColorMap[levelColor.logLevel] = levelColor.color;
                    }
                });
             });

            _logger = _loggerFactory.CreateLogger<NESManager>();
        }

        private void Start()
        {
#if !UNITY_EDITOR
            StartEmulator(@"D:\Projects\NESEmulator\Roms\donkey kong.nes");
#endif
        }

        private void Update()
        {
            if (Emulator is null)
            {
                return;
            }

            _nextUpdate -= Time.deltaTime;

            if (_nextUpdate > 0)
            {
                return;
            }

            Emulator.Frame();

            _frameCount++;

            _nextUpdate += 1d / _emulator.frameRate;
        }

        private void OnDestroy()
        {
            StopEmulator();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            UpdateStepMode();
        }
#endif

        public void StartEmulator(string path)
        {
            var cartridge = Cartridge.Create(path, _loggerFactory);
            if (cartridge is null)
            {
                return;
            }

            _emulator = Emulator.Create(cartridge, _loggerFactory);
            if (_emulator is null)
            {
                return;
            }

            _screen?.StartOutput(_emulator.Ppu, Palette);

            _audio?.StartOutput(_emulator.Apu);

            _controller?.StartController(_emulator.ControllerOne, _emulator.ControllerTwo);

#if UNITY_EDITOR
            _cpuDebug?.StartCpuDebug(_emulator.Cpu);

            _ppuDebug?.StartPpuDebug(_emulator.Ppu, Palette);

            _framerate?.StartFramerate(_emulator.Ppu);

            _log?.StartLog(_emulator);

            UpdateStepMode();
#endif
        }

#if UNITY_EDITOR
        private void UpdateStepMode() => _emulator?.StepMode(_stepMode);
        public void StepEmulator() => _emulator?.Step();
#endif
        public void ResetEmulator() => _emulator?.Reset();
        public void StopEmulator() => _emulator?.Stop();
    }
}
