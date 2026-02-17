#ifndef WATER_SIMULATOR_INFO_INCLUDED
#define WATER_SIMULATOR_INFO_INCLUDED

// JW_WaterSimulator to prevent naming conflicts
StructuredBuffer<float4> JW_WaterSimulator_Data;
float3 JW_WaterSimulator_Size;
float2 JW_WaterSimulator_StepSize;
float2 JW_WaterSimulator_StepSizeInv;
uint2  JW_WaterSimulator_Resolution;

void VertexInfo_float(float vertexID, out float depth, out float2 velocity, out float foam) {
#if defined(SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN)
    depth = 3.0;
    velocity = float2(1.0, 0.0);
    foam = 0.0;
#else
    float4 data = JW_WaterSimulator_Data[(int)vertexID];
    depth = data.x;
    velocity = data.yz;
    foam = data.w;
#endif
}

void SimulatorInfo_float(out float3 size, out float2 stepSize, out float2 stepSizeInv, out float2 resolution) {
#if defined(SHADERGRAPH_PREVIEW) || defined(SHADERGRAPH_PREVIEW_MAIN)
    size = float3(10.0, 10.0, 10.0);
    resolution = float2(64, 64);
    stepSize = size.xz / (resolution - 1);
    stepSizeInv = 1.0 / stepSize;
#else
    size = JW_WaterSimulator_Size;
    stepSize = JW_WaterSimulator_StepSize;
    stepSizeInv = JW_WaterSimulator_StepSizeInv;
    resolution = JW_WaterSimulator_Resolution;
#endif
}

#endif // WATER_SIMULATOR_INFO_INCLUDED
