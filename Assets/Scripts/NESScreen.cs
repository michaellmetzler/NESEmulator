using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RawImage))]
public class NESScreen : MonoBehaviour
{
    private RawImage screenImage;
    private Texture2D screenTexture;
    private Color32[] screenBuffer;
    private int bufferLength;

    private NESManager manager;
    private Ppu ppu;

    private void Start()
    {
        screenImage = GetComponent<RawImage>();
    }

    private void LateUpdate()
    {
        if (screenTexture != null && ppu != null)
        {
            var palette = manager.Palette;
            var screenPixels = ppu.ScreenPixels;

            // Look up colors in palette
            for (int i = 0; i < bufferLength; i++)
            {
                screenBuffer[i] = palette[screenPixels[i]];
            }

            // RGBA32 texture format data layout exactly matches Color32 struct
            var nativeScreenTexture = screenTexture.GetRawTextureData<Color32>();
            var nativeScreenBuffer = new NativeArray<Color32>(screenBuffer, Allocator.Temp);

            nativeScreenBuffer.CopyTo(nativeScreenTexture);
            nativeScreenBuffer.Dispose();

            // Upload to the GPU
            screenTexture.Apply(false);
        }
    }

    public void StartOutput(NESManager newManager)
    {
        manager = newManager;
        ppu = manager.Emulator.ppu;

        // Create textures and buffers that match the screen size
        screenTexture = new Texture2D(NesEmulator.ScreenWidth, NesEmulator.ScreenHeight, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point
        };
        screenImage.texture = screenTexture;
        bufferLength = screenTexture.height * screenTexture.width;
        screenBuffer = new Color32[bufferLength];
    }
}
