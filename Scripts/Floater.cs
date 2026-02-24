using System;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace JonasWischeropp.Unity.WaterSimulation {

using PositionInfo = Sampler.PositionInfo;

[AddComponentMenu(Simulator.SIM_MENU_GROUP + "Water Floater")]
[RequireComponent(typeof(Rigidbody))]
[DefaultExecutionOrder(Simulator.EXECUTION_ORDER + 2)]
public class Floater : MonoBehaviour {
    [Serializable]
    public class FloaterPoint {
        [SerializeField]
        public Vector3 Offset = Vector3.zero;
        [SerializeField]
        public float Size = 1f;
    }

    [SerializeField, NoPlayModeEdit]
    Sampler _sampler;

    [SerializeField]
    FloaterPoint[] _floaters = new FloaterPoint[]{new FloaterPoint()};

    [SerializeField]
    bool _positionPrediction = true;

    [Header("Physic Settings")]
    [Tooltip("With a value greater 1 it will float and with a value smaller 1 it will sink")]
    [SerializeField]
    float _buoyancyAmount = 3f; // TODO maybe use density instead?
    [SerializeField]
    float _accelerationSpeed = 6f;
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

    Action<PositionInfo>[] _callbacks;
    PositionInfo[] _infos;

    void Awake() {
        if (_sampler == null) {
            Debug.LogError("Sampler is not assigned", this);
            enabled = false;
            return;
        }

        if (_sampler.Simulator.IsLayerConflicting(gameObject.layer)) {
            Debug.LogError("The Floater should not be on a layer that is used by the Simulator GroundLayer", this);
            enabled = false;
            return;
        }

        _rigidbody = GetComponent<Rigidbody>();
        _callbacks = new Action<PositionInfo>[_floaters.Length];
        _infos = new PositionInfo[_floaters.Length];
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
            FloaterPoint floater = _floaters[i];
            PositionInfo info = _infos[i];

            Vector3 currentPos = transform.TransformPoint(floater.Offset);

            // Apply physics
            if (currentPos.y >= info.GlobalGroundPos && info.Depth > 0f) {
                float surfacePos = info.GlobalGroundPos + info.Depth;
                float submergeAmount = Mathf.Clamp01((surfacePos - currentPos.y + floater.Size) / (2f * floater.Size));
                submergeTotal += submergeAmount;
                if (submergeAmount != 0.0) {
                    // TODO currently the rotation of simulator is not considered (probably also in manipulator)
                    Vector3 velocity = new Vector3(info.Velocity.x, 0f, info.Velocity.y);
                    Vector3 requiredVelocity = velocity - _rigidbody.linearVelocity;
                    Vector3 buoyancy = new Vector3(0f, _sampler.Simulator.Gravity * _buoyancyAmount, 0f);

                    Vector3 force = (requiredVelocity * _accelerationSpeed + buoyancy) * submergeAmount / _floaters.Length;
                    _rigidbody.AddForceAtPosition(force, currentPos, ForceMode.Force);
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

    void OnWaterUpdate(int i, PositionInfo info) {
        _infos[i] = info;
    }

    public FloaterPoint[] GetFloaters() { // TODO internal?
        return _floaters;
    }

#if UNITY_EDITOR
    public void SetSimulatorSampler(Sampler newSampler) {
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
        foreach (FloaterPoint floater in _floaters) {
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
#endif
}

} // namespace JonasWischeropp.Unity.WaterSimulation
