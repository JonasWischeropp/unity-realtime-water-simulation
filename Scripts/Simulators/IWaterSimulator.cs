// TODO refactor (only use one)
using UnityEngine;

namespace JonasWischeropp.Unity.WaterSimulation {

public interface IWaterSimulator {
    public abstract void Init(Simulator simulator, ComputeShader simulationComputeShader, Vector2Int resolution, Texture groundDepthTexture, ComputeBuffer manipulationBuffer);

    public abstract void Dispatch(float deltaTime);

    public abstract void Release();

    public abstract ComputeBuffer GetSimulationData();
    
    public abstract void SetGravity(float gravity);
}

} // namespace JonasWischeropp.Unity.WaterSimulation
