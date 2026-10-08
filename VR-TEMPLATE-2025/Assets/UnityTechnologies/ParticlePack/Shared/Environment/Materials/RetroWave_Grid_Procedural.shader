Shader "Custom/RetroWave Grid Procedural"
{
    Properties
    {
        [Header(Base)]

        _BaseColor(
            "Base Color (fundo)",
            Color
        ) = (0.05, 0.0, 0.15, 1.0)


        [Space(10)]
        [Header(Grid Lines)]

        _LineColor(
            "Line Color",
            Color
        ) = (1.0, 0.0, 0.65, 1.0)

        _GridSpacing(
            "Grid Spacing (object space units)",
            Float
        ) = 1.0

        _LineThickness(
            "Line Thickness (object space units)",
            Range(0.001, 1.0)
        ) = 0.05

        [Toggle(_USE_X_AXIS)]
        _UseXAxisLines(
            "Lines on X Axis",
            Float
        ) = 1.0

        [Toggle(_USE_Y_AXIS)]
        _UseYAxisLines(
            "Lines on Y Axis",
            Float
        ) = 1.0


        [Space(10)]
        [Header(Line Emission)]

        [Toggle(_LINE_EMISSION)]
        _LineEmission(
            "Line Emissive?",
            Float
        ) = 1.0

        [HDR]
        _LineEmissionColor(
            "Line Emission Color",
            Color
        ) = (1.0, 0.0, 0.65, 1.0)

        _LineEmissionStrength(
            "Line Emission Strength",
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
            Name "RetroWaveGridProcedural"

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

            #pragma shader_feature_local _USE_X_AXIS
            #pragma shader_feature_local _USE_Y_AXIS
            #pragma shader_feature_local _LINE_EMISSION

            #pragma multi_compile_instancing


            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"


            CBUFFER_START(UnityPerMaterial)

                float4 _BaseColor;

                float4 _LineColor;
                float4 _LineEmissionColor;
                float _LineEmissionStrength;

                float _GridSpacing;
                float _LineThickness;

            CBUFFER_END


            struct Attributes
            {
                float4 positionOS : POSITION;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };


            struct Varyings
            {
                float4 positionCS : SV_POSITION;

                float3 positionOSInterpolated : TEXCOORD0;

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


                // Object space position is passed through as-is.
                // The fragment stage reconstructs the grid directly
                // from these interpolated vertex coordinates, so no
                // UV or texture is needed to know where the lines are.
                OUT.positionOSInterpolated =
                    IN.positionOS.xyz;


                return OUT;
            }


            // Distance (in object space units) from a coordinate to the
            // nearest grid line, spaced every "spacing" units.
            float DistanceToNearestLine(
                float coord,
                float spacing
            )
            {
                float scaledCoord =
                    coord /
                    max(spacing, 0.0001);


                float fractionalOffset =
                    frac(scaledCoord + 0.5) -
                    0.5;


                return abs(fractionalOffset) * spacing;
            }


            // Turns a distance-to-line into a 0-1 mask, anti-aliased
            // with screen-space derivatives so the line stays crisp
            // regardless of distance or viewing angle.
            float LineMaskFromDistance(
                float distanceToLine,
                float thickness
            )
            {
                float halfThickness =
                    thickness * 0.5;

                float edgeSoftness =
                    max(
                        fwidth(distanceToLine),
                        0.0001
                    );

                return 1.0 -
                    smoothstep(
                        halfThickness - edgeSoftness,
                        halfThickness + edgeSoftness,
                        distanceToLine
                    );
            }


            half4 RetroWaveFragment(
                Varyings IN
            ) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(IN);


                float lineMask = 0.0;


                #ifdef _USE_X_AXIS

                    float distanceX =
                        DistanceToNearestLine(
                            IN.positionOSInterpolated.x,
                            _GridSpacing
                        );

                    lineMask =
                        max(
                            lineMask,
                            LineMaskFromDistance(
                                distanceX,
                                _LineThickness
                            )
                        );

                #endif


                #ifdef _USE_Y_AXIS

                    float distanceY =
                        DistanceToNearestLine(
                            IN.positionOSInterpolated.y,
                            _GridSpacing
                        );

                    lineMask =
                        max(
                            lineMask,
                            LineMaskFromDistance(
                                distanceY,
                                _LineThickness
                            )
                        );

                #endif


                // --------------------------------------------------------
                // ALBEDO
                // --------------------------------------------------------

                half3 finalColor =
                    lerp(
                        _BaseColor.rgb,
                        _LineColor.rgb,
                        lineMask
                    );

                half finalAlpha =
                    lerp(
                        _BaseColor.a,
                        _LineColor.a,
                        lineMask
                    );


                // --------------------------------------------------------
                // LINE EMISSION
                //
                // Usa a mesma mascara procedural calculada acima -
                // nao existe textura de emissao, o "mapa" e gerado
                // aqui, por pixel, a partir da posicao interpolada
                // dos vertices do modelo.
                // --------------------------------------------------------

                #ifdef _LINE_EMISSION

                    finalColor +=
                        lineMask *
                        _LineEmissionColor.rgb *
                        _LineEmissionStrength;

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