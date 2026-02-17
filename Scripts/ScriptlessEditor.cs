namespace JonasWischeropp.Unity.WaterSimulation.Editor {

public class ScriptlessEditor : UnityEditor.Editor {
    public override void OnInspectorGUI() {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, new string[]{"m_Script"});
        serializedObject.ApplyModifiedProperties();
    }
}

} // namespace JonasWischeropp.Unity.WaterSimulation.Editor
