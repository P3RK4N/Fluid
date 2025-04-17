    Shader "Unlit/FluidBoundedRaymarcherShader"
    {
        Properties
        {
            _Color("Color", Color) = (1, 1, 1, 1)
        }
        SubShader
        {
            Tags { "RenderType"="Opaque" }
            Pass
            {
                ZTest Always
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
                #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
                
                #define COMPUTE_GRID_SHADER
                #include "../../../Grid/Compute/ComputeGrid.hlsl"
                #include "../../../Fluid/Compute/FluidMaths3D.hlsl"

                struct Attributes
                {
                    float4 positionOS : POSITION;
                    float3 normalOS : NORMAL;
                };
            
                struct Varyings
                {
                    float4 positionCS : SV_POSITION;
                    float2 screenUV : TEXCOORD0;
                };
            
                CBUFFER_START(UnityPerMaterial)
                    float4x4 invVP;
                    float step;
                    float iso;
                    float densityCoeff;
                    float surfaceThreshold;
                CBUFFER_END

                struct RaymarcherRange
                {
                    float3 origin;
                    float min;
                    float3 ray;
                    float max;
                };

                StructuredBuffer<float3> positions;
                StructuredBuffer<float2> densities;
                StructuredBuffer<int3> bounds;

                SamplerState sampler_bilinearClamp;

                Varyings vert (uint vertexID : SV_VertexID)
                {
                    Varyings o;

                    // 6-vertex full-screen quad using triangle list
                    const float2 positions[6] = {
                        float2(-1, -1), // bottom left
                        float2( 1, -1), // bottom right
                        float2(-1,  1), // top left

                        float2(-1,  1), // top left
                        float2( 1, -1), // bottom right
                        float2( 1,  1)  // top right
                    };

                    const float2 uvs[6] = {
                        float2(0, 1),
                        float2(1, 1),
                        float2(0, 0),

                        float2(0, 0),
                        float2(1, 1),
                        float2(1, 0)
                    };

                    o.positionCS = float4(positions[vertexID], 0, 1);
                    o.screenUV = uvs[vertexID];
                    return o;
                }
            
                float3 WorldPosFromDepth(float2 screenUV, float depth)
                {
                    #ifdef UNITY_REVERSED_Z
                        depth = 1 - depth;
                    #endif

                    float4 projPos = float4(screenUV * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
                    float4 worldPos = mul(invVP, projPos);
                    worldPos /= worldPos.w;

                    return worldPos.xyz;
                }

                // Returns true if the ray hits the box and sets outMinT and outMaxT accordingly
                bool RayBoxIntersection(float3 rayOrigin, float3 rayDir, float3 boxMin, float3 boxMax, out float outMinT, out float outMaxT)
                {
                    float3 invDir = 1.0 / rayDir;

                    float3 t0s = (boxMin - rayOrigin) * invDir;
                    float3 t1s = (boxMax - rayOrigin) * invDir;

                    float3 tsmaller = min(t0s, t1s);
                    float3 tbigger  = max(t0s, t1s);

                    outMinT = max(max(tsmaller.x, tsmaller.y), tsmaller.z);
                    outMaxT = min(min(tbigger.x, tbigger.y), tbigger.z);

                    return outMaxT >= max(outMinT, 0.0); // Only allow forward ray
                }

                void GetBounds(out float3 minn, out float3 maxx)
                {
                    minn = (float3)bounds[0] / 1000.0f;
                    maxx = (float3)bounds[1] / 1000.0f;
                }

                float dot2(float3 p)
                {
                    return dot(p, p);
                }

                float CalculateDensity(float3 pos)
                {
                    float density = 0;
                    float _bucketRadius2 = _bucketRadius * _bucketRadius;

                    FOREACH_ADJACENT_VALUE_BEGIN(pos, i)

                        float sqrDist = dot2(positions[i] - pos);
        
                        if (sqrDist >= _bucketRadius2)
                            continue;
    
                        float dst = sqrt(sqrDist);
                        density += FluidMaths.DensityKernel(dst, _bucketRadius);

                    FOREACH_ADJACENT_VALUE_END()
    
                    return density;
                }

                float3 CalculateNormal(float3 pos)
                {
                    float3 densityNormal = 0;
                    float _bucketRadius2 = _bucketRadius * _bucketRadius;
	
                    FOREACH_ADJACENT_VALUE_BEGIN(pos, i)

                        float3 offsetToNeighbour = positions[i] - pos;
                        float sqrDist = dot2(offsetToNeighbour);
        
                        if (sqrDist >= _bucketRadius2)
                            continue;
        
                        float dst = sqrt(sqrDist);
                        float3 dirToNeighbour = dst > 0 ? offsetToNeighbour / dst : float3(0, 1, 0);

                        float neighbourDensity = densities[i][0];
                        densityNormal += dirToNeighbour * FluidMaths.DensityDerivative(dst, _bucketRadius) * neighbourDensity;

                    FOREACH_ADJACENT_VALUE_END()

                    return normalize(densityNormal);
                }

                bool GetRaymarcherRange(float2 screenUV, float margin, out RaymarcherRange raymarcherRange)
                {
                    float depth = SampleSceneDepth(screenUV, sampler_bilinearClamp);

                    #if UNITY_REVERSED_Z
                        depth = 1 - depth;
                    #endif

                    float4 nearCS = float4(screenUV * 2.0f - 1.0f, -1.0f, 1.0f);
                    float4 depthCS = float4(screenUV * 2.0f - 1.0f, depth * 2.0f - 1.0f, 1.0f);

                    float4 worldNear = mul(invVP, nearCS);
                    float4 worldDepth = mul(invVP, depthCS);

                    worldNear /= worldNear.w;
                    worldDepth /= worldDepth.w;

                    float3 cameraRay = worldDepth.xyz - worldNear.xyz;
                    float tdepth = length(cameraRay);
                    cameraRay = normalize(cameraRay);
                    float tmin, tmax;
                    float3 minn, maxx;
                    GetBounds(minn, maxx);

                    if (RayBoxIntersection(worldNear.xyz, cameraRay, minn - margin, maxx + margin, tmin, tmax)) // Hit bounds
                    {
                        tmin = max(tmin, 0); // Starting from near plane
                        tmax = min(tmax, tdepth); // Ensure end before depth

                        raymarcherRange.min = tmin;
                        raymarcherRange.max = tmax;
                        raymarcherRange.ray = cameraRay;
                        raymarcherRange.origin = worldNear.xyz;

                        return tmin < tmax;
                    }

                    return false;
                }

                float4 frag (Varyings i) : SV_Target
                {
                    RaymarcherRange rr;

                    if (!GetRaymarcherRange(i.screenUV, _bucketRadius, rr))
                        discard;

                    const int maxSteps = 512;
                    float t = rr.min;
                    float densityAccum = 0.0;
                    float3 color = float3(0, 0, 0);

                    float3 lightDir = GetMainLight().direction;
                    float3 vibrantWaterColor = float3(0.3, 0.6, 0.9); // Ghibli water tone
                    float3 foamColor = float3(0.9, 0.9, 0.95);

                    bool entered = false;
                    float3 entryNormal = float3(0, 1, 0);
                    float entryDepth = 0.0;
                    float3 entryPoint = float3(0, 0, 0);

                    [loop]
                    for (int i = 0; i < maxSteps && t < rr.max && densityAccum < 1.0; ++i)
                    {
                        float3 p = rr.origin + t * rr.ray;
                        float density = CalculateDensity(p);

                        if (density > surfaceThreshold)
                        {
                            if (!entered)
                            {
                                entered = true;
                                entryNormal = normalize(CalculateNormal(p));
                                entryDepth = t;
                                entryPoint = p;
                            }

                            float opacity = saturate(density * densityCoeff);
                            densityAccum += opacity * 0.05;
                        }

                        t += step;
                    }

                    // Stylized shading only happens if we entered the fluid
                    if (entered)
                    {
                        // Simple banded lighting (toon)
                        float diffuse = saturate(dot(entryNormal, -lightDir));
                        float3 bandedLighting = (
                            diffuse < 0.3 ? 0.2 :
                            diffuse < 0.6 ? 0.5 :
                            1.0
                        ) * vibrantWaterColor;

                        // Vertical fade effect (lighter near top)
                        float heightFade = saturate((entryPoint.y + 1.0) * 0.5); // assumes water ~ y=0
                        bandedLighting *= lerp(0.6, 1.2, heightFade);

                        // Add foam if shallow
                        float foamAmount = saturate(1.0 - entryDepth * 2.0) * 0.8; // shallow water more foam
                        float3 finalColor = lerp(bandedLighting, foamColor, foamAmount);

                        // Blend by accumulated density (transparency)
                        color = finalColor * saturate(densityAccum);
                    }



                    return float4(color, saturate(densityAccum));
                }
            
                ENDHLSL
            }
        }
    }
