using UnityEngine;
using UnityEditor;

namespace JonasWischeropp.Unity.WaterSimulation.Editor {
using static Floater;

[CustomEditor(typeof(Floater)), CanEditMultipleObjects]
public class FloaterEditor : ScriptlessEditor {
    private bool _editing = false;
    private Tool _lastTool;

    protected virtual void OnSceneGUI() {
        if (!_editing) {
            return;
        }
        var t = (Floater)target;

        foreach (FloaterPoint floater in t.GetFloaters()) {
            EditorGUI.BeginChangeCheck();
            Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Local
                ? t.transform.rotation
                : Quaternion.identity;
            Vector3 newPos =
                Handles.PositionHandle(t.transform.TransformPoint(floater.Offset), handleRotation);
            if (EditorGUI.EndChangeCheck()) {
                Undo.RecordObject(t, "Move FloaterPoint");
                floater.Offset = t.transform.InverseTransformPoint(newPos);
            }
        }
    }

    public override void OnInspectorGUI() {
        Texture2D icon = EditorGUIUtility.FindTexture("MoveTool");
        if (_editing != GUILayout.Toggle(_editing, icon, new GUIStyle(GUI.skin.button))) {
            _editing = ! _editing;
            if (_editing) {
                _lastTool = Tools.current;
                Tools.current = Tool.None;
            }
            else {
                Tools.current = _lastTool;
            }
        }
        base.OnInspectorGUI();
    }

    private void OnDisable() {
        if (_editing) {
            _editing = false;
            Tools.current = _lastTool;
        }
    }
}

} // namespace JonasWischeropp.Unity.WaterSimulation.Editor
