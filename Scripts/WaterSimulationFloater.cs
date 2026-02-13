using System;
using UnityEditor;
using UnityEngine;

[AddComponentMenu("WaterSimulator/Floater")]
[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(WaterSimulator.EXECUTION_ORDER + 1)]
public class WaterSimulationFloater : MonoBehaviour {

    public void SetSimulator(WaterSimulator simulator) {
        // TODO implement special logic at runtime
    }

#if false
    [Serializable]
    public class Floater {
        [SerializeField]
        public Vector3 offset = Vector3.zero;
        [SerializeField]
        public float size = 1f;
    }
    [SerializeField]
    public Floater[] floaters = new Floater[0];

    [SerializeField]
    private WaterSimulator simulator;
    
    [Header("Physic Settings")]
    [Tooltip("With a value greater 1 it will float and with a value smaller 1 it will sink")]
    [SerializeField]
    private float buoyancyAmount = 1f;
    [Header("Regular Drag")]
    [SerializeField] 
    private float drag;
    [SerializeField] 
    private float angularDrag;
    [Header("Water Drag")]
    [SerializeField] 
    private float underWaterDrag;
    [SerializeField] 
    private float underWaterAngularDrag;

    private Rigidbody _rigidbody;

    private Action<WaterSimulator.WaterSimulationSampler.WaterPositionInfo>[] _callbacks;
    
    private float _submergeAmountTotal = 0f;

    private void Awake() {
        _rigidbody = GetComponent<Rigidbody>();
        _callbacks = new Action<WaterSimulator.WaterSimulationSampler.WaterPositionInfo>[floaters.Length];
    }
    
#if UNITY_EDITOR
    private int _oldFloaterCount;
    private void OnValidate() {
        _oldFloaterCount = floaters.Length;
        if (Application.isPlaying && _oldFloaterCount != floaters.Length) {
            Debug.LogError("Changing the size of the floaters array at runtime is currently not supported");
            _oldFloaterCount = floaters.Length; // Only trigger once per change
        }
    }
#endif
    
    private void OnEnable() {
        for (int i = 0; i < floaters.Length; i++) {
            var floater = floaters[i];
            _callbacks[i] = info => OnWaterUpdate(floater, info);
            simulator.Sampler.Subscribe(_callbacks[i], transform.TransformPoint(floater.offset));
        }
        simulator.Sampler.OnBeforeCallback += OnBeforeWaterUpdate;
        simulator.Sampler.OnAfterCallback += OnAfterWaterUpdate;
    }

    private void OnDisable() {
        foreach (var callback in _callbacks)
            simulator.Sampler.Unsubscribe(callback);
        simulator.Sampler.OnBeforeCallback -= OnBeforeWaterUpdate;
        simulator.Sampler.OnAfterCallback -= OnAfterWaterUpdate;
    }

    private void OnBeforeWaterUpdate() {
        _submergeAmountTotal = 0f;
    }
    private void OnAfterWaterUpdate() {
        SetDrag(_submergeAmountTotal / floaters.Length);
    }
    
    private void OnWaterUpdate(Floater floater, WaterSimulator.WaterSimulationSampler.WaterPositionInfo info) {
        Vector3 position = transform.TransformPoint(floater.offset);
        if (position.y < info.globalGroundPos)
            return;
        if (info.depth <= 0f)
            return;
        
        float surfacePos = info.globalGroundPos + info.depth;
        float submergeAmount = Mathf.Clamp01((surfacePos - position.y + floater.size) / (2f * floater.size));
        _submergeAmountTotal += submergeAmount;
        if (submergeAmount != 0.0) {
            Vector3 velocityForce = new Vector3(info.velocity.x, 0f, info.velocity.y) * _rigidbody.mass;
            Vector3 buoyancyForce = new Vector3(0f, simulator.Gravity * submergeAmount * buoyancyAmount / floaters.Length, 0f);
            _rigidbody.AddForceAtPosition(velocityForce + buoyancyForce, position, ForceMode.Acceleration);
        }
    }

    private void FixedUpdate() {
        if (transform.hasChanged) {
            for (int i = 0; i < floaters.Length; i++) {
                var floater = floaters[i];
                Vector3 currentPos = transform.TransformPoint(floater.offset);
                Vector3 predictedPos = currentPos + _rigidbody.velocity * simulator.Sampler.SmoothedLatency();
                simulator.Sampler.UpdateValue(_callbacks[i], predictedPos);
            }
            transform.hasChanged = false;
        }
    }
    
    private void SetDrag(float value) {
        _rigidbody.drag = Mathf.Lerp(drag, underWaterDrag, value);
        _rigidbody.angularDrag = Mathf.Lerp(angularDrag, underWaterAngularDrag, value);
    }
    
    private void OnDrawGizmosSelected() {
        Gizmos.color = Color.green;
        foreach (Floater floater in floaters) {
            Gizmos.DrawSphere(transform.TransformPoint(floater.offset), floater.size);
        }
    }
}

[CustomEditor(typeof(WaterSimulationFloater))]
public class WaterSimulationFloaterEditor : ScriptlessEditor {
    private bool _editing = false;
    private Tool _lastTool;

    protected virtual void OnSceneGUI() {
        if (!_editing)
            return;
        var t = (WaterSimulationFloater)target;

        foreach (WaterSimulationFloater.Floater floater in t.floaters) {
            EditorGUI.BeginChangeCheck();
            Quaternion handleRotation =Tools.pivotRotation == PivotRotation.Local
                ? t.transform.rotation
                : Quaternion.identity;
            Vector3 newPos =
                Handles.PositionHandle(t.transform.TransformPoint(floater.offset), handleRotation);
            if (EditorGUI.EndChangeCheck()) {
                floater.offset = t.transform.InverseTransformPoint(newPos);
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
        DrawScriptlessDefaultInspector();
    }

    private void OnDisable() {
        if (_editing) {
            _editing = false;
            Tools.current = _lastTool;
        }
    }
#endif // false
}
