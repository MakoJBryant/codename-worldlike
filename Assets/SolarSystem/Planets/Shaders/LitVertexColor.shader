Shader "Custom/URPPlanetLit"
{
    Properties
    {
        _AmbientColor ("Ambient Color", Color) = (0.2, 0.2, 0.2, 1)
        _RimColor ("Rim Color", Color) = (0.4, 0.6, 1, 1)
        _RimPower ("Rim Power", Range(1,8)) = 3
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS : NORMAL;
                float4 color : COLOR;
                float3 viewDir : TEXCOORD0;
            };

            float4 _AmbientColor;
            float4 _RimColor;
            float _RimPower;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.normalWS = normalize(TransformObjectToWorldNormal(IN.normalOS));
                OUT.color = IN.color;

                float3 worldPos = TransformObjectToWorld(IN.positionOS.xyz);
                float3 viewPos = _WorldSpaceCameraPos;
                OUT.viewDir = normalize(viewPos - worldPos);

                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                Light mainLight = GetMainLight();
                half3 lightDir = normalize(mainLight.direction);
                half3 lightColor = mainLight.color.rgb;

                // Diffuse lambert lighting
                half diff = max(0, dot(IN.normalWS, lightDir));

                // Ambient lighting
                half3 ambient = _AmbientColor.rgb;

                // Rim lighting for subtle edge glow
                half rim = 1.0 - saturate(dot(IN.viewDir, IN.normalWS));
                rim = pow(rim, _RimPower);

                half3 rimLight = _RimColor.rgb * rim;

                // Combine lighting with vertex color (which encodes biome/elevation)
                half3 finalColor = IN.color.rgb * (diff * lightColor + ambient) + rimLight;

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
