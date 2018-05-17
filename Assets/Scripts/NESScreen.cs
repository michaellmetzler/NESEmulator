using Nes;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NESEmulator
{
    [RequireComponent(typeof(RawImage))]
    public class NESScreen : MonoBehaviour
    {
        private RawImage _screenImage;
        private Texture2D _screenTexture;
        private Color32[] _screenBuffer;
        private int _bufferLength;
        private NativeArray<Color32> _nativeScreenTexture;

        private NESManager _manager;
        private Ppu _ppu;

        private void Start()
        {
            _screenImage = GetComponent<RawImage>();

            _screenTexture = new Texture2D(Emulator.ScreenWidth, Emulator.ScreenHeight, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Point
            };
            _screenImage.texture = _screenTexture;
            _bufferLength = _screenTexture.width * _screenTexture.height;
            _screenBuffer = new Color32[_bufferLength];

            // RGBA32 texture format data layout exactly matches Color32 struct
            _nativeScreenTexture = _screenTexture.GetRawTextureData<Color32>();
        }

        private void LateUpdate()
        {
            if (_screenTexture is null || _ppu is null)
            {
                return;
            }

            var palette = _manager.Palette;
            var screenPixels = _ppu.ScreenPixels;

            for (int i = 0; i < _bufferLength; i++)
            {
                _screenBuffer[i] = palette[screenPixels[i]];
            }
            var nativeScreenBuffer = new NativeArray<Color32>(_screenBuffer, Allocator.Temp);

            nativeScreenBuffer.CopyTo(_nativeScreenTexture);
            nativeScreenBuffer.Dispose();

            _screenTexture.Apply(false);
        }

        private void OnApplicationQuit()
        {
            if (_nativeScreenTexture.IsCreated)
            {
                _nativeScreenTexture.Dispose();
            }
        }

        public void StartOutput(NESManager newManager)
        {
            _manager = newManager;
            _ppu = _manager.Emulator.Ppu;
        }
    }
}
