using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace JonasWischeropp.Unity.WaterSimulation {

[AddComponentMenu(Simulator.SIM_MENU_GROUP + "Water Sampler")]
[RequireComponent(typeof(Simulator))]
[DefaultExecutionOrder(Simulator.EXECUTION_ORDER + 1)]
public class Sampler : MonoBehaviour, IDisposable {
    const int KERNEL_SIZE = 64;
    const int RESULT_BUFFER_STRIDE = 4 * 4;
    const int POINTS_BUFFER_STRIDE = 2 * 4;
    const int NEAREST_SAMPLE_KERNEL = 0;
    const int BILINEAR_SAMPLE_KERNEL = 1;

    public struct PositionInfo {
        public readonly float Depth;
        public readonly Vector2 Velocity;
        public float GlobalGroundPos; // Writable because the returned local position is turned into a global one
    }

    enum SamplingMode {
        Nearest, Bilinear
    }

    [SerializeField]
    SamplingMode _samplingMode = SamplingMode.Bilinear;

    public Simulator Simulator { get; private set; }

    [SerializeField, HideInInspector]
    ComputeShader _sampleComputeShader;
    PackedComputeBuffer<Action<PositionInfo>, Vector2> _pointsBuffer;
    ComputeBuffer _resultBuffer;

    public event Action OnBeforeCallback;
    public event Action OnAfterCallback;

    public float Latency {get; private set;}
    SmoothedMeasurement _smoothedLatency = new SmoothedMeasurement(10);

    void Awake() {
        Simulator = GetComponent<Simulator>();

        _resultBuffer = new ComputeBuffer(KERNEL_SIZE, RESULT_BUFFER_STRIDE);
        _pointsBuffer = new PackedComputeBuffer<Action<PositionInfo>, Vector2>(KERNEL_SIZE, POINTS_BUFFER_STRIDE);
        _pointsBuffer.OnResize += (int size) => {
            if (_resultBuffer.IsValid()) {
                _resultBuffer.Dispose();
            }
            _resultBuffer = new ComputeBuffer(size, RESULT_BUFFER_STRIDE);
            BindBuffer(ShaderIDs.Positions, _pointsBuffer.Buffer);
            BindBuffer(ShaderIDs.SampledData, _resultBuffer);
        };

        BindBuffer(ShaderIDs.Positions, _pointsBuffer.Buffer);
        BindBuffer(ShaderIDs.SampledData, _resultBuffer);
        BindBuffer(ShaderIDs.Data, Simulator.GetSimulationData());
        BindTexture(ShaderIDs.GroundHeight, Simulator.GetGroundTexture());
        Simulator.SetShaderSimResolution(_sampleComputeShader);
        Simulator.SetShaderSimSize(_sampleComputeShader);
    }

    void FixedUpdate() { // TODO should I use FixedUpdate, Update or should both be possible?
        Sample();
    }

    void OnDestroy() {
        Release();
    }

    public void Subscribe(Action<PositionInfo> callback, Vector3 position) {
        _pointsBuffer.Add(callback, ConvertToBufferPositionValue(position));
    }

    public void UpdatePosition(Action<PositionInfo> callback, Vector3 position) {
        _pointsBuffer.SetValue(callback, ConvertToBufferPositionValue(position));
    }
    
    public void Unsubscribe(Action<PositionInfo> callback) {
        _pointsBuffer.Remove(callback);
    }

    public float GetSmoothedLatency() {
        if (_smoothedLatency.Ready())
            return _smoothedLatency.Value();
        else 
            return Latency;
    }
    public void Release() {
        _pointsBuffer.Release();
        _resultBuffer.Release();
    }

    public void Dispose() {
        Release();
    }

    Vector2 ConvertToBufferPositionValue(Vector3 position) {
        Vector3 simPos = Simulator.GlobalToSimulationSpace(position);
        return new Vector2(simPos.x, simPos.z);
    }

    void Sample() {
        if (_pointsBuffer.Count == 0) {
            return;
        }

        _pointsBuffer.UpdateBuffer();
        BindBuffer(ShaderIDs.Data, Simulator.GetSimulationData());

        float timeOfRequest = Time.time;
        Action<PositionInfo>[] currentMapping
            = _pointsBuffer.Select((e, i) => e.Item1).ToArray();

        _sampleComputeShader.Dispatch(GetActiveKernel(), (_pointsBuffer.Count + KERNEL_SIZE - 1) / KERNEL_SIZE, 1, 1);

        AsyncGPUReadback.Request(_resultBuffer, request => {
            if (Simulator == null) {
                return;
            }

            Latency = Time.time - timeOfRequest;
            _smoothedLatency.AddMeasurement(Latency);

            OnBeforeCallback?.Invoke();
            var positionInfos = request.GetData<PositionInfo>();
            for (int i = 0; i < currentMapping.Length; i++) {
                PositionInfo posInfo = positionInfos[i];
                // Assuming that rotations are not allowed around the x- and z-axis
                posInfo.GlobalGroundPos += Simulator.GetCenter().y - 0.5f * Simulator.GetSize().y;
                currentMapping[i].Invoke(posInfo);
            }
            OnAfterCallback?.Invoke();
        });
    }

    int GetActiveKernel() {
        return _samplingMode switch {
            SamplingMode.Nearest => NEAREST_SAMPLE_KERNEL,
            SamplingMode.Bilinear => BILINEAR_SAMPLE_KERNEL,
            _ => throw new ArgumentException("Unknown SamplingMode")
        };
    }

    void BindBuffer(int shaderID, ComputeBuffer buffer) {
        _sampleComputeShader.SetBuffer(NEAREST_SAMPLE_KERNEL, shaderID, buffer);
        _sampleComputeShader.SetBuffer(BILINEAR_SAMPLE_KERNEL, shaderID, buffer);
    }

    void BindTexture(int shaderID, Texture texture) {
        _sampleComputeShader.SetTexture(NEAREST_SAMPLE_KERNEL, ShaderIDs.GroundHeight, texture);
        _sampleComputeShader.SetTexture(BILINEAR_SAMPLE_KERNEL, ShaderIDs.GroundHeight, texture);
    }
}

} // namespace JonasWischeropp.Unity.WaterSimulation
