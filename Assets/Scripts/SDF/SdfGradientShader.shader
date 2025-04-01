    Shader "Unlit/SdfGradientShader"
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
                #pragma geometry geom
                #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 

                struct v2g
                {
                    float3 worldPos : TEXCOORD0;
                    float3 gradient : NORMAL;
                    float value : TEXCOORD1;
                };

                struct g2f
                {
                    float4 pos : SV_POSITION;
                    float3 color : COLOR;
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

                v2g vert (uint vertexID: SV_VertexID, uint instanceID : SV_InstanceID)
                {
                    v2g o;

                    float x = (float) (instanceID / (_Resolution * _Resolution));
                    float y = (float) ((instanceID / _Resolution) % _Resolution);
                    float z = (float) (instanceID % _Resolution);
                    
                    float val = SAMPLE_TEXTURE3D_LOD(_MainTex, sampler_MainTex, getUVW(x, y, z), 0).r;
                    float3 gradient = float3
                    (
                        SAMPLE_TEXTURE3D_LOD(_MainTex, sampler_MainTex, getUVW(x, y, z) + float3(1e-3, 0, 0), 0).r - val,
                        SAMPLE_TEXTURE3D_LOD(_MainTex, sampler_MainTex, getUVW(x, y, z) + float3(0, 1e-3, 0), 0).r - val,
                        SAMPLE_TEXTURE3D_LOD(_MainTex, sampler_MainTex, getUVW(x, y, z) + float3(0, 0, 1e-3), 0).r - val
                    );
                    // float size = (_MaxBound.x - _MinBound.x) / _Resolution;

                    o.worldPos = getOffset(x, y, z);
                    o.gradient = normalize(gradient);
                    o.value = val;

                    return o;
                }
                
                [maxvertexcount(2)] // Each arrow is a line (2 vertices)
                void geom(point v2g input[1], inout LineStream<g2f> outputStream)
                {
                    g2f outputVertex;

                    if (input[0].value > -0.3f) return;

                    // Base color (e.g., normalized gradient color)
                    float3 color = abs(input[0].gradient);
    
                    // First vertex (arrow base)
                    outputVertex.pos = TransformWorldToHClip(input[0].worldPos);
                    outputVertex.color = color;
                    outputStream.Append(outputVertex);

                    // Second vertex (arrow tip)
                    float3 tipPosition = input[0].worldPos + input[0].gradient * input[0].value;
                    outputVertex.pos = TransformWorldToHClip(tipPosition);
                    outputStream.Append(outputVertex);

                    outputStream.RestartStrip();
                }

                float4 frag (g2f i) : SV_Target
                {
                    return float4(1, 0, 0, 1.0);
                    // float3 lightDir = normalize(_MainLightPosition.xyz);
                    // float diff = max(dot(i.normalWS, lightDir), 0.2);
                    // float3 lighting = _MainLightColor.rgb * diff;
                    // return float4(lighting, 1);
                }
            
                ENDHLSL
            }
        }
    }
