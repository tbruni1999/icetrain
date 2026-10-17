# Arma el sombreador del terreno de nieve a partir del de URP. Uso, desde la carpeta del proyecto:
#   perl Fuentes/Sombreadores/generar.pl Library/PackageCache/com.unity.render-pipelines.universal@<versión>/Shaders/Terrain Assets/_Project/Shaders
# (también copia NieveSinRepeticion.hlsl, que está al lado de este script). Si se actualiza URP, correrlo de nuevo.
use strict; use warnings;
my ($o, $out) = @ARGV;
sub leer { my $f = shift; open(my $h, '<', $f) or die "$f: $!"; local $/; my $s = <$h>; close $h; $s =~ s/\r\n/\n/g; return $s; }
sub escribir { my ($f, $s) = @_; open(my $h, '>', $f) or die "$f: $!"; print $h $s; close $h; }
my $pkg = 'Packages/com.unity.render-pipelines.universal/Shaders/Terrain/';
my $cab = "// Copia del terreno de URP 17.6 (\"Universal Render Pipeline/Terrain/Lit\") con la nieve sin repetición (NieveSinRepeticion.hlsl).\n" .
          "// Generado por Fuentes/Sombreadores/generar.pl desde los originales del paquete; los cambios están marcados con \"Survival en tren\".\n";

# 1) El sombreador.
my $s = leer("$o/TerrainLit.shader");
$s =~ s{Shader "Universal Render Pipeline/Terrain/Lit"}{Shader "Survival en tren/Terreno Nieve"} or die "nombre";
$s =~ s{(\[ToggleUI\] _EnableInstancedPerPixelNormal[^\n]*\n)}{$1
        // Survival en tren: la nieve sin repetición (ver NieveSinRepeticion.hlsl).
        [Header(Nieve sin repeticion)]
        _NieveRuidoEscala("Cada cuantas baldosas cambia el corrimiento", Range(0.02, 1.0)) = 0.15
        _NieveLejosDesde("Lejos: desde (m)", Range(0, 200)) = 20
        _NieveLejosAncho("Lejos: transicion (m)", Range(1, 300)) = 60
        _NieveLejosEscala("Lejos: escala de la textura grande", Range(0.05, 1.0)) = 0.15
        _NieveLejosMezcla("Lejos: cuanto se mezcla", Range(0, 1)) = 0.7
        _NieveSimpleDesde("Lejos: desde donde se lee una sola vez (m)", Range(50, 2000)) = 250
        _NieveMacroEscala("Manchas de brillo: tamano (m)", Range(5, 500)) = 70
        _NieveMacroFuerza("Manchas de brillo: fuerza", Range(0, 0.3)) = 0.06
        [HideInInspector] _NieveTerrenoTamano("Lado del terreno (m)", Float) = 1600
} or die "props";
$s =~ s{\Q${pkg}\ETerrainLitInput\.hlsl}{TerrenoNieveInput.hlsl}g;
$s =~ s{\Q${pkg}\ETerrainLitPasses\.hlsl}{TerrenoNievePasses.hlsl}g;
$s =~ s{\Q${pkg}\ETerrainLitDepthNormalsPass\.hlsl}{TerrenoNieveDepthNormals.hlsl}g;
# Sin el editor propio de URP (es interno y no muestra los valores nuevos); el terreno pone solo _NORMALMAP y _MASKMAP.
$s =~ s{\n\s*CustomEditor "UnityEditor\.Rendering\.Universal\.TerrainLitShaderGUI"\n}{\n} or die "editor";
escribir("$out/TerrenoNieve.shader", $cab . $s);

# 2) Las entradas: los valores nuevos dentro de UnityPerMaterial.
my $i = leer("$o/TerrainLitInput.hlsl");
$i =~ s{(CBUFFER_START\(UnityPerMaterial\)\n)}{$1    // Survival en tren: la nieve sin repetición.\n    float _NieveRuidoEscala, _NieveLejosDesde, _NieveLejosAncho, _NieveLejosEscala, _NieveLejosMezcla;\n    float _NieveMacroEscala, _NieveMacroFuerza, _NieveTerrenoTamano, _NieveSimpleDesde;\n} or die "cbuffer";
escribir("$out/TerrenoNieveInput.hlsl", $cab . $i);

# 3) Las pasadas: leer cada capa sin repetición y preparar los corrimientos una vez por píxel.
my $p = leer("$o/TerrainLitPasses.hlsl");
$p =~ s{(#include "Packages/com\.unity\.render-pipelines\.universal/ShaderLibrary/DBuffer\.hlsl"\n)}{$1#include "NieveSinRepeticion.hlsl" // Survival en tren\n} or die "include";
my $n = 0;
$n += ($p =~ s{SAMPLE_TEXTURE2D\((_Normal(\d)), (sampler_Normal0), (uvSplat\d\d\.\w\w)\)}{NIEVE_LEER_CERCA($1, $3, $4, $2)}g);
$n += ($p =~ s{SAMPLE_TEXTURE2D\((_Splat(\d)), (sampler_Splat0), (uvSplat\d\d\.\w\w)\)}{NIEVE_LEER($1, $3, $4, $2)}g);
$n += ($p =~ s{SAMPLE_TEXTURE2D\((_Mask(\d)), (sampler_Mask0), (uvSplat\d\d\.\w\w)\)}{NIEVE_LEER_CERCA($1, $3, $4, $2)}g);
die "lecturas: $n" unless $n == 12;
$p =~ s{(    NormalMapMix\(uvSplat01, uvSplat23, splatControl, mixedNormal\);\n\}\n)}{    mixedDiffuse.rgb *= gNieveBrillo; // Survival en tren: manchas de brillo a gran escala\n\n$1} or die "brillo";
$p =~ s{(    half4 hasMask = half4\(_LayerHasMask0, _LayerHasMask1, _LayerHasMask2, _LayerHasMask3\);\n    half4 masks\[4\];\n    ComputeMasks\(masks, hasMask, IN\.uvSplat01, IN\.uvSplat23\);\n)}{    NieveSetup(IN.uvMainAndLM.xy, IN.uvSplat01, IN.uvSplat23, distance(IN.positionWS, GetCameraPositionWS())); // Survival en tren\n$1} or die "setup";
escribir("$out/TerrenoNievePasses.hlsl", $cab . $p);

# 4) Profundidad y normales (las usa la oclusión ambiental): las mismas normales sin repetición.
my $d = leer("$o/TerrainLitDepthNormalsPass.hlsl");
$d =~ s{#include "\Q${pkg}\ETerrainLitPasses\.hlsl"}{#include "TerrenoNievePasses.hlsl"} or die "dn include";
$d =~ s{(    half3 normalTS = half3\(0\.0h, 0\.0h, 1\.0h\);\n    NormalMapMix)}{    NieveSetup(IN.uvMainAndLM.xy, IN.uvSplat01, IN.uvSplat23, LinearEyeDepth(IN.clipPos.z, _ZBufferParams)); // Survival en tren (la profundidad en la vista)\n$1} or die "dn setup";
escribir("$out/TerrenoNieveDepthNormals.hlsl", $cab . $d);

# 5) La parte propia (no viene de URP).
(my $aca = __FILE__) =~ s{[^/\\]*$}{};
escribir("$out/NieveSinRepeticion.hlsl", leer("${aca}NieveSinRepeticion.hlsl"));
print "ok ($n lecturas)\n";
