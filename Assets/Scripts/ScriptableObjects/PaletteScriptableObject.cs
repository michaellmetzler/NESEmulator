using UnityEngine;

namespace NESEmulator
{
    [CreateAssetMenu(fileName = "Palette", menuName = "ScriptableObjects/PaletteScriptableObject")]
    public class PaletteScriptableObject : ScriptableObject
    {
        public Color32[] Palette = new Color32[64];
    }
}
