    Shader "Unlit/FluidShader"
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
                HLSLPROGRAM
                #pragma vertex vert
                #pragma fragment frag
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 

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
                };
            
                TEXTURE2D(_MainTex);
                SAMPLER(sampler_MainTex);

                CBUFFER_START(UnityPerMaterial)
                    float4 _Color;
                    float _VelocityCoeff;
                    float _ParticleRadius;
                CBUFFER_END

                StructuredBuffer<float3> positions;
                StructuredBuffer<float3> velocities;

                Varyings vert (Attributes v, uint instanceID : SV_InstanceID)
                {
                    Varyings o;
                    
                    float4 positionWS = v.positionOS * _ParticleRadius + float4(positions[instanceID], 1.0f);
                    o.positionCS = TransformWorldToHClip(positionWS);
                    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                    o.uv = float2(1.0f - length(velocities[instanceID]) * _VelocityCoeff, 0.0f);
                    return o;
                }
            
                float4 frag (Varyings i) : SV_Target
                {
                    float3 lightDir = normalize(_MainLightPosition.xyz);
                    float diff = max(dot(i.normalWS, lightDir), 0.2);
                    float3 lighting = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv) * _MainLightColor.rgb * diff;
                    return float4(lighting, 1.0);
                }
            
                ENDHLSL
            }
        }
    }
