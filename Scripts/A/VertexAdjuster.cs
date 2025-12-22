using System;
using UnityEngine;

public class VertexAdjuster {
    // ComputeShader _computeShader; // TODO how to set
    // Mesh _mesh;
    // Texture _groundDepthTexture;
    // Func<ComputeBuffer> _bufferProvider;

    // public void Dispatch() {
    //     // _meshFilter.mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
    //     // TODO should the target be set here or outside?
    //     GraphicsBuffer vertexBuffer = _mesh.GetVertexBuffer(0);
    //     _computeShader.SetBuffer(0, "VertexBuffer", vertexBuffer);
    //     _computeShader.SetBuffer(1, "VertexBuffer", vertexBuffer);
    //     vertexBuffer.Dispose();

    //     _computeShader.SetTexture(0, "GroundHeight", _groundDepthTexture);
    //     _computeShader.SetTexture(1, "GroundHeight", _groundDepthTexture);
    //     _computeShader.SetBuffer(0, "Data", _waterSimulator.GetSimulationData());
    //     _computeShader.SetBuffer(1, "Data", _waterSimulator.GetSimulationData());
    //     _computeShader.SetVector("Size", _size);
    //     _computeShader.SetInts("Resolution", new int[] { _resolution.x, _resolution.y });
    //     _computeShader.SetFloats("StepSize", new float[] { _size.x / (_resolution.x - 1), _size.z / (_resolution.y - 1) });
    //     _computeShader.SetFloats("StepSizeInv", new float[] { (_resolution.x - 1) / _size.x, (_resolution.y - 1) / _size.z });

    //     _computeShader.Dispatch(0, _resolution.x / KERNEL_SIZE, _resolution.y / KERNEL_SIZE, 1);

    //     _computeShader.Dispatch(1, _resolution.x / KERNEL_SIZE, _resolution.y / KERNEL_SIZE, 1);
    // }
}
