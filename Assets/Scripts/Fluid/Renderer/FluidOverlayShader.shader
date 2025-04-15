    Shader "Unlit/FluidOverlayShader"
    {
        Properties
        {
            _Color("Color", Color) = (1, 1, 1, 1)
            _MainTex ("Texture", 2D) = "white" {}
            _VelocityCoeff("VelocityCoeff", float) = 1.0
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
                    float3 normalWS : TEXCOORD0;
                    float2 uv : TEXCOORD1;
                    float3 positionWS : TEXCOORD2;
                };
            
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);

                CBUFFER_START(UnityPerMaterial)
                    float4 _Color;
                    float _VelocityCoeff;
                    float _ParticleRadius;
                    float4x4 invVP;
                    float magicFactor;
                CBUFFER_END

                StructuredBuffer<float3> positions;
                StructuredBuffer<float3> velocities;

                Varyings vert (Attributes v, uint instanceID : SV_InstanceID)
                {
                    Varyings o;
                    
                    float4 positionWS = v.positionOS * _ParticleRadius + float4(positions[instanceID], 1.0f);
                    o.positionWS = positionWS.xyz;
                    o.positionCS = TransformWorldToHClip(positionWS);
                    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                    o.uv = float2(1.0f - length(velocities[instanceID]) * _VelocityCoeff, 0.0f);
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

                SamplerState sampler_bilinearClamp;

                float4 frag (Varyings i) : SV_Target
                {
                    float2 screenUV = i.positionCS.xy / _ScreenParams.xy;
                    float depth = SampleSceneDepth(screenUV * magicFactor, sampler_bilinearClamp); 
                    

                    if (depth >= i.positionCS.z) discard;

                    float3 worldPos = WorldPosFromDepth(screenUV, depth);
                    float worldDist = distance(worldPos, i.positionWS);

                    float3 lightDir = normalize(_MainLightPosition.xyz);
                    float diff = max(dot(i.normalWS, lightDir), 0.2);
                    float3 lighting = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _MainLightColor.rgb * diff;

                    if (worldDist < 1) return float4(1,0,0,1);
                    else return float4(0,1,0,1);
                    return float4(lighting, saturate(distance(worldPos, i.positionWS)));
                }
            
                ENDHLSL
            }
        }
    }
