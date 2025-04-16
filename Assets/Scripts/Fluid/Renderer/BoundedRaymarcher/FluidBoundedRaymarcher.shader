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
                CBUFFER_END

                struct RaymarcherRange
                {
                    float3 origin;
                    float min;
                    float3 ray;
                    float max;
                };

                StructuredBuffer<float3> positions;
                StructuredBuffer<float3> velocities;
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

                bool GetRaymarcherRange(float2 screenUV, out RaymarcherRange raymarcherRange)
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

                    if (RayBoxIntersection(worldNear.xyz, cameraRay, minn, maxx, tmin, tmax)) // Hit bounds
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

                    if (!GetRaymarcherRange(i.screenUV, rr))
                        discard;

                    return float4(saturate(rr.max - rr.min), 0, 0, 1);
                }
            
                ENDHLSL
            }
        }
    }
