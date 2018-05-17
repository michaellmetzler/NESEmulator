using Nes;
using UnityEngine;

namespace NESEmulator
{
    public class NESManager : MonoBehaviour
    {
        [SerializeField]
        private NESScreen _screen;

        [SerializeField]
        private NESAudio _audio;

        [SerializeField]
        private StandardController _controller;

        [SerializeField]
        private PaletteScriptableObject _palletteSO;
        public Color32[] Palette => _palletteSO.Palette;

#if UNITY_EDITOR
        [Header("Debugging")]
        [SerializeField]
        private bool _stepMode;
#endif
        public Emulator Emulator { get; private set; }

        private long _frameCount;
        private double _nextUpdate;

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

            _nextUpdate += 1d / Emulator.ApuFrameCounterRate;
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
            var cartridge = Cartridge.Create(path);
            if (cartridge is null)
            {
                return;
            }

            Emulator = Emulator.Create(cartridge);
            if (Emulator is null)
            {
                return;
            }

            _screen.StartOutput(this);
            _audio.StartOutput(Emulator.Apu);
            _controller.StartController(Emulator);

#if UNITY_EDITOR
            UpdateStepMode();
#endif
        }

#if UNITY_EDITOR
        private void UpdateStepMode() => Emulator?.StepMode(_stepMode);
        public void StepEmulator() => Emulator?.Step();
#endif
        public void ResetEmulator() => Emulator?.Reset();
        public void StopEmulator() => Emulator?.Stop();
    }
}
