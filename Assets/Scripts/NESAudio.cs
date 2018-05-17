using Nes;
using UnityEngine;

namespace NESEmulator
{
    [RequireComponent(typeof(AudioSource))]
    public class NESAudio : MonoBehaviour
    {
        private float _lastSample;

        private Apu _apu;

        void OnAudioFilterRead(float[] data, int channels)
        {
            if (_apu == null)
            {
                return;
            }

            var buffer = _apu.SampleBuffer;

            for (int i = 0; i < data.Length; i++)
            {
                if (buffer.TryDequeue(out float sample))
                {
                    _lastSample = sample;
                }

                data[i] = _lastSample;
            }
        }

        public void StartOutput(Apu apu)
        {
            _apu = apu;
            _apu.SetSampleRate(AudioSettings.outputSampleRate);
        }
    }
}
