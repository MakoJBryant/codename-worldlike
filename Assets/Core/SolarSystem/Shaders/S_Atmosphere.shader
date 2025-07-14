Shader "Custom/S_Atmosphere"
{
    Properties
    {
        // PlanetCenter is passed from C# script (AtmosphereController)
        _PlanetCenter("PlanetCenter", Vector) = (0, 0, 0, 0)
        // Changed default alpha to 1.0 (fully opaque) so it's visible by default
        [HDR]_AtmosphereColor("AtmosphereColor", Color) = (0.3921569, 0.7843137, 1, 1)
        _SunDirection("SunDirection", Vector) = (0, 1, 0, 0)
        _Density("Density", Range(0, 5)) = 1
        _Power("Power", Range(0, 10)) = 2
        // Added AtmosphereRadius property, as it's likely used in the shader logic
        // (even if not explicitly in the provided HLSL snippet, it's crucial for atmosphere size)
        _AtmosphereRadius("AtmosphereRadius", Float) = 1.02 // Default to slightly larger than planet radius
        [HideInInspector]_QueueOffset("_QueueOffset", Float) = 0
        [HideInInspector]_QueueControl("_QueueControl", Float) = -1
        [HideInInspector][NoScaleOffset]unity_Lightmaps("unity_Lightmaps", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_LightmapsInd("unity_LightmapsInd", 2DArray) = "" {}
        [HideInInspector][NoScaleOffset]unity_ShadowMasks("unity_ShadowMasks", 2DArray) = "" {}
    }
    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
            "RenderType"="Transparent"
            "UniversalMaterialType" = "Unlit"
            "Queue"="Transparent" // Ensure it renders after opaque objects
            "DisableBatching"="False"
            "ShaderGraphShader"="true" // Keep this tag for compatibility if needed
            "ShaderGraphTargetId"="UniversalUnlitSubTarget" // Keep this tag
        }
        Pass
        {
            Name "Universal Forward"
            Tags
            {
                // LightMode: <None>
            }
        
            // Render State
            Cull Off // Render both front and back faces (important for atmosphere)
            Blend SrcAlpha One, One One // Additive blending (common for atmosphere)
            ZTest LEqual // Render if Z is less than or equal to existing pixel
            ZWrite Off // Do not write to Z-buffer (important for transparency)
        
            HLSLPROGRAM
        
            // Pragmas
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_fog
            #pragma instancing_options renderinglayer
            #pragma vertex vert
            #pragma fragment frag
        
            // Keywords
            #pragma multi_compile _ LIGHTMAP_ON
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ USE_LEGACY_LIGHTMAPS
            #pragma shader_feature _ _SAMPLE_GI
            #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ DEBUG_DISPLAY
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
        
            // Defines
            #define ATTRIBUTES_NEED_NORMAL
            #define ATTRIBUTES_NEED_TANGENT
            #define FEATURES_GRAPH_VERTEX_NORMAL_OUTPUT
            #define FEATURES_GRAPH_VERTEX_TANGENT_OUTPUT
            #define VARYINGS_NEED_POSITION_WS
            #define VARYINGS_NEED_NORMAL_WS
            #define FEATURES_GRAPH_VERTEX
            #define SHADERPASS SHADERPASS_UNLIT
            #define _FOG_FRAGMENT 1
            #define _SURFACE_TYPE_TRANSPARENT 1
        
            // Includes
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Texture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRendering.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/TextureStack.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/DebugMipmapStreamingMacros.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/ShaderGraphFunctions.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DBuffer.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
        
            // --------------------------------------------------
            // Structs and Packing
        
            struct Attributes
            {
                float3 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float4 tangentOS : TANGENT;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(ATTRIBUTES_NEED_INSTANCEID)
                uint instanceID : INSTANCEID_SEMANTIC;
            #endif
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS;
                float3 normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            };
            struct SurfaceDescriptionInputs
            {
                float3 WorldSpaceViewDirection;
                float3 WorldSpacePosition;
            };
            struct VertexDescriptionInputs
            {
                float3 ObjectSpaceNormal;
                float3 ObjectSpaceTangent;
                float3 ObjectSpacePosition;
            };
            struct PackedVaryings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : INTERP0;
                float3 normalWS : INTERP1;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                uint instanceID : CUSTOM_INSTANCE_ID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                uint stereoTargetEyeIndexAsBlendIdx0 : BLENDINDICES0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                uint stereoTargetEyeIndexAsRTArrayIdx : SV_RenderTargetArrayIndex;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                FRONT_FACE_TYPE cullFace : FRONT_FACE_SEMANTIC;
            #endif
            };
        
            PackedVaryings PackVaryings (Varyings input)
            {
                PackedVaryings output;
                ZERO_INITIALIZE(PackedVaryings, output);
                output.positionCS = input.positionCS;
                output.positionWS.xyz = input.positionWS;
                output.normalWS.xyz = input.normalWS;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
            #endif
                return output;
            }
        
            Varyings UnpackVaryings (PackedVaryings input)
            {
                Varyings output;
                output.positionCS = input.positionCS;
                output.positionWS = input.positionWS.xyz;
                output.normalWS = input.normalWS.xyz;
            #if UNITY_ANY_INSTANCING_ENABLED || defined(VARYINGS_NEED_INSTANCEID)
                output.instanceID = input.instanceID;
            #endif
            #if (defined(UNITY_STEREO_MULTIVIEW_ENABLED)) || (defined(UNITY_STEREO_INSTANCING_ENABLED) && (defined(SHADER_API_GLES3) || defined(SHADER_API_GLCORE)))
                output.stereoTargetEyeIndexAsBlendIdx0 = input.stereoTargetEyeIndexAsBlendIdx0;
            #endif
            #if (defined(UNITY_STEREO_INSTANCING_ENABLED))
                output.stereoTargetEyeIndexAsRTArrayIdx = input.stereoTargetEyeIndexAsRTArrayIdx;
            #endif
            #if defined(SHADER_STAGE_FRAGMENT) && defined(VARYINGS_NEED_CULLFACE)
                output.cullFace = input.cullFace;
            #endif
                return output;
            }
        
            // --------------------------------------------------
            // Graph
        
            // Graph Properties
            CBUFFER_START(UnityPerMaterial)
            float4 _AtmosphereColor;
            float _Density;
            float _Power;
            float3 _SunDirection;
            float3 _PlanetCenter;
            float _AtmosphereRadius; // Added this property to HLSL
            UNITY_TEXTURE_STREAMING_DEBUG_VARS;
            CBUFFER_END
        
            // Graph Functions (from Shader Graph)
            void Unity_Subtract_float3(float3 A, float3 B, out float3 Out)
            {
                Out = A - B;
            }
        
            void Unity_Normalize_float3(float3 In, out float3 Out)
            {
                Out = normalize(In);
            }
        
            void Unity_DotProduct_float3(float3 A, float3 B, out float Out)
            {
                Out = dot(A, B);
            }
        
            void Unity_OneMinus_float(float In, out float Out)
            {
                Out = 1 - In;
            }
        
            void Unity_Saturate_float(float In, out float Out)
            {
                Out = saturate(In);
            }
        
            void Unity_Power_float(float A, float B, out float Out)
            {
                Out = pow(A, B);
            }
        
            void Unity_Multiply_float_float(float A, float B, out float Out)
            {
                Out = A * B;
            }
        
            void Unity_Multiply_float4_float4(float4 A, float4 B, out float4 Out)
            {
                Out = A * B;
            }
        
            // Graph Vertex
            struct VertexDescription
            {
                float3 Position;
                // Normal and Tangent are not used directly in the VertexDescriptionFunction,
                // but are defined in Attributes struct, so keep them here for completeness
                float3 Normal;
                float3 Tangent;
            };
        
            VertexDescription VertexDescriptionFunction(VertexDescriptionInputs IN)
            {
                VertexDescription description = (VertexDescription)0;
                description.Position = IN.ObjectSpacePosition;
                description.Normal = IN.ObjectSpaceNormal;    // Added back for completeness if needed by other passes
                description.Tangent = IN.ObjectSpaceTangent; // Added back for completeness if needed by other passes
                return description;
            }
        
            // Graph Pixel (Fragment Shader Logic)
            struct SurfaceDescription
            {
                float3 BaseColor;
                float Alpha;
            };
        
            SurfaceDescription SurfaceDescriptionFunction(SurfaceDescriptionInputs IN)
            {
                SurfaceDescription surface = (SurfaceDescription)0;
        
                // --- Core Positional Logic (from your Shader Graph) ---
                // Calculate fragment position relative to planet center
                float3 _PlanetRelativePos;
                Unity_Subtract_float3(IN.WorldSpacePosition, _PlanetCenter, _PlanetRelativePos);
        
                // Normalize to get the Planet_Relative_Normal
                float3 _PlanetRelativeNormal;
                Unity_Normalize_float3(_PlanetRelativePos, _PlanetRelativeNormal);
        
                // --- Top Branch (Sun Interaction) ---
                // Dot product between Planet_Relative_Normal and SunDirection
                float _SunDotNormal;
                Unity_DotProduct_float3(_PlanetRelativeNormal, _SunDirection, _SunDotNormal);
                float _OneMinusSunDotNormal;
                Unity_OneMinus_float(_SunDotNormal, _OneMinusSunDotNormal);
                float _SaturateSunDotNormal;
                Unity_Saturate_float(_OneMinusSunDotNormal, _SaturateSunDotNormal);
        
                // --- Bottom Branch (View Interaction / Fresnel) ---
                // Dot product between View Direction and Planet_Relative_Normal (Fresnel term)
                float _ViewDotNormal;
                Unity_DotProduct_float3(IN.WorldSpaceViewDirection, _PlanetRelativeNormal, _ViewDotNormal);
                float _OneMinusViewDotNormal;
                Unity_OneMinus_float(_ViewDotNormal, _OneMinusViewDotNormal);
                float _SaturateViewDotNormal;
                Unity_Saturate_float(_OneMinusViewDotNormal, _SaturateViewDotNormal);
        
                // --- Reconstructing your graph's logic ---
                // This part is a direct translation of your graph's connections
                // It appears to multiply the Fresnel effect by Density and then by the Sun's effect.
        
                // Calculate the view-dependent falloff (Fresnel-like)
                float _PowerResult;
                Unity_Power_float(_SaturateViewDotNormal, _Power, _PowerResult);
                float _DensityMultiply;
                Unity_Multiply_float_float(_PowerResult, _Density, _DensityMultiply);
        
                // Multiply the view-dependent falloff by the sun-dependent effect
                float _FinalEffectIntensity;
                Unity_Multiply_float_float(_DensityMultiply, _SaturateSunDotNormal, _FinalEffectIntensity);
        
                // Apply Atmosphere Color
                float4 _AtmosphereColorLinear = IsGammaSpace() ? LinearToSRGB(_AtmosphereColor) : _AtmosphereColor;
                // FIX: Direct multiplication instead of calling the helper function incorrectly
                float4 _FinalColor = (_FinalEffectIntensity.xxxx) * _AtmosphereColorLinear;
        
                surface.BaseColor = (_FinalColor.xyz);
                // CRITICAL FIX: Use the alpha channel from _AtmosphereColor for the final alpha
                // and multiply it by the calculated intensity if desired for fade-out.
                // Assuming _AtmosphereColor.a is the desired base transparency.
                surface.Alpha = _FinalColor.a * _FinalEffectIntensity; // Multiply the calculated intensity by the original alpha
                // If you want a fixed alpha from the property, use: surface.Alpha = _AtmosphereColor.a;
        
                return surface;
            }
        
            // --------------------------------------------------
            // Build Graph Inputs
            VertexDescriptionInputs BuildVertexDescriptionInputs(Attributes input)
            {
                VertexDescriptionInputs output;
                ZERO_INITIALIZE(VertexDescriptionInputs, output);
        
                output.ObjectSpaceNormal = input.normalOS;
                output.ObjectSpaceTangent = input.tangentOS.xyz;
                output.ObjectSpacePosition = input.positionOS;
        
                return output;
            }
            SurfaceDescriptionInputs BuildSurfaceDescriptionInputs(Varyings input)
            {
                SurfaceDescriptionInputs output;
                ZERO_INITIALIZE(SurfaceDescriptionInputs, output);
        
                output.WorldSpaceViewDirection = GetWorldSpaceNormalizeViewDir(input.positionWS);
                output.WorldSpacePosition = input.positionWS;
        
                return output;
            }
        
            // --------------------------------------------------
            // Main
        
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/Varyings.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/UnlitPass.hlsl"
        
            ENDHLSL
        }
        // Removed the MotionVectors pass as it's not strictly necessary for the visual effect
        // and simplifies the shader for debugging. You can add it back if needed.
    }
    // Fallback "Hidden/Universal Render Pipeline/FallbackError" // Shader Graph sometimes adds this
}
