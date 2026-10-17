#ifndef SURVIVAL_NIEVE_SIN_REPETICION_INCLUDED
#define SURVIVAL_NIEVE_SIN_REPETICION_INCLUDED

// La nieve del piso sin repetición (Tomi, 2026-10-16: "la nieve se ve con repetición"). Tres cosas, con los valores en el
// material del terreno (Terreno_Lit):
// 1) Cada capa se lee dos veces con corrimientos que cambian de a poco por el terreno y se mezclan entre sí (técnica 3 de
//    Inigo Quilez, "texture repetition"): la baldosa deja de verse igual cada 2 m.
// 2) Lejos se mezcla con la misma textura mucho más grande: de lejos tampoco se ve la grilla.
// 3) Una variación de brillo muy suave a gran escala, como la nieve que el viento junta o barre.
// Todo se calcula con las coordenadas del terreno (no con las del mundo): no salta cuando el origen se recentra.

// Los valores (en UnityPerMaterial, declarados en TerrenoNieveInput.hlsl):
//   _NieveRuidoEscala   cada cuántas baldosas cambia el corrimiento (0,15 = cada ~7 baldosas)
//   _NieveLejosDesde    metros desde la cámara donde empieza la textura grande
//   _NieveLejosAncho    metros que tarda en entrar del todo
//   _NieveLejosEscala   la textura grande, en veces la normal (0,15: la baldosa de 2 m se ve de 13 m)
//   _NieveLejosMezcla   cuánto de la textura grande hay lejos (0 a 1)
//   _NieveSimpleDesde   metros desde donde cada capa se lee una sola vez, con la textura grande (lo de lejos cuesta casi lo
//                       mismo que la imagen de lejos de Unity; antes de llegar, la textura grande va ganando hasta el 100 %:
//                       no hay corte)
//   _NieveMacroEscala   metros de las manchas de brillo
//   _NieveMacroFuerza   cuánto cambia el brillo (0,06 = ±6 %)
//   _NieveTerrenoTamano metros de lado del terreno (lo pone la herramienta que arma la escena)

static float4 gNieveCorrimiento[4]; // xy: corrimiento A, zw: corrimiento B (en baldosas)
static half gNieveMezcla[4];        // de A a B
static bool gNieveActiva[4];        // si la capa se ve en este punto (las que no, no se leen: cuesta mucho menos)
static half gNieveLejos;            // 0 cerca, hasta _NieveLejosMezcla lejos (y 1 al llegar a _NieveSimpleDesde)
static bool gNieveSimple;           // muy lejos: una sola lectura por capa
static half gNieveBrillo;           // la variación de brillo de este punto

float NieveAzar(float2 p)
{
    p = frac(p * float2(123.34, 456.21));
    p += dot(p, p + 45.32);
    return frac(p.x * p.y);
}

float NieveRuido(float2 p)
{
    float2 i = floor(p);
    float2 f = frac(p);
    float2 u = f * f * (3.0 - 2.0 * f);
    return lerp(lerp(NieveAzar(i), NieveAzar(i + float2(1, 0)), u.x),
                lerp(NieveAzar(i + float2(0, 1)), NieveAzar(i + float2(1, 1)), u.x), u.y);
}

void NieveCapa(int i, float2 uv, half peso)
{
    gNieveActiva[i] = peso > 0.002h;
    gNieveCorrimiento[i] = 0;
    gNieveMezcla[i] = 0;
    UNITY_BRANCH
    if (gNieveActiva[i])
    {
        float l = NieveRuido(uv * _NieveRuidoEscala) * 8.0;
        float ia = floor(l);
        gNieveCorrimiento[i] = float4(sin(float2(3.0, 7.0) * ia), sin(float2(3.0, 7.0) * (ia + 1.0)));
        gNieveMezcla[i] = (half)smoothstep(0.2, 0.8, frac(l));
    }
}

// Se llama una vez por píxel, antes de leer las capas. Lee el mismo control de capas que el terreno (en el mismo lugar), para
// saber cuáles se ven acá.
void NieveSetup(float2 uvTerreno, float4 uvSplat01, float4 uvSplat23, float distancia)
{
    float2 splatUV = (uvTerreno * (_Control_TexelSize.zw - 1.0f) + 0.5f) * _Control_TexelSize.xy;
    half4 control = SAMPLE_TEXTURE2D(_Control, sampler_Control, splatUV);
    NieveCapa(0, uvSplat01.xy, control.r);
    NieveCapa(1, uvSplat01.zw, control.g);
    NieveCapa(2, uvSplat23.xy, control.b);
    NieveCapa(3, uvSplat23.zw, control.a);
    float simple = max(_NieveSimpleDesde, 1.0);
    half lejos = (half)(saturate((distancia - _NieveLejosDesde) / max(_NieveLejosAncho, 1.0)) * _NieveLejosMezcla);
    gNieveLejos = lerp(lejos, 1.0h, (half)saturate((distancia - simple * 0.6) / (simple * 0.4)));
    gNieveSimple = distancia >= simple;
    float2 m = uvTerreno * _NieveTerrenoTamano / max(_NieveMacroEscala, 1.0);
    float n = NieveRuido(m) * 0.65 + NieveRuido(m * 2.7 + 17.3) * 0.35;
    gNieveBrillo = (half)(1.0 + (n - 0.5) * 2.0 * _NieveMacroFuerza);
}

// Una capa: las dos lecturas con corrimiento (una sola donde no se mezclan) y, si conLejos, la textura grande lejos (solo para el
// color: en el relieve y en la máscara no se nota y cuesta).
half4 NieveLeer(TEXTURE2D_PARAM(tex, samp), float2 uv, int i, bool conLejos)
{
    float2 dx = ddx(uv);
    float2 dy = ddy(uv);
    half4 c = half4(0.5h, 0.5h, 1.0h, 0.5h);
    UNITY_BRANCH
    if (gNieveActiva[i] && gNieveSimple)
        c = SAMPLE_TEXTURE2D_GRAD(tex, samp, uv * _NieveLejosEscala + float2(0.37, 0.71), dx * _NieveLejosEscala, dy * _NieveLejosEscala);
    else if (gNieveActiva[i])
    {
        half t = gNieveMezcla[i];
        UNITY_BRANCH
        if (t < 0.002h) c = SAMPLE_TEXTURE2D_GRAD(tex, samp, uv + gNieveCorrimiento[i].xy, dx, dy);
        else if (t > 0.998h) c = SAMPLE_TEXTURE2D_GRAD(tex, samp, uv + gNieveCorrimiento[i].zw, dx, dy);
        else c = lerp(SAMPLE_TEXTURE2D_GRAD(tex, samp, uv + gNieveCorrimiento[i].xy, dx, dy),
                      SAMPLE_TEXTURE2D_GRAD(tex, samp, uv + gNieveCorrimiento[i].zw, dx, dy), t);
        UNITY_BRANCH
        if (conLejos && gNieveLejos > 0.001h)
        {
            half4 lejos = SAMPLE_TEXTURE2D_GRAD(tex, samp, uv * _NieveLejosEscala + float2(0.37, 0.71), dx * _NieveLejosEscala, dy * _NieveLejosEscala);
            c = lerp(c, lejos, gNieveLejos);
        }
    }
    return c;
}

// Relieve y máscara: una sola lectura, con el corrimiento que más pesa en este punto (en la franja donde el color mezcla dos, el
// relieve sigue al que más se ve; no se nota y cuesta la mitad).
half4 NieveLeerUna(TEXTURE2D_PARAM(tex, samp), float2 uv, int i)
{
    float2 dx = ddx(uv);
    float2 dy = ddy(uv);
    half4 c = half4(0.5h, 0.5h, 1.0h, 0.5h);
    UNITY_BRANCH
    if (gNieveActiva[i] && gNieveSimple)
        c = SAMPLE_TEXTURE2D_GRAD(tex, samp, uv * _NieveLejosEscala + float2(0.37, 0.71), dx * _NieveLejosEscala, dy * _NieveLejosEscala);
    else if (gNieveActiva[i])
        c = SAMPLE_TEXTURE2D_GRAD(tex, samp, uv + (gNieveMezcla[i] < 0.5h ? gNieveCorrimiento[i].xy : gNieveCorrimiento[i].zw), dx, dy);
    return c;
}

#define NIEVE_LEER(tex, samp, uv, i) NieveLeer(TEXTURE2D_ARGS(tex, samp), uv, i, true)
#define NIEVE_LEER_CERCA(tex, samp, uv, i) NieveLeerUna(TEXTURE2D_ARGS(tex, samp), uv, i)

#endif
