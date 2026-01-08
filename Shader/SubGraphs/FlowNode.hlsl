// TODO clean up
#ifndef FLOW_NODE_INCLUDED
#define FLOW_NODE_INCLUDED

float3 FlowUVW (
	float2 uv, float2 flowVector, float2 jump,
	float tiling, float time, bool flowB
) {
	float phaseOffset = flowB ? 0.5 : 0;
	float progress = frac(time + phaseOffset);
	float3 uvw;
	uvw.xy = uv - flowVector * progress;
	uvw.xy *= tiling;
	uvw.xy += phaseOffset;
	uvw.xy += (time - progress) * jump;
	uvw.z = 1 - abs(1 - 2 * progress);
	return uvw;
}

float3 UnpackDerivativeHeight (float4 textureData) {
    float3 dh = textureData.agb;
    dh.xy = dh.xy * 2 - 1;
    return dh;
}

void Flow_float(float time, float2 uv, float tiling, float2 velocity,
    out float2 uv1, out float weight1, out float2 uv2, out float weight2) {
    float3 flow = float3(velocity, length(velocity));
    float2 jump = float2(6.0/25.0, 5.0/24.0);

    float3 uvwA = FlowUVW(
        uv, flow.xy, jump,
        tiling, time, false
    );
    float3 uvwB = FlowUVW(
        uv, flow.xy, jump,
        tiling, time, true
    );

    uv1 = uvwA.xy;
    weight1 = uvwA.z;
    uv2 = uvwB.xy;
    weight2 = uvwB.z;

    // float finalHeightScale =
    //     flow.z * _HeightScaleModulated + _HeightScale;
    //
    // float3 dhA =
    //     UnpackDerivativeHeight(tex2D(_DerivHeightMap, uvwA.xy)) *
    //     (uvwA.z * finalHeightScale);
    // float3 dhB =
    //     UnpackDerivativeHeight(tex2D(_DerivHeightMap, uvwB.xy)) *
    //     (uvwB.z * finalHeightScale);
    // o.Normal = normalize(float3(-(dhA.xy + dhB.xy), 1));

    // fixed4 texA = tex2D(_MainTex, uvwA.xy) * uvwA.z;
    // fixed4 texB = tex2D(_MainTex, uvwB.xy) * uvwB.z;
}

#endif // FLOW_NODE_INCLUDED
