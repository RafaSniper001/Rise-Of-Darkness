// Lit do URP com projeção triplanar no espaço do mundo: a textura cobre os modelos da nave
// (feitos no Blender, com UVs esticados pela escala) num tamanho fixo em metros.
// Cor, normal, rugosidade e emissão (luzes do painel) seguem o mesmo mapeamento.
Shader "Rise of Darkness/Triplanar Lit"
{
    Properties
    {
        _BaseMap ("Cor", 2D) = "white" {}
        _BaseColor ("Tom", Color) = (1, 1, 1, 1)
        [Normal] _BumpMap ("Normal", 2D) = "bump" {}
        _BumpScale ("Relevo", Float) = 1
        [NoScaleOffset] _RoughnessMap ("Rugosidade", 2D) = "gray" {}
        _Smoothness ("Brilho", Range(0, 1)) = 0.8
        _Metallic ("Metálico", Range(0, 1)) = 0.3
        [NoScaleOffset] _EmissionMap ("Emissão", 2D) = "black" {}
        [HDR] _EmissionColor ("Cor da emissão", Color) = (0, 0, 0, 1)
        _Tiling ("Metros por repetição", Float) = 8
        _Sharpness ("Transição entre lados", Range(1, 16)) = 8
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" "Queue" = "Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseMap_ST;
            half4 _BaseColor;
            half4 _EmissionColor;
            half _BumpScale;
            half _Smoothness;
            half _Metallic;
            float _Tiling;
            half _Sharpness;
            half _Cutoff;
        CBUFFER_END

        TEXTURE2D(_BaseMap);        SAMPLER(sampler_BaseMap);
        TEXTURE2D(_BumpMap);        SAMPLER(sampler_BumpMap);
        TEXTURE2D(_RoughnessMap);   SAMPLER(sampler_RoughnessMap);
        TEXTURE2D(_EmissionMap);    SAMPLER(sampler_EmissionMap);
        ENDHLSL

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma target 3.0
            #pragma vertex Vert
            #pragma fragment Frag

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            #pragma multi_compile _ _CLUSTER_LIGHT_LOOP
            #pragma multi_compile_fog
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                half fogFactor : TEXCOORD2;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings Vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                VertexPositionInputs position = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = position.positionCS;
                output.positionWS = position.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.fogFactor = ComputeFogFactor(position.positionCS.z);
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float3 n = normalize(input.normalWS);

                // peso de cada projeção (X, Y, Z) pela direção da face
                half3 blend = pow(abs(n), _Sharpness);
                blend /= dot(blend, 1.0);

                // UVs em metros; o lado de trás espelha para a textura não sair invertida
                float3 p = input.positionWS / _Tiling;
                half3 axisSign = n < 0 ? -1 : 1;
                // o Tiling do _BaseMap corrige texturas não quadradas
                float2 uvX = p.zy * float2(axisSign.x, 1) * _BaseMap_ST.xy;
                float2 uvY = p.xz * float2(axisSign.y, 1) * _BaseMap_ST.xy;
                float2 uvZ = p.xy * float2(-axisSign.z, 1) * _BaseMap_ST.xy;

                half4 albedo =
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvX) * blend.x +
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvY) * blend.y +
                    SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uvZ) * blend.z;

                half rough =
                    SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uvX).r * blend.x +
                    SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uvY).r * blend.y +
                    SAMPLE_TEXTURE2D(_RoughnessMap, sampler_RoughnessMap, uvZ).r * blend.z;

                half3 emission =
                    SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uvX).rgb * blend.x +
                    SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uvY).rgb * blend.y +
                    SAMPLE_TEXTURE2D(_EmissionMap, sampler_EmissionMap, uvZ).rgb * blend.z;

                // normais: "whiteout blend" (cada projeção vira normal do mundo)
                half3 tnX = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvX), _BumpScale);
                half3 tnY = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvY), _BumpScale);
                half3 tnZ = UnpackNormalScale(SAMPLE_TEXTURE2D(_BumpMap, sampler_BumpMap, uvZ), _BumpScale);
                tnX.x *= axisSign.x;
                tnY.x *= axisSign.y;
                tnZ.x *= -axisSign.z;
                tnX = half3(tnX.xy + n.zy, abs(tnX.z) * n.x);
                tnY = half3(tnY.xy + n.xz, abs(tnY.z) * n.y);
                tnZ = half3(tnZ.xy + n.xy, abs(tnZ.z) * n.z);
                float3 normalWS = normalize(tnX.zyx * blend.x + tnY.xzy * blend.y + tnZ.xyz * blend.z);

                InputData inputData = (InputData)0;
                inputData.positionWS = input.positionWS;
                inputData.positionCS = input.positionCS;
                inputData.normalWS = normalWS;
                inputData.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
                inputData.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
                inputData.fogCoord = input.fogFactor;
                inputData.bakedGI = SampleSH(normalWS);
                inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
                inputData.shadowMask = half4(1, 1, 1, 1);

                SurfaceData surface = (SurfaceData)0;
                surface.albedo = albedo.rgb * _BaseColor.rgb;
                surface.alpha = 1;
                surface.metallic = _Metallic;
                surface.smoothness = (1 - rough) * _Smoothness;
                surface.normalTS = half3(0, 0, 1);
                surface.occlusion = 1;
                surface.emission = emission * _EmissionColor.rgb;

                half4 color = UniversalFragmentPBR(inputData, surface);
                color.rgb = MixFog(color.rgb, input.fogFactor);
                return color;
            }
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On
            ZTest LEqual
            ColorMask 0

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex ShadowPassVertex
            #pragma fragment ShadowPassFragment
            #pragma multi_compile_instancing
            #pragma multi_compile_vertex _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #include "Packages/com.unity.render-pipelines.universal/Shaders/ShadowCasterPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthOnlyVertex
            #pragma fragment DepthOnlyFragment
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/Shaders/DepthOnlyPass.hlsl"
            ENDHLSL
        }

        Pass
        {
            Name "DepthNormals"
            Tags { "LightMode" = "DepthNormals" }
            ZWrite On

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex DepthNormalsVertex
            #pragma fragment DepthNormalsFragment
            #pragma multi_compile_instancing
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/Shaders/LitDepthNormalsPass.hlsl"
            ENDHLSL
        }
    }

    FallBack "Universal Render Pipeline/Lit"
}
