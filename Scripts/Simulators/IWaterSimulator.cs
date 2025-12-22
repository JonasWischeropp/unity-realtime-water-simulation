// TODO refactor (only use one)
using UnityEngine;

public interface IWaterSimulator {
    public abstract void Init(ComputeShader simulationComputeShader, Vector3 size, Vector2Int resolution, Texture groundDepthTexture, ComputeBuffer manipulationBuffer);

    public abstract void Dispatch(float deltaTime);

    public abstract void Release();

    public abstract ComputeBuffer GetSimulationData();
    
    public abstract void SetGravity(float gravity);
}
