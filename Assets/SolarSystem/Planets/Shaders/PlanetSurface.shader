Shader "SolarSystem/PlanetSurface"
{
    Properties
    {
        _OceanColor ("Ocean Color", Color) = (0, 0.2, 0.5, 1)
        _LowLandColor ("Lowland Color", Color) = (0.2, 0.6, 0.2, 1)
        _HighLandColor ("Highland Color", Color) = (0.5, 0.5, 0.5, 1)
        _OceanThreshold ("Ocean Threshold", Float) = 0.3
        _Radius ("Planet Radius", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        CGPROGRAM
        #pragma surface surf Standard fullforwardshadows

        struct Input
        {
            float3 worldPos;
        };

        fixed4 _OceanColor;
        fixed4 _LowLandColor;
        fixed4 _HighLandColor;
        float _OceanThreshold;
        float _Radius;

        void surf (Input IN, inout SurfaceOutputStandard o)
        {
            float elevation = length(IN.worldPos);
            float height = (elevation - _Radius);

            fixed4 surfaceColor;
            if (height < _OceanThreshold)
                surfaceColor = _OceanColor;
            else
            {
                float t = saturate((height - _OceanThreshold) * 5);
                surfaceColor = lerp(_LowLandColor, _HighLandColor, t);
            }

            o.Albedo = surfaceColor.rgb;
            o.Alpha = surfaceColor.a;
        }
        ENDCG
    }
    FallBack "Diffuse"
}
