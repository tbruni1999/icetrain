# G1 paso 5: las zonas para picar en el constructor. Uso, desde la carpeta del proyecto (después de paso3.pl): perl paso5.pl
use strict; use warnings;
sub leer { my $f = shift; open(my $h, '<', $f) or die "$f: $!"; local $/; my $s = <$h>; close $h; return $s; }
sub escribir { my ($f, $s) = @_; open(my $h, '>', $f) or die "$f: $!"; print $h $s; close $h; }
my $S = $ENV{G1_SCRIPTS} // "Assets/_Project/Scripts";
my $SC = 'Fuentes/G1_pendiente'; # zonas.cs.txt está acá

my $gr = leer("$S/Editor/TestScenesBuilder.Grind.cs");
$gr =~ s~using SurvivalEnTren\.Items;\n~using SurvivalEnTren.Equipment;\nusing SurvivalEnTren.Items;\nusing SurvivalEnTren.Probe;\n~ or die "usings1";
$gr =~ s~using UnityEngine;\n~using UnityEditor;\nusing UnityEngine;\n~ or die "usings2";
my $zonas = leer("$SC/zonas.cs.txt");
$gr =~ s~(    public static partial class TestScenesBuilder\n    \{\n)~$1$zonas\n~ or die "zonas";
escribir("$S/Editor/TestScenesBuilder.Grind.cs", $gr);

my $b = leer("$S/Editor/TestScenesBuilder.cs");
$b =~ s~(            BuildLooseStones\(straight1a, root\); // G1: las primeras piedras, para el hacha y el pico\n)~$1            BuildGrindZones(); // G1: la cantera y el bosque muerto\n~ or die "llamada";
escribir("$S/Editor/TestScenesBuilder.cs", $b);
print "ok zonas\n";
