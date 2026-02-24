#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace JonasWischeropp.Unity.WaterSimulation {

public class NoPlayModeEditAttribute : PropertyAttribute {
}

namespace Editor {
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(NoPlayModeEditAttribute))]
public class NoPlayModeEditDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying)) {
            EditorGUI.PropertyField(position, property, label);
        }
    }
}
#endif
} // namespace Editor

} // namespace JonasWischeropp.Unity.WaterSimulation
