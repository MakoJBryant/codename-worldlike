Shader "Custom/S_PlanetSurface"
{
    Properties
    {
        [Header(Planet Parameters)]
        _Radius("Radius", Float) = 1.0
        _MinHeight("Min Height", Float) = 0.0
        _MaxHeight("Max Height", Float) = 1.0
        _PlanetCenter("Planet Center (world space)", Vector) = (0, 0, 0, 0)
        _OceanColor("Ocean Color", Color) = (0.0, 0.0, 1.0, 1.0)

        [Header(Surface Textures)]
        _BiomeTexture("Biome Texture", 2D) = "white" {}
        _NormalMap("Normal Map", 2D) = "white" {}
        _NormalStrength("Normal Strength", Range(0.0, 10.0)) = 1.0
        _Smoothness("Smoothness", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
            "Queue" = "Geometry"
        }

        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _Radius;
                float _MinHeight;
                float _MaxHeight;
                float3 _PlanetCenter;
                float4 _OceanColor;
                float _NormalStrength;
                float _Smoothness;
            CBUFFER_END

            TEXTURE2D(_BiomeTexture);
            SAMPLER(sampler_BiomeTexture);
            TEXTURE2D(_NormalMap);
            SAMPLER(sampler_NormalMap);

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float2 uv : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                float3 posWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionWS = posWS;
                output.positionCS = TransformWorldToHClip(posWS);
                output.uv = input.uv;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // Safety: only subtract _PlanetCenter if it is valid (not near zero vector)
                float3 correctedWS = input.positionWS;
                if (length(_PlanetCenter) > 0.001)
                {
                    correctedWS -= _PlanetCenter;
                }

                // Prevent divide by zero if _MaxHeight == _MinHeight
                float heightRange = max(_MaxHeight - _MinHeight, 0.0001);

                float worldPosLength = length(correctedWS);

                // Normalize height between min and max heights
                float heightNormalized = saturate((worldPosLength - _MinHeight) / heightRange);

                float2 biomeUV = float2(heightNormalized, 0.5);
                float4 biomeColor = SAMPLE_TEXTURE2D(_BiomeTexture, sampler_BiomeTexture, biomeUV);

                // Normal mapping from height map texture
                float offset = 0.5; // This offset is large; consider reducing for better results (e.g., 0.01)
                float strength = _NormalStrength;
                float2 uv = input.uv;

                float heightCenter = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv).r;
                float heightU = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv + float2(offset, 0)).r;
                float heightV = SAMPLE_TEXTURE2D(_NormalMap, sampler_NormalMap, uv + float2(0, offset)).r;

                // Calculate tangent-space normal from height differences
                float3 va = float3(1, 0, (heightU - heightCenter) * strength);
                float3 vb = float3(0, 1, (heightV - heightCenter) * strength);
                float3 normalTS = normalize(cross(va, vb));

                // Output albedo color and alpha 1 (fully opaque)
                float3 albedo = biomeColor.rgb;
                float smoothness = _Smoothness;

                return float4(albedo, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Hidden/Universal Render Pipeline/Lit"
}
