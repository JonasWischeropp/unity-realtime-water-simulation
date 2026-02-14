#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

[AddComponentMenu("WaterSimulator/Water Manipulator")]
[DefaultExecutionOrder(WaterSimulator.EXECUTION_ORDER + 1)]
public class WaterManipulator : MonoBehaviour {
    [SerializeField] WaterSimulator _simulator;

    bool IsUniformScale(Vector3 scale) {
        return scale.x == scale.y && scale.y == scale.z;
    }

    void Awake() {
        if (_simulator == null) {
            Debug.LogError("Simulator is not assigned", this);
            enabled = false;
        }
    }

    void Update() {
#if UNITY_EDITOR
        if (!IsUniformScale(transform.localScale)) {
            Debug.LogError("Only uniform scale s allowed", this);
            enabled = false;
        }
#endif

        if (transform.hasChanged) {
            transform.hasChanged = false;
            _simulator?.UpdateManipulator(this, GetPosition(), GetSize());
        }
    }

    void OnEnable() {
        _simulator?.AddManipulator(this, GetPosition(), GetSize());
    }

    void OnDisable() {
        _simulator?.RemoveManipulator(this);
    }

    Vector3 GetPosition() {
        return transform.position;
    }

    float GetSize() {
        return transform.localScale.x; // TODO should lossyScale be used and what needs to be adjusted?
    }

#if UNITY_EDITOR
    public void SetSimulator(WaterSimulator simulator) {
        if (simulator == _simulator) {
            return;
        }

        if (EditorApplication.isPlaying && enabled) {
            _simulator?.RemoveManipulator(this);
            _simulator = simulator;
            _simulator?.AddManipulator(this, GetPosition(), GetSize());
        }
        else {
            _simulator = simulator;
        }
    }
#endif

    void OnDrawGizmosSelected() {
        Handles.color = Color.blue;
        Handles.DrawWireDisc(transform.position, transform.up, GetSize());
    }
}

#if UNITY_EDITOR
[CustomEditor(typeof(WaterManipulator))]
public class WaterManipulatorEditor : ScriptlessEditor {
    public override void OnInspectorGUI() {
        using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying)) {
            base.OnInspectorGUI();
        }
    }
}
#endif
