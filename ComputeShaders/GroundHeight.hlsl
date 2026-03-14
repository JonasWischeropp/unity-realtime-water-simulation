#ifndef WATER_SIMULATION_GROUND_HEIGHT_INCLUDED
#define WATER_SIMULATION_GROUND_HEIGHT_INCLUDED

#include "HLSLSupport.cginc" // Required for UNITY_REVERSED_Z

Texture2D<float> GroundHeight;

float GetGroundHeight(uint x, uint y, float3 size) {
#if UNITY_REVERSED_Z
    return GroundHeight[uint2(x,y)] *  size.y;
#else
    return (1.0 - GroundHeight[uint2(x,y)]) *  size.y;
#endif
}

#endif // WATER_SIMULATION_GROUND_HEIGHT_INCLUDED
