    Shader "Unlit/InstancedDepthShader"
    {
        Properties
        {
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
                #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
                
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
                    int width;
                    int height;
                CBUFFER_END

                #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DynamicScalingClamping.hlsl"

                TEXTURE2D_X_FLOAT(_CameraDepthTexture);
                float4 _CameraDepthTexture_TexelSize;

                // 2023.3 Deprecated. This is for backwards compatibility. Remove in the future.
                #define sampler_CameraDepthTexture sampler_PointClamp

                float SampleSceneDepth(float2 uv, SAMPLER(samplerParam))
                {
                    uv = ClampAndScaleUVForBilinear(UnityStereoTransformScreenSpaceTex(uv), _CameraDepthTexture_TexelSize.xy);
                    return SAMPLE_TEXTURE2D_X_LOD(_CameraDepthTexture, samplerParam, uv, 1.0f).r;
                }

                float SampleSceneDepth(float2 uv)
                {
                    return SampleSceneDepth(uv, sampler_PointClamp);
                }

                Varyings vert (Attributes v, uint instanceID : SV_InstanceID)
                {
                    Varyings o;
                    
                    int x = instanceID % height;
                    int y = instanceID / height;

                    float2 screenUV = float2((float)x / width, (float)y / height);

                    // Sample depth
                    float depth = 1 - SampleSceneDepth(screenUV);

                    // Convert to clip space Z ([-1, 1])
                    float clipZ = depth * 2.0 - 1.0;

                    // Reconstruct clip space position (NDC)
                    float4 clipPos = float4(screenUV * 2.0 - 1.0, clipZ, 1.0);

                    // Transform to world space
                    float4 worldPos = mul(_InvCameraViewProj, clipPos);
                    worldPos.xyz /= worldPos.w;

                    // o.positionCS = TransformWorldToHClip(float3(x * 0.01f, eyeDepth, y * 0.01f) + v.positionOS.xyz * 0.005f);
                    o.positionCS = TransformWorldToHClip(worldPos.xyz + v.positionOS.xyz * 0.005f);
                    o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                    return o;
                }
            
                float4 frag (Varyings i) : SV_Target
                {
                    float3 lightDir = normalize(_MainLightPosition.xyz);
                    float diff = max(dot(i.normalWS, lightDir), 0.2);
                    float3 lighting = _MainLightColor.rgb * diff;
                    return float4(0, 1, 0, 1.0);
                }
            
                ENDHLSL
            }
        }
    }
