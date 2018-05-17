using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NESManager))]
public class EmulatorEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        if (EditorApplication.isPlaying)
        {
            NESManager manager = (NESManager)target;

            if (GUILayout.Button("Step"))
            {
                manager.StepEmulator();
            }

            if (GUILayout.Button("Load ROM"))
            {
                string path = EditorUtility.OpenFilePanel("Load ROM", "", "nes");

                if (path.Length != 0)
                {
                    manager.StartEmulator(path);
                }
            }
            if (GUILayout.Button("Reset"))
            {
                manager.ResetEmulator();
            }
        }
    }
}