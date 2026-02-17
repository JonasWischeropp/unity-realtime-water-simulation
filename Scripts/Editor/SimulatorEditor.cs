using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace JonasWischeropp.Unity.WaterSimulation.Editor {

[CustomEditor(typeof(Simulator))]
public class SimulatorEditor : ScriptlessEditor {
    BoxBoundsHandle _boxHandle = new BoxBoundsHandle();
    bool _editingBox = false;
    Tool _lastTool;

    bool _isInEditMode; // TODO

    Texture2D _editBoxIcon;
    
    void OnEnable() {
        _editBoxIcon = EditorGUIUtility.FindTexture("EditCollider");
        _isInEditMode = Application.isEditor && !Application.isPlaying;
    }

    void OnDisable() {
        if (_editingBox) {
            _editingBox = false;
            Tools.current = _lastTool;
        }
    }

    void OnDestroy() {
        // if (_isInEditMode && target == null) {
        //     // Calling CleanUp on null is intended. Not actually null, == is overwritten.
        //     ((WaterSimulator)target).ComponentRemovedCleanUp();
        // }
    }

    void OnSceneGUI() {
        if (!_editingBox) {
            return;
        }
        Simulator simulator = (Simulator)target;

        _boxHandle.center = simulator.GetCenter();
        _boxHandle.size = simulator.GetSize();
        _boxHandle.SetColor(Simulator.GIZMO_COLOR);

        EditorGUI.BeginChangeCheck();
        _boxHandle.DrawHandle();
        if (EditorGUI.EndChangeCheck()) {
            Undo.RecordObject(simulator, "Resizing Bounds");
            simulator.SetBounds(_boxHandle.center, _boxHandle.size);
        }
    }

    public override void OnInspectorGUI() {
        using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying)) {
            if (_editingBox != GUILayout.Toggle(_editingBox, _editBoxIcon, GUI.skin.button)) {
                if (_editingBox) {
                    StopEdit();
                }
                else {
                    StartEdit();
                }
            }
        }
        // TODO deactivate the right fields while playing
        base.OnInspectorGUI();
    }
    
    void StartEdit() {
        _editingBox = true;
        _lastTool = Tools.current;
        Tools.current = Tool.Custom;
    }
    
    void StopEdit() {
        _editingBox = false;
        Tools.current = _lastTool;
    }
}

} // namespace JonasWischeropp.Unity.WaterSimulation.Editor
