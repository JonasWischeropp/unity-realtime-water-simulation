using UnityEditor;

public class ScriptlessEditor : Editor {
    public override void OnInspectorGUI() {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, new string[]{"m_Script"});
        serializedObject.ApplyModifiedProperties();
    }
}
