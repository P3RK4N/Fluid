    Shader "Unlit/DepthDebugShader"
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
                ZWrite Off
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 
                #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
                
                struct Varyings
                {
                    float4 positionCS : SV_POSITION;
                    float2 uv : TEXCOORD1;
                };
            
                CBUFFER_START(UnityPerMaterial)
                    float4x4 invVP;
                CBUFFER_END
                
                Varyings vert (uint id : SV_VertexID)
                {
                    Varyings o;

                    // Fullscreen quad made of two triangles:
                    // Vertex ID order: 0 1 2, 2 1 3
                    float2 positions[6] = {
                        float2(-1.0, -1.0), // bottom left
                        float2( 1.0, -1.0), // bottom right
                        float2(-1.0,  1.0), // top left

                        float2(-1.0,  1.0), // top left
                        float2( 1.0, -1.0), // bottom right
                        float2( 1.0,  1.0)  // top right
                    };

                    float2 uvs[6] = {
                        float2(0.0, 1.0),
                        float2(1.0, 1.0),
                        float2(0.0, 0.0),

                        float2(0.0, 0.0),
                        float2(1.0, 1.0),
                        float2(1.0, 0.0)
                    };

                    o.positionCS = float4(positions[id], 0.0, 1.0);
                    o.uv = uvs[id];
                    return o;
                }
            
                float4 frag (Varyings i) : SV_Target
                {
                    float depth = SampleSceneDepth(i.uv, sampler_PointClamp);
                    float eye = LinearEyeDepth(depth, _ZBufferParams);

                    #ifdef UNITY_REVERSED_Z
                        depth = 1 - depth;
                    #endif


                    // return float4(i.uv * 5, eye * 0.01f, 1.0f);

                    float4 projPos = float4(i.uv * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
                    float4 worldPos = mul(invVP, projPos); 
                    worldPos /= worldPos.w;

                    if (distance(projPos, worldPos) < 0.0001f) return (float4)0;
                    return worldPos;
                }
            
                ENDHLSL
            }
        }
    }
