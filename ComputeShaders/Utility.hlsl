#ifndef WATER_SIMULATION_UTILITY_INCLUDED
#define WATER_SIMULATION_UTILITY_INCLUDED

float sq(float x) { return x * x; }

// b=bottom, t=top, l=left, r=right, weight(0,0)=bl, weight(1,1)=tr
float bilinearInterpolate(float bl, float br, float tl, float tr, float2 weights) {
    float2 xInterpolate = lerp(
        float2(tl, bl),
        float2(tr, br), weights.x);
    float yInterpolate = lerp(xInterpolate.y, xInterpolate.x, weights.y);
    return yInterpolate;
}

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
