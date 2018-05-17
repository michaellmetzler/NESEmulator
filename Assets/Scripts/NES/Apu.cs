using System;
using System.Collections.Concurrent;

using static Nes.Constants;

namespace Nes
{
    public class Apu
    {
        private const int NtscCpuClockSpeed = 1789773;

        // Sample buffer
        private readonly ConcurrentQueue<float> _sampleBuffer;
        public ConcurrentQueue<float> SampleBuffer => SampleBuffer;

        // Sample rate configuration
        private int _sampleRate;
        private double _cyclesPerSample;

        private readonly Emulator _emulator;

        public Apu(Emulator emulator)
        {
            Reset();
        }

        public void Reset()
        {

        }

        public void Step()
        {
          
        }
        public void WriteRegister(ushort address, byte value)
        {

        }

        public void SetSampleRate(int sampleRate)
        {
            _sampleRate = sampleRate;
            _cyclesPerSample = (double)NtscCpuClockSpeed / _sampleRate;
        }
    }
}
