using UnityEngine;

[CreateAssetMenu(fileName = "Palette", menuName = "ScriptableObjects/PaletteScriptableObject")]
public class PaletteScriptableObject : ScriptableObject
{
    public Color32[] palette;
}
