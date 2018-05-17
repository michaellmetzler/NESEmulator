using Nes;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.UI;

namespace NESEmulator
{
    [RequireComponent(typeof(RawImage))]
    public unsafe class NESScreen : MonoBehaviour
    {
        private Texture2D _screenTexture;
        private Color32* _texturePtr;

        private NativeArray<byte> _nativeScreenPixels;
        private byte* _screenPtr;

        private NativeArray<Color32> _nativePalette;
        private Color32* _palettePtr;

        private int _bufferLength;

        private bool _lastOddFrame;

        private RawImage _screenImage;

        private Ppu _ppu;

        private void Start()
        {
            _screenImage = GetComponent<RawImage>();
        }

        private void LateUpdate()
        {
            if (_screenTexture is null || _ppu is null)
            {
                return;
            }

            var currentOddFrame = _ppu.OddFrame;
            if (_lastOddFrame != currentOddFrame)
            {
                return;
            }
            _lastOddFrame = currentOddFrame;

            _nativeScreenPixels.CopyFrom(_ppu.ScreenPixels);

            for (int i = 0; i < _bufferLength; i++)
            {
                _texturePtr[i] = _palettePtr[_screenPtr[i]];
            }

            _screenTexture.Apply(false);
        }

        private void OnApplicationQuit()
        {
            if (_nativeScreenPixels.IsCreated)
            {
                _nativeScreenPixels.Dispose();
            }

            if (_nativePalette.IsCreated)
            {
                _nativePalette.Dispose();
            }
        }

        public void StartOutput(Ppu ppu, Color32[] palette)
        {
            _ppu = ppu;

            _screenTexture = new Texture2D(_ppu.ScreenWidth, _ppu.ScreenHeight, TextureFormat.RGBA32, false) { filterMode = FilterMode.Point };
            _texturePtr = (Color32*)_screenTexture.GetRawTextureData<Color32>().GetUnsafePtr();
            _bufferLength = _screenTexture.width * _screenTexture.height;

            _nativeScreenPixels = new NativeArray<byte>(_ppu.ScreenPixels, Allocator.Persistent);
            _screenPtr = (byte*)_nativeScreenPixels.GetUnsafeReadOnlyPtr();

            _nativePalette = new NativeArray<Color32>(palette, Allocator.Persistent);
            _palettePtr = (Color32*)_nativePalette.GetUnsafeReadOnlyPtr();

            _lastOddFrame = !_ppu.OddFrame;

            _screenImage.texture = _screenTexture;
        }
    }
}
