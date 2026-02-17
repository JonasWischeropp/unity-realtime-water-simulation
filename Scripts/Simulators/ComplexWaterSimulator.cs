using UnityEngine;
using UnityEngine.Assertions;

namespace JonasWischeropp.Unity.WaterSimulation {

public class ComplexWaterSimulator : IWaterSimulator {
    // TODO refactor duplicated code (SimpleWaterSimulator)
    Simulator _simulator;
    ComputeShader _simulationComputeShader;

    ComputeBuffer _simulationData0;
    ComputeBuffer _simulationData1;
    bool _simulationData0IsLatest = true;

    ComputeBuffer _cellDataBuffer;

    Vector3Int _dispatchGroupSize;

    public void Init(Simulator simulator, ComputeShader simulationComputeShader, Vector2Int resolution, Texture groundDepthTexture, ComputeBuffer manipulationBuffer) {
        _simulator = simulator;
        _simulationComputeShader = simulationComputeShader;

        int bufferSize = resolution.x * resolution.y;
        _simulationData0 = new ComputeBuffer(bufferSize, 4 * 4);
        _simulationData1 = new ComputeBuffer(bufferSize, 4 * 4);
        _simulationData0IsLatest = true;

        _cellDataBuffer = new ComputeBuffer(bufferSize, 5 * 4 * 4);

        for (int kernel = 0; kernel < 3; kernel++) {
            _simulationComputeShader.SetBuffer(kernel, ShaderIDs.Manipulation, manipulationBuffer);
            _simulationComputeShader.SetTexture(kernel, ShaderIDs.GroundHeight, groundDepthTexture);
            _simulationComputeShader.SetBuffer(kernel, ShaderIDs.Cells, _cellDataBuffer);

            _simulationComputeShader.SetBuffer(kernel, ShaderIDs.Target, _simulationData0);
        }

        _simulator.SetShaderSimSize(_simulationComputeShader);
        _simulator.SetShaderSimStepSize(_simulationComputeShader);
        _simulator.SetShaderSimStepSizeInv(_simulationComputeShader);
        _simulator.SetShaderSimResolution(_simulationComputeShader);

        SetGravity(9.81f);
        _simulationComputeShader.SetFloat(ShaderIDs.ManningRoughness, 0.13f);
        _simulationComputeShader.SetFloat(ShaderIDs.MaxVelocity, 10f);

        // TODO make parameters
        _simulationComputeShader.SetFloat(ShaderIDs.FoamDissipation, 0.1f);
        _simulationComputeShader.SetFloat(ShaderIDs.FoamAirTrapMul, 0.03f);
        _simulationComputeShader.SetFloat(ShaderIDs.FoamSteepMul, 0.03f);
        _simulationComputeShader.SetFloat(ShaderIDs.FoamVanishing, 1f);

        Assert.IsTrue(resolution.x % Simulator.KERNEL_SIZE == 0);
        Assert.IsTrue(resolution.y % Simulator.KERNEL_SIZE == 0);
        _dispatchGroupSize = new Vector3Int(resolution.x / Simulator.KERNEL_SIZE, resolution.y / Simulator.KERNEL_SIZE, 1);

        // Init buffer
        _simulationComputeShader.Dispatch(2, _dispatchGroupSize.x, _dispatchGroupSize.y, _dispatchGroupSize.z);
    }

    public void Dispatch(float deltaTime) {
        _simulationComputeShader.SetFloat(ShaderIDs.DeltaTime, deltaTime);
        for (int kernel = 0; kernel < 2; kernel++) {
            _simulationComputeShader.SetBuffer(kernel, ShaderIDs.Source, _simulationData0IsLatest ? _simulationData0 : _simulationData1);
            _simulationComputeShader.SetBuffer(kernel, ShaderIDs.Target, _simulationData0IsLatest ? _simulationData1 : _simulationData0);
        }

        _simulationComputeShader.Dispatch(1, _dispatchGroupSize.x, _dispatchGroupSize.y, _dispatchGroupSize.z);
        _simulationComputeShader.Dispatch(0, _dispatchGroupSize.x, _dispatchGroupSize.y, _dispatchGroupSize.z);
        _simulationData0IsLatest = ! _simulationData0IsLatest;
    }

    public void Release() {
        _simulationData0.Release();
        _simulationData1.Release();
        _cellDataBuffer.Release();
    }

    ~ComplexWaterSimulator() => Release();

    public ComputeBuffer GetSimulationData() {
        return _simulationData0IsLatest ? _simulationData0 : _simulationData1;
    }

    public void SetGravity(float gravity) {
        _simulationComputeShader.SetFloat(ShaderIDs.Gravity, gravity);
    }
}

} // namespace JonasWischeropp.Unity.WaterSimulation
