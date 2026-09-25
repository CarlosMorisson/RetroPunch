Shader "Custom/RetroWave Grid"
{
    Properties
    {
        [Header(RetroWave Grid)]

        _GridThreshold(
            "Line Threshold",
            Range(0.0, 1.0)
        ) = 0.5

        _GridContrast(
            "Line Contrast",
            Range(0.0, 4.0)
        ) = 1.0


        [Space(10)]
        [Header(Primary Color Fundo)]

        _PrimaryColor(
            "Primary Color (Base)",
            Color
        ) = (0.05, 0.0, 0.15, 1.0)

        [Toggle(_PRIMARY_EMISSION)]
        _PrimaryEmission(
            "Primary Emissive?",
            Float
        ) = 0.0

        _PrimaryEmissionMap(
            "Primary Mask (branco = emite, opcional)",
            2D
        ) = "white" {}

        [HDR]
        _PrimaryEmissionColor(
            "Primary Emission Color",
            Color
        ) = (0.05, 0.0, 0.15, 1.0)

        _PrimaryEmissionStrength(
            "Primary Emission Strength",
            Range(0.0, 20.0)
        ) = 1.0


        [Space(10)]
        [Header(Secondary Color Linhas)]

        _SecondaryColor(
            "Secondary Color (Linhas)",
            Color
        ) = (1.0, 0.0, 0.65, 1.0)

        [Toggle(_SECONDARY_EMISSION)]
        _SecondaryEmission(
            "Secondary Emissive?",
            Float
        ) = 1.0

        _SecondaryEmissionMap(
            "Secondary Mask (branco = linha / emite)",
            2D
        ) = "black" {}

        [HDR]
        _SecondaryEmissionColor(
            "Secondary Emission Color",
            Color
        ) = (1.0, 0.0, 0.65, 1.0)

        _SecondaryEmissionStrength(
            "Secondary Emission Strength",
            Range(0.0, 20.0)
        ) = 3.0
    }


    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }

        LOD 100


        Pass
        {
            Name "RetroWaveGrid"

            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Cull Back
            ZWrite On
            ZTest LEqual


            HLSLPROGRAM

            #pragma target 3.0

            #pragma vertex RetroWaveVertex
            #pragma fragment RetroWaveFragment

            #pragma shader_feature_local _PRIMARY_EMISSION
            #pragma shader_feature_local _SECONDARY_EMISSION

            #pragma multi_compile_instancing


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            TEXTURE2D(_PrimaryEmissionMap);
            SAMPLER(sampler_PrimaryEmissionMap);

            TEXTURE2D(_SecondaryEmissionMap);
            SAMPLER(sampler_SecondaryEmissionMap);


            CBUFFER_START(UnityPerMaterial)

                float4 _PrimaryColor;
                float4 _PrimaryEmissionColor;
                float _PrimaryEmissionStrength;

                float4 _SecondaryColor;
                float4 _SecondaryEmissionColor;
                float _SecondaryEmissionStrength;

                float _GridThreshold;
                float _GridContrast;

            CBUFFER_END


            struct Attributes
            {
                float4 positionOS : POSITION;

                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID

                UNITY_VERTEX_OUTPUT_STEREO
            };


            Varyings RetroWaveVertex(
                Attributes IN
            )
            {
                Varyings OUT;


                UNITY_SETUP_INSTANCE_ID(IN);

                UNITY_TRANSFER_INSTANCE_ID(
                    IN,
                    OUT
                );

                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(
                    OUT
                );


                OUT.positionCS =
                    TransformObjectToHClip(
                        IN.positionOS.xyz
                    );


                OUT.uv = IN.uv;


                return OUT;
            }


            half4 RetroWaveFragment(
                Varyings IN
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);


                // --------------------------------------------------------
                // MASKS
                //
                // As duas mascaras sao lidas uma unica vez aqui.
                // Elas fazem DOIS trabalhos:
                //
                // 1) Definir a cor base (albedo): usamos a mascara
                //    Secondary pra decidir onde e linha (Secondary
                //    Color) e onde e fundo (Primary Color). Como as
                //    duas texturas sao opostas, nao precisamos de
                //    uma terceira textura de grid so pra isso.
                //
                // 2) Controlar a emissao de cada cor, de forma
                //    independente (mais abaixo).
                //
                // _GridThreshold / _GridContrast ajustam o corte da
                // mascara Secondary caso sua textura nao seja um
                // preto/branco 100% puro.
                // --------------------------------------------------------

                half primaryMask =
                    SAMPLE_TEXTURE2D(
                        _PrimaryEmissionMap,
                        sampler_PrimaryEmissionMap,
                        IN.uv
                    ).r;


                half secondaryMask =
                    SAMPLE_TEXTURE2D(
                        _SecondaryEmissionMap,
                        sampler_SecondaryEmissionMap,
                        IN.uv
                    ).r;


                half lineMask =
                    saturate(
                        (
                            secondaryMask - _GridThreshold
                        )
                        *
                        _GridContrast
                        +
                        0.5
                    );


                // --------------------------------------------------------
                // ALBEDO
                //
                // Primary = cor de fundo / Secondary = cor da linha
                // --------------------------------------------------------

                half3 finalColor =
                    lerp(
                        _PrimaryColor.rgb,
                        _SecondaryColor.rgb,
                        lineMask
                    );


                half finalAlpha =
                    lerp(
                        _PrimaryColor.a,
                        _SecondaryColor.a,
                        lineMask
                    );


                // --------------------------------------------------------
                // LED EMISSION
                //
                // Cada cor emite com base na SUA propria mascara
                // (ja lida acima), de forma independente. Onde a
                // mascara for branca (1), aquela cor emite; onde for
                // preta (0), nao emite. Como as duas texturas sao
                // opostas, elas nao se sobrepoem (a nao ser em
                // bordas com anti-aliasing - use Filter Mode Point
                // nas texturas se quiser evitar isso).
                // --------------------------------------------------------

                #ifdef _PRIMARY_EMISSION

                    finalColor +=
                        primaryMask *
                        _PrimaryEmissionColor.rgb *
                        _PrimaryEmissionStrength;

                #endif


                #ifdef _SECONDARY_EMISSION

                    finalColor +=
                        secondaryMask *
                        _SecondaryEmissionColor.rgb *
                        _SecondaryEmissionStrength;

                #endif


                return half4(
                    finalColor,
                    finalAlpha
                );
            }


            ENDHLSL
        }
    }


    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}