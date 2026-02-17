using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace JonasWischeropp.Unity.WaterSimulation {

using WaterPositionInfo = WaterSimulatorSampler.WaterPositionInfo;

[AddComponentMenu("WaterSimulator/Floater")]
[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(WaterSimulator.EXECUTION_ORDER + 2)]
public class WaterSimulationFloater : MonoBehaviour {
    [Serializable]
    public class Floater {
        [SerializeField]
        public Vector3 Offset = Vector3.zero;
        [SerializeField]
        public float Size = 1f;
    }

    [SerializeField]
    WaterSimulatorSampler _sampler;

    [SerializeField]
    Floater[] _floaters = new Floater[]{new Floater()};

    [SerializeField]
    bool _positionPrediction = true;

    [Header("Physic Settings")]
    [Tooltip("With a value greater 1 it will float and with a value smaller 1 it will sink")]
    [SerializeField]
    float _buoyancyAmount = 3f; // TODO maybe use density instead?
    [Header("Regular Damping")]
    [SerializeField] 
    float _linearDamping = 0f;
    [SerializeField] 
    float _angularDamping = 0.05f;
    [Header("Water Damping")]
    [SerializeField] 
    float _underWaterLinearDamping = 4f;
    [SerializeField] 
    float _underWaterAngularDamping = 0.8f;

    Rigidbody _rigidbody;

    Action<WaterPositionInfo>[] _callbacks;
    WaterPositionInfo[] _infos;

    void Awake() {
        if (_sampler == null) {
            Debug.LogError("Sampler is not assigned", this);
            enabled = false;
            return;
        }

        _rigidbody = GetComponent<Rigidbody>();
        _callbacks = new Action<WaterPositionInfo>[_floaters.Length];
        _infos = new WaterPositionInfo[_floaters.Length];
    }

    void OnEnable() {
        for (int i = 0; i < _floaters.Length; i++) {
            int iCopy = i;
            _callbacks[i] = info => OnWaterUpdate(iCopy, info);
            _sampler.Subscribe(_callbacks[i], transform.TransformPoint(_floaters[i].Offset));
        }
    }

    void OnDisable() {
        foreach (var callback in _callbacks) {
            _sampler.Unsubscribe(callback);
        }
    }

    void FixedUpdate() {
        float submergeTotal = 0f;

        for (int i = 0; i < _floaters.Length; i++) {
            Floater floater = _floaters[i];
            WaterPositionInfo info = _infos[i];

            Vector3 currentPos = transform.TransformPoint(floater.Offset);

            // Apply physics
            if (currentPos.y >= info.GlobalGroundPos && info.Depth > 0f) {
                float surfacePos = info.GlobalGroundPos + info.Depth;
                float submergeAmount = Mathf.Clamp01((surfacePos - currentPos.y + floater.Size) / (2f * floater.Size));
                submergeTotal += submergeAmount;
                if (submergeAmount != 0.0) {
                    // TODO currently the rotation of simulator is not considered (probably also in manipulator)
                    Vector3 velocityForce = new Vector3(info.Velocity.x, 0f, info.Velocity.y) * _rigidbody.mass;
                    Vector3 buoyancyForce = new Vector3(0f, _sampler.Simulator.Gravity * _buoyancyAmount, 0f);
                    _rigidbody.AddForceAtPosition((velocityForce + buoyancyForce) * submergeAmount / _floaters.Length, currentPos, ForceMode.Acceleration);
                }
            }

            // Update positions
            if (transform.hasChanged) {
                Vector3 predictedPos = currentPos + _rigidbody.linearVelocity * _sampler.GetSmoothedLatency();
                _sampler.UpdatePosition(_callbacks[i], _positionPrediction ? predictedPos : currentPos);
            }
        }
        transform.hasChanged = false;

        SetDrag(submergeTotal / _floaters.Length);
    }

    void SetDrag(float value) {
        _rigidbody.linearDamping = Mathf.Lerp(_linearDamping, _underWaterLinearDamping, value);
        _rigidbody.angularDamping = Mathf.Lerp(_angularDamping, _underWaterAngularDamping, value);
    }

    void OnWaterUpdate(int i, WaterPositionInfo info) {
        _infos[i] = info;
    }

#if UNITY_EDITOR
    public void SetSimulatorSampler(WaterSimulatorSampler newSampler) {
        if (_sampler == newSampler) {
            return;
        }

        if (EditorApplication.isPlaying && enabled) {
            for (int i = 0; i < _floaters.Length; i++) {
                _sampler.Unsubscribe(_callbacks[i]);
                newSampler?.Subscribe(_callbacks[i], transform.TransformPoint(_floaters[i].Offset));
            }
        }
        _sampler = newSampler;
    }

    void OnDrawGizmosSelected() {
        foreach (Floater floater in _floaters) {
            Gizmos.DrawSphere(transform.TransformPoint(floater.Offset), 0.5f * floater.Size);
        }
        
        if (_infos == null) {
            return;
        }

        for (int i = 0; i < _floaters.Length; i++) {
            Vector3 floaterPos = transform.TransformPoint(_floaters[i].Offset);
            Vector3 groundPos = floaterPos;
            groundPos.y = _infos[i].GlobalGroundPos;
            Vector3 surfacePos = groundPos;
            surfacePos.y += _infos[i].Depth;
            Gizmos.color = Color.green;
            Gizmos.DrawLine(floaterPos, groundPos);
            Gizmos.DrawSphere(surfacePos, 0.5f);
            Gizmos.color = Color.red;
            Gizmos.DrawCube(groundPos, 0.5f * Vector3.one);
        }
    }

    [CustomEditor(typeof(WaterSimulationFloater))]
    public class WaterSimulationFloaterEditor : Editor.ScriptlessEditor {
        private bool _editing = false;
        private Tool _lastTool;

        protected virtual void OnSceneGUI() {
            if (!_editing) {
                return;
            }
            var t = (WaterSimulationFloater)target;

            foreach (Floater floater in t._floaters) {
                EditorGUI.BeginChangeCheck();
                Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Local
                    ? t.transform.rotation
                    : Quaternion.identity;
                Vector3 newPos =
                    Handles.PositionHandle(t.transform.TransformPoint(floater.Offset), handleRotation);
                if (EditorGUI.EndChangeCheck()) {
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
#endif
}

} // namespace JonasWischeropp.Unity.WaterSimulation
