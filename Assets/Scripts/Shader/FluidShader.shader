    Shader "Unlit/FluidShader"
    {
        Properties
        {
            _Color("Color", Color) = (1, 1, 1, 1)
            _MainTex ("Texture", 2D) = "white" {}
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
                };
            
                CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float _ParticleRadius;
                float3 _WorldPos;
                CBUFFER_END
            
                StructuredBuffer<float3> positions;

                Varyings vert (Attributes v, uint instanceID : SV_InstanceID)
                {
                    Varyings o;
                    
                    float4 positionWS = v.positionOS * _ParticleRadius + float4(positions[instanceID] + _WorldPos, 0.0f);
                    o.positionCS = TransformObjectToHClip(positionWS);
                    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                    return o;
                }
            
                float4 frag (Varyings i) : SV_Target
                {
                    float3 lightDir = normalize(_MainLightPosition.xyz);
                    float diff = max(dot(i.normalWS, lightDir), 0);
                    float3 lighting = _Color.rgb * _MainLightColor.rgb * diff;
                    return float4(lighting, 1.0);
                }
            
                ENDHLSL
            }
        }
    }
