using UnityEngine;
using UnityEngine.Assertions;

public class ComplexWaterSimulator : IWaterSimulator {
    // TODO refactor duplicated code (SimpleWaterSimulator)
    ComputeShader _simulationComputeShader;

    ComputeBuffer _simulationData0;
    ComputeBuffer _simulationData1;
    bool _simulationData0IsLatest = true;

    ComputeBuffer _cellDataBuffer;

    Vector3Int _dispatchGroupSize;

    public void Init(ComputeShader simulationComputeShader, Vector3 size, Vector2Int resolution, Texture groundDepthTexture, ComputeBuffer manipulationBuffer) {
        _simulationComputeShader = simulationComputeShader; // TODO get reference without passing it as an argument
        int bufferSize = resolution.x * resolution.y;
        _simulationData0 = new ComputeBuffer(bufferSize, 4 * 4);
        _simulationData1 = new ComputeBuffer(bufferSize, 4 * 4);
        _simulationData0IsLatest = true;

        _cellDataBuffer = new ComputeBuffer(bufferSize, 5 * 4 * 4);

        for (int kernel = 0; kernel < 3; kernel++) {
            _simulationComputeShader.SetBuffer(kernel, "Manipulation", manipulationBuffer);
            _simulationComputeShader.SetTexture(kernel, "GroundHeight", groundDepthTexture);
            _simulationComputeShader.SetBuffer(kernel, "Cells", _cellDataBuffer);

            _simulationComputeShader.SetBuffer(kernel, "Target", _simulationData0);
        }

        _simulationComputeShader.SetVector("Size", size);
        _simulationComputeShader.SetFloats("StepSize", new float[] { size.x / (resolution.x - 1), size.z / (resolution.y - 1) });
        // _simulationComputeShader.SetFloats("StepSizeInv", new float[] { (resolution.x - 1) / size.x, (resolution.y - 1) / size.z });
        _simulationComputeShader.SetInts("Resolution", new int[] { resolution.x, resolution.y });

        // TODO parameter
        _simulationComputeShader.SetFloat("Gravity", 9.81f);
        _simulationComputeShader.SetFloat("ManningRoughness", 0.13f);
        _simulationComputeShader.SetFloat("MaxVelocity", 100000f);

        // TODO make parameters
        _simulationComputeShader.SetFloat("FoamDissipation", 0.1f);
        _simulationComputeShader.SetFloat("FoamAirTrapMul", 0.03f);
        _simulationComputeShader.SetFloat("FoamSteepMul", 0.03f);
        _simulationComputeShader.SetFloat("FoamVanishing", 1f);

        Assert.IsTrue(resolution.x % WaterSimulator.KERNEL_SIZE == 0);
        Assert.IsTrue(resolution.y % WaterSimulator.KERNEL_SIZE == 0);
        _dispatchGroupSize = new Vector3Int(resolution.x / WaterSimulator.KERNEL_SIZE, resolution.y / WaterSimulator.KERNEL_SIZE, 1);

        // Init buffer
        _simulationComputeShader.Dispatch(2, _dispatchGroupSize.x, _dispatchGroupSize.y, _dispatchGroupSize.z);
    }

    public void Dispatch(float deltaTime) {
        _simulationComputeShader.SetFloat("DeltaTime", deltaTime);
        for (int kernel = 0; kernel < 2; kernel++) {
            _simulationComputeShader.SetBuffer(kernel, "Source", _simulationData0IsLatest ? _simulationData0 : _simulationData1);
            _simulationComputeShader.SetBuffer(kernel, "Target", _simulationData0IsLatest ? _simulationData1 : _simulationData0);
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
        _simulationComputeShader.SetFloat("Gravity", 9.81f);
    }
}
