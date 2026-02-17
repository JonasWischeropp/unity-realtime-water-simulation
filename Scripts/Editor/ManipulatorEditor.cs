using UnityEditor;

namespace JonasWischeropp.Unity.WaterSimulation.Editor {

[CustomEditor(typeof(Manipulator)), CanEditMultipleObjects]
public class ManipulatorEditor : ScriptlessEditor {
    public override void OnInspectorGUI() {
        using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying)) {
            base.OnInspectorGUI();
        }
    }
}

} // namespace JonasWischeropp.Unity.WaterSimulation.Editor
