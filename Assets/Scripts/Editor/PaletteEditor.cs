using System.IO;
using UnityEditor;
using UnityEngine;

namespace NESEmulator
{
    [CustomEditor(typeof(PaletteScriptableObject))]
    public class PaletteEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            var paletteSO = target as PaletteScriptableObject;

            if (GUILayout.Button("Load Palette"))
            {
                var path = EditorUtility.OpenFilePanel("Load Palette", "", "pal");

                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                using var stream = new FileStream(path, FileMode.Open, FileAccess.Read);
                using var reader = new BinaryReader(stream);

                var paletteBuffer = new byte[64 * 3];
                reader.Read(paletteBuffer, 0, 64 * 3);

                for (int i = 0; i < 64; i++)
                {
                    paletteSO.Palette[i] = new Color32(paletteBuffer[i * 3],
                                                       paletteBuffer[i * 3 + 1],
                                                       paletteBuffer[i * 3 + 2],
                                                       255);
                }
            }
        }
    }
}
