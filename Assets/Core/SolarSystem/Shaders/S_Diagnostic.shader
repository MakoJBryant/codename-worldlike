Shader "Custom/Diagnostic"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        LOD 100

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha // Standard alpha blending
            ZWrite Off // Do not write to Z-buffer
            Cull Off // Render both sides

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" // Provides TransformObjectToWorld, TransformWorldToHClip

            struct Attributes
            {
                float4 positionOS : POSITION; // Object Space Position
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION; // Clip Space Position
                float3 positionWS : TEXCOORD0;   // World Space Position
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
            CBUFFER_END

            Varyings vert (Attributes input)
            {
                Varyings output;
                // Transform object space position to world space using the object's transform matrix
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                // Transform world space position to clip space (what the GPU uses to draw)
                output.positionCS = TransformWorldToHClip(output.positionWS.xyz);
                return output;
            }

            float4 frag (Varyings input) : SV_Target
            {
                // Simply output the color.
                // If this sphere moves with the planet, then the fundamental vertex transformation is working.
                return _Color;
            }
            ENDHLSL
        }
    }
}
