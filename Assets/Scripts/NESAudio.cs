using Nes;
using UnityEngine;

namespace NESEmulator
{
    [RequireComponent(typeof(AudioSource))]
    public class NESAudio : MonoBehaviour
    {
        private const float RadianConversion = 2.0f * Mathf.PI;

        [SerializeField]
        private float _frequency = 440.0f;

        private float _phase;
        private float _sampleRate;
        private float _sampleTime;

        private Apu _apu;

        private void Start()
        {
            _sampleRate = AudioSettings.outputSampleRate;
            _sampleTime = 1 / _sampleRate;
        }

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (_apu is null)
            {
                return;
            }
            
            var dataLen = data.Length / channels;

            var n = 0;
            while (n < dataLen)
            {
                var x = Mathf.Sin(_phase * _frequency * RadianConversion);
                var i = 0;
                while (i < channels)
                {
                    data[n * channels + i] += x;
                    i++;
                }
                _phase += _sampleTime;
                n++;
            }

        }

        public void StartOutput(Apu apu)
        {
            _apu = apu;
        }
    }
}
