Shader "Custom/DepthFromFrag"
{
    Properties
    {
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
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
            };

            
            CBUFFER_START(UnityPerMaterial)
                float4x4 invVP;
                float4x4 TRS;
            CBUFFER_END

            Varyings vert(Attributes IN, uint id : SV_InstanceID)
            {
                Varyings OUT;
                OUT.positionWS = mul(TRS, float4(IN.positionOS.xyz, 1));
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                return OUT;
            }

            float3 WorldPosFromDepth(float2 screenUV)
            {
                float depth = SampleSceneDepth(screenUV);

                #ifdef UNITY_REVERSED_Z
                    depth = 1 - depth;
                #endif

                float4 projPos = float4(screenUV * 2.0 - 1.0, depth * 2.0 - 1.0, 1.0);
                float4 worldPos = mul(invVP, projPos);
                worldPos /= worldPos.w;

                return worldPos.xyz;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 screenUV = IN.positionHCS.xy / _ScreenParams.xy;
                float3 worldPosDepth = WorldPosFromDepth(screenUV);

                // Distance from frag
                float dist = distance(worldPosDepth, IN.positionWS);

                // Color by distance (simple gradient)
                float3 col = lerp(float3(0, 0, 1), float3(1, 0, 0), saturate(dist)); // blue -> red

                return float4(col, 1.0);
            }
            ENDHLSL
        }
    }

}
