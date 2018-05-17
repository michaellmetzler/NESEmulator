using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(PaletteScriptableObject))]
public class PaletteEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        var paletteSO = (PaletteScriptableObject)target;

        if (GUILayout.Button("Load Palette"))
        {
            string path = EditorUtility.OpenFilePanel("Load Palette", "", "pal");

            if (path.Length != 0)
            {
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Read))
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        var paletteBuffer = new byte[64 * 3];
                        reader.Read(paletteBuffer, 0, 64 * 3);

                        paletteSO.palette = new Color32[64];
                        for (int i = 0; i < 64; i++)
                        {
                            paletteSO.palette[i] = new Color32(paletteBuffer[i * 3],
                                                               paletteBuffer[i * 3 + 1],
                                                               paletteBuffer[i * 3 + 2],
                                                               255);
                        }
                    }
                }
            }
        }
    }
}