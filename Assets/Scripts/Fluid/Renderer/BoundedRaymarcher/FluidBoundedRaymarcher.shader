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
                    float3 minAABB;
                    float3 maxAABB;
                CBUFFER_END

                StructuredBuffer<float3> positions;
                StructuredBuffer<float3> velocities;

                SamplerState sampler_bilinearClamp;

                Varyings vert (uint vertexID : SV_VertexID)
                {
                    Varyings o;

                    // 6-vertex full-screen quad using triangle list
                    float2 positions[6] = {
                        float2(-1, -1), // bottom left
                        float2( 1, -1), // bottom right
                        float2(-1,  1), // top left

                        float2(-1,  1), // top left
                        float2( 1, -1), // bottom right
                        float2( 1,  1)  // top right
                    };

                    float2 uvs[6] = {
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

                float3 ReconstructWorldPosition(float2 screenUV, float depth, float4x4 invViewProj)
                {
                    // Convert screenUV [0,1] to NDC [-1,1]
                    float2 ndc = screenUV * 2.0 - 1.0;
    
                    // Reconstruct clip space position
                    float4 clipPos = float4(ndc, depth, 1.0);

                    // Transform to world space
                    float4 worldPosH = mul(invViewProj, clipPos);
                    return worldPosH.xyz / worldPosH.w;
                }

                void ReconstructRay(
                    float2 screenUV,
                    float depth,
                    float4x4 invViewProj,
                    out float3 rayOriginWS,
                    out float3 rayEndWS)
                {
                    // T0: Near plane (z = 0 in clip space)
                    rayOriginWS = ReconstructWorldPosition(screenUV, 0.0, invViewProj);

                    // T1: World position from depth buffer (z = depth in clip space)
                    rayEndWS = ReconstructWorldPosition(screenUV, depth, invViewProj);
                }

                float4 frag (Varyings i) : SV_Target
                {
                    float depth = SampleSceneDepth(i.screenUV, sampler_bilinearClamp);
                    return float4(depth, 0, 0, 1);

                    // if (depth >= i.positionCS.z) discard;
                    
                    // float3 fragWorldPos = i.positionWS;
                    // float3 depthWorldPos = WorldPosFromDepth(i.screenUV, depth);

                    // float worldDist = distance(depthWorldPos, fragWorldPos);

                    // float3 lightDir = normalize(_MainLightPosition.xyz);
                    // float diff = max(dot(i.normalWS, lightDir), 0.2);
                    // float3 lighting = _MainLightColor.rgb * diff;

                    // return float4(lighting * saturate(worldDist), 1);
                    // return float4(screenUV, 0, 1);
                }
            
                ENDHLSL
            }
        }
    }
