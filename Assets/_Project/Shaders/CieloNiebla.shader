// Cielo de invierno (producción P1): un degradé del color de la niebla en el horizonte a un gris azulado arriba, más un sol
// chico y difuso. Como el horizonte toma el color de la niebla de la escena (RenderSettings.fogColor, que el día y la noche
// cambian), la nieve lejana se funde con el cielo sin una línea dura. _Exposure lo maneja el ciclo de día y noche (M13).
Shader "SurvivalEnTren/CieloNiebla"
{
    Properties
    {
        _ZenithColor ("Color arriba", Color) = (0.36, 0.44, 0.54, 1)
        _HorizonHeight ("Alto del degradé del horizonte", Range(0.02, 1)) = 0.35
        _HorizonPower ("Suavidad del degradé", Range(0.2, 4)) = 0.8
        _Exposure ("Exposición", Range(0, 8)) = 1.3
        _SunColor ("Color del sol", Color) = (1, 0.95, 0.85, 1)
        _SunSize ("Tamaño del sol (radio, en grados)", Range(0.1, 5)) = 0.6
        _SunGlow ("Halo del sol", Range(0, 2)) = 0.35
    }
    SubShader
    {
        Tags { "Queue" = "Background" "RenderType" = "Background" "PreviewType" = "Skybox" }
        Cull Off ZWrite Off

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                half4 _ZenithColor;
                half _HorizonHeight;
                half _HorizonPower;
                half _Exposure;
                half4 _SunColor;
                half _SunSize;
                half _SunGlow;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 direction : TEXCOORD0; };

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.direction = input.positionOS.xyz;
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                float3 dir = normalize(input.direction);
                // Horizonte (y debajo): el color de la niebla. Arriba: el color del cielo, con la exposición del día y la noche.
                half t = pow(saturate(dir.y / _HorizonHeight), _HorizonPower);
                half3 zenith = _ZenithColor.rgb * _Exposure / 1.3h;
                half3 color = lerp(unity_FogColor.rgb, zenith, t);
                // El sol: un disco chico y un halo, del color de la luz principal (de noche, la luna tenue).
                Light sun = GetMainLight();
                float facing = saturate(dot(dir, (float3)sun.direction)); // en float: cerca de 1 la precisión importa (el sol es chico)
                float radius = radians(_SunSize);
                float disc = smoothstep(cos(radius * 1.6), cos(radius), facing);
                float glow = pow(facing, 900.0) * _SunGlow + pow(facing, 60.0) * _SunGlow * 0.15;
                color += _SunColor.rgb * sun.color * (disc + glow) * saturate(dir.y * 8 + 0.2);
                return half4(color, 1);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
