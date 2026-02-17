using UnityEngine;
using UnityEngine.Assertions;

namespace JonasWischeropp.Unity.WaterSimulation {

public class SimpleWaterSimulator : IWaterSimulator {
    Simulator _simulator;
    ComputeShader _simulationComputeShader;

    ComputeBuffer _simulationData0;
    ComputeBuffer _simulationData1;
    bool _simulationData0IsLatest = true;

    Vector3Int _dispatchGroupSize;

    public void Init(Simulator simulator, ComputeShader simulationComputeShader, Vector2Int resolution, Texture groundDepthTexture, ComputeBuffer manipulationBuffer) {
        _simulator = simulator;
        _simulationComputeShader = simulationComputeShader;

        int bufferSize = resolution.x * resolution.y;
        _simulationData0 = new ComputeBuffer(bufferSize, 4 * 4);
        _simulationData1 = new ComputeBuffer(bufferSize, 4 * 4);
        _simulationData0IsLatest = true;

        for (int kernel = 0; kernel < 2; kernel++) {
            // Source and Target for initialization
            _simulationComputeShader.SetBuffer(kernel, ShaderIDs.Source, _simulationData0);
            _simulationComputeShader.SetBuffer(kernel, ShaderIDs.Target, _simulationData0);
            _simulationComputeShader.SetBuffer(kernel, ShaderIDs.Manipulation, manipulationBuffer);
            _simulationComputeShader.SetTexture(kernel, ShaderIDs.GroundHeight, groundDepthTexture);
        }
        _simulator.SetShaderSimSize(_simulationComputeShader);
        _simulator.SetShaderSimStepSize(_simulationComputeShader);
        _simulator.SetShaderSimStepSizeInv(_simulationComputeShader);
        _simulator.SetShaderSimResolution(_simulationComputeShader);

        SetGravity(9.81f);

        Assert.IsTrue(resolution.x % Simulator.KERNEL_SIZE == 0);
        Assert.IsTrue(resolution.y % Simulator.KERNEL_SIZE == 0);
        _dispatchGroupSize = new Vector3Int(resolution.x / Simulator.KERNEL_SIZE, resolution.y / Simulator.KERNEL_SIZE, 1);

        // Init kernel
        _simulationComputeShader.Dispatch(1, _dispatchGroupSize.x, _dispatchGroupSize.y, _dispatchGroupSize.z);
    }
    
    public void Dispatch(float deltaTime) {
        _simulationComputeShader.SetFloat(ShaderIDs.DeltaTime, deltaTime);
        _simulationComputeShader.SetBuffer(0, ShaderIDs.Source, _simulationData0IsLatest ? _simulationData0 : _simulationData1);
        _simulationComputeShader.SetBuffer(0, ShaderIDs.Target, _simulationData0IsLatest ? _simulationData1 : _simulationData0);
        _simulationComputeShader.Dispatch(0, _dispatchGroupSize.x, _dispatchGroupSize.y, _dispatchGroupSize.z);
        _simulationData0IsLatest = ! _simulationData0IsLatest;
    }

    public void Release() {
        _simulationData0.Release();
        _simulationData1.Release();
    }

    ~SimpleWaterSimulator() => Release();

    public ComputeBuffer GetSimulationData() {
        return _simulationData0IsLatest ? _simulationData0 : _simulationData1;
    }

    public void SetGravity(float gravity) {
        _simulationComputeShader.SetFloat(ShaderIDs.Gravity, gravity);
    }
}

} // namespace JonasWischeropp.Unity.WaterSimulation
