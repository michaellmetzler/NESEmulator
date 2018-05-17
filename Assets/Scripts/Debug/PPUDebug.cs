using Nes;
using System.Text;
using TMPro;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace NESEmulator
{
    public class PPUDebug : MonoBehaviour
    {
        [SerializeField]
        private RawImage[] paletteImages = default;
        [SerializeField]
        private RawImage[] patternImages = default;
        [SerializeField]
        private TextMeshProUGUI oamText = default;
        [SerializeField]
        private TextMeshProUGUI framesText = default;

        private Texture2D[] paletteTextures;
        private Texture2D[] patternTextures;
        private Color32[][] paletteBuffers;
        private Color32[][] patternBuffers;
        private byte[,] patternBuffer;
        private byte[] paletteBuffer;
        private NativeArray<Color32>[] nativePaletteTextures;
        private NativeArray<Color32>[] nativePatternTextures;

        private StringBuilder oamBuilder;
        private int currentPalette = 0;

        private Ppu ppu;
        private Color32[] palette;

        private void Start()
        {
            paletteTextures = new Texture2D[paletteImages.Length];
            paletteBuffers = new Color32[paletteImages.Length][];
            nativePaletteTextures = new NativeArray<Color32>[paletteImages.Length];
            for (int i = 0; i < paletteTextures.Length; i++)
            {
                paletteTextures[i] = new Texture2D(4, 1, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point
                };
                paletteImages[i].texture = paletteTextures[i];
                paletteBuffers[i] = new Color32[paletteTextures[i].width * paletteTextures[i].height];
                nativePaletteTextures[i] = paletteTextures[i].GetRawTextureData<Color32>();
            }

            patternTextures = new Texture2D[patternImages.Length];
            patternBuffers = new Color32[patternImages.Length][];
            nativePatternTextures = new NativeArray<Color32>[patternImages.Length];
            for (int i = 0; i < patternTextures.Length; i++)
            {
                patternTextures[i] = new Texture2D(16 * 8, 16 * 8, TextureFormat.RGBA32, false)
                {
                    filterMode = FilterMode.Point
                };
                patternImages[i].texture = patternTextures[i];
                patternBuffers[i] = new Color32[patternTextures[i].width * patternTextures[i].height];
                nativePatternTextures[i] = patternTextures[i].GetRawTextureData<Color32>();
            }

            patternBuffer = new byte[patternTextures[0].width, patternTextures[0].height];
            paletteBuffer = new byte[4];

            oamBuilder = new StringBuilder();
        }

        private void Update()
        {
            if (ppu == null)
            {
                return;
            }

            if (paletteImages != null)
            {
                var palettes = new byte[paletteTextures.Length][];

                var width = paletteTextures[0].width;
                var height = paletteTextures[0].height;
                var bufferLength = width * height;

                for (int i = 0; i < paletteTextures.Length; i++)
                {
                    FillPaletteBuffer(i);
                    palettes[i] = paletteBuffer;

                    for (int j = 0; j < bufferLength; j++)
                    {
                        paletteBuffers[i][j] = palette[palettes[i][j]];
                    }

                    nativePaletteTextures[i].CopyFrom(paletteBuffers[i]);

                    paletteTextures[i].Apply(false);
                }
            }

            if (patternImages != null)
            {
                var patternTables = new byte[patternTextures.Length][,];

                var width = patternTextures[0].width;
                var height = patternTextures[0].height;
                var bufferLength = width * height;

                for (int i = 0; i < patternTextures.Length; i++)
                {
                    FillPatternBuffer(i);
                    patternTables[i] = patternBuffer;

                    for (int j = 0; j < bufferLength; j++)
                    {
                        patternBuffers[i][j] = palette[patternTables[i][j % width, height - j / width - 1]];
                    }

                    nativePatternTextures[i].CopyFrom(patternBuffers[i]);

                    patternTextures[i].Apply(false);
                }
            }

            if (oamText != null)
            {
                oamBuilder.Clear();

                for (int i = 0; i < 64; i++)
                {
                    var oamSprite = ppu.GetOamSprite(i);
                    oamBuilder.Append($"{i,2} X:{oamSprite._x,3} Y:{oamSprite._y,3} I:{(byte)oamSprite._tileIndex:X2} A:{(byte)oamSprite._attributes:X2}\n");
                }

                oamText.text = oamBuilder.ToString();
            }

            if (framesText != null)
            {
                framesText.text = $"Frames:{ppu.TotalFrames}";
            }
        }

        private void OnApplicationQuit()
        {
            if (nativePaletteTextures != null)
            {
                foreach (var texture in nativePaletteTextures)
                {
                    if (texture.IsCreated)
                    {
                        texture.Dispose();
                    }
                }
            }

            if (nativePatternTextures != null)
            {
                foreach (var texture in nativePatternTextures)
                {
                    if (texture.IsCreated)
                    {
                        texture.Dispose();
                    }
                }
            }
        }

        public void SetCurrentPalette(int paletteNum)
        {
            currentPalette = paletteNum;
        }

        private void FillPaletteBuffer(int paletteNum)
        {
            for (int i = 0; i < 4; i++)
            {
                paletteBuffer[i] = ppu.ReadMemory((ushort)(Ppu.PalleteAddressStart + paletteNum * 4 + i));
            }
        }

        private void FillPatternBuffer(int tableNum)
        {
            for (var tileY = 0; tileY < 16; tileY++)
            {
                for (var tileX = 0; tileX < 16; tileX++)
                {
                    var offset = tileY * 256 + tileX * 16;

                    for (var row = 0; row < 8; row++)
                    {
                        var tile_lsb = ppu.ReadMemory((ushort)(tableNum * 0x1000 + offset + row));
                        var tile_msb = ppu.ReadMemory((ushort)(tableNum * 0x1000 + offset + row + 8));

                        for (var col = 7; col >= 0; col--)
                        {
                            var pixel = (tile_lsb & 0x01) + (tile_msb & 0x01);

                            tile_lsb >>= 1;
                            tile_msb >>= 1;

                            patternBuffer[tileX * 8 + col, tileY * 8 + row] = ppu.ReadMemory((ushort)(Ppu.PalleteAddressStart + currentPalette * 4 + pixel));
                        }
                    }
                }
            }
        }

        public void StartPpuDebug(Ppu newPpu, Color32[] newPalette)
        {
            ppu = newPpu;
            palette = newPalette;
        }
    }
}
