    Shader "Unlit/SdfValueShader"
    {
        Properties
        {
            _MainTex ("Field", 3D) = "black" {}
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
            
                TEXTURE3D(_MainTex);
                SAMPLER(sampler_MainTex);

                CBUFFER_START(UnityPerMaterial)
                    int _Resolution;
                    float3 _MinBound;
                    float3 _MaxBound;
                CBUFFER_END

                float3 getOffset(int x, int y, int z)
                {
                    float size = (_MaxBound.x - _MinBound.x) / _Resolution;
                    float halfSize = size / 2.0f;
                    return lerp(_MinBound, _MaxBound, float3(x, y, z) / _Resolution) + halfSize;
                }

                float3 getUVW(int x, int y, int z)
                {
                    return (float3(x, y, z) + 0.5f) / _Resolution;
                }

                Varyings vert (Attributes v, uint instanceID : SV_InstanceID)
                {
                    Varyings o;

                    float x = (float) (instanceID / (_Resolution * _Resolution));
                    float y = (float) ((instanceID / _Resolution) % _Resolution);
                    float z = (float) (instanceID % _Resolution);
                    
                    float val = SAMPLE_TEXTURE3D_LOD(_MainTex, sampler_MainTex, getUVW(x, y, z), 0).r;
                    float size = (_MaxBound.x - _MinBound.x) / _Resolution;

                    float inside = val <= 0.0f ? 1.0f : 0.0f;

                    o.positionCS = TransformWorldToHClip(v.positionOS * size * 0.95f * inside + getOffset(x, y, z));
                    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                    return o;
                }
            
                float4 frag (Varyings i) : SV_Target
                {
                    float3 lightDir = normalize(_MainLightPosition.xyz);
                    float diff = max(dot(i.normalWS, lightDir), 0.2);
                    float3 lighting = _MainLightColor.rgb * diff;
                    return float4(lighting, 1);
                }
            
                ENDHLSL
            }
        }
    }
