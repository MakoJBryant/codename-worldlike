Shader "Solar System/S_Ocean"
{
    Properties
    {
        _Color ("Color", Color) = (0,0.05,0.4,0.7) // Default ocean color with some transparency (A=0.7)
        _Radius ("Planet Radius", Float) = 1.0 // This is the base radius of the planet
        _OceanLevel ("Ocean Level (Relative to Radius)", Range(-1, 1)) = 0.0 // Offset for the ocean surface, relative to Radius
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" } // Ensure it renders as transparent and in the correct queue
        LOD 100

        // Enable alpha blending for transparency
        Blend SrcAlpha OneMinusSrcAlpha
        // Do not write to the Z-buffer so terrain behind the ocean is visible
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            // make fog work
            #pragma multi_compile_fog

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD2; // Pass world position for potential future use
            };

            fixed4 _Color; // Matches _Color in Properties
            float _Radius; // Matches _Radius in Properties
            float _OceanLevel; // Matches _OceanLevel in Properties

            v2f vert (appdata v)
            {
                v2f o;
                // Calculate the final radius for the ocean sphere.
                // It's the planet's base radius scaled by (1 + _OceanLevel).
                // _OceanLevel acts as a percentage offset.
                float finalOceanRadius = _Radius * (1.0 + _OceanLevel);

                // Scale the vertex by the final calculated ocean radius
                o.vertex = UnityObjectToClipPos(v.vertex * finalOceanRadius);

                o.uv = v.uv;
                o.worldPos = mul(unity_ObjectToWorld, v.vertex * finalOceanRadius).xyz;
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // Just output the color, which includes transparency from _Color.a
                fixed4 col = _Color;
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col;
            }
            ENDCG
        }
    }
}