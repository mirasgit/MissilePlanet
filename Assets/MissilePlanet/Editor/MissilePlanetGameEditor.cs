#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace MissilePlanet.Editor
{
    [CustomEditor(typeof(MissilePlanetGame))]
    public sealed class MissilePlanetGameEditor : UnityEditor.Editor
    {
        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("Runtime Debug", EditorStyles.boldLabel);

            MissilePlanetGame game = (MissilePlanetGame)target;

            using (new EditorGUI.DisabledScope(!Application.isPlaying))
            {
                if (GUILayout.Button("SPAWN BOSS NOW", GUILayout.Height(32f)))
                    game.DebugSpawnBossNow();
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox(
                    "Enter Play Mode to use SPAWN BOSS NOW. For a direct boss test, set Debug Start Wave to 10/20/30 before pressing START.",
                    MessageType.Info);
            }
        }
    }
}
#endif
