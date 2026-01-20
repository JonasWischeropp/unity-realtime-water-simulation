#ifndef WATER_SIMULATION_UTILITY_INCLUDED
#define WATER_SIMULATION_UTILITY_INCLUDED

float sq(float x) { return x * x; }

#define DEFINE_TO_INDEX(Resolution) \
uint ToIndex(uint2 coord) { return coord.x + coord.y * Resolution.x; } \
uint ToIndex(uint x, uint y) { return x + y * Resolution.x; }

struct Vertex {
    float3 position;
    float3 normal;
    float2 uv;
};

bool IsDry(float depth) { return depth < 0.001; }

#endif // WATER_SIMULATION_UTILITY_INCLUDED
