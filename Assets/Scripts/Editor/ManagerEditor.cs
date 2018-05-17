using UnityEditor;
using UnityEngine;

namespace NESEmulator
{
    [CustomEditor(typeof(NESManager))]
    public class EmulatorEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            if (EditorApplication.isPlaying)
            {
                var manager = target as NESManager;

                if (GUILayout.Button("Step"))
                {
                    manager.StepEmulator();
                }

                if (GUILayout.Button("Load ROM"))
                {
                    var path = EditorUtility.OpenFilePanel("Load ROM", "", "nes");

                    if (!string.IsNullOrEmpty(path))
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
}
