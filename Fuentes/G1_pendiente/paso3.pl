# G1 paso 3: hacha y pico improvisados. Uso, desde la carpeta del proyecto: perl paso3.pl
use strict; use warnings;
sub leer { my $f = shift; open(my $h, '<', $f) or die "$f: $!"; local $/; my $s = <$h>; close $h; return $s; }
sub escribir { my ($f, $s) = @_; open(my $h, '>', $f) or die "$f: $!"; print $h $s; close $h; }
my $S = $ENV{G1_SCRIPTS} // "Assets/_Project/Scripts";

# 1) Guardado: el jugador tiene hacha y pico.
my $g = leer("$S/Save/SaveGame.cs");
$g =~ s~(        public bool wrench;\n)~$1        public bool axe, pickaxe; // G1 (2026-10-17)\n~ or die "savegame";
escribir("$S/Save/SaveGame.cs", $g);
my $v = leer("$S/Save/SaveSystem.cs");
$v =~ s~(            p\.wrench = gear\.Has\(GearId\.Wrench\);\n)~$1            p.axe = gear.Has(GearId.Axe);\n            p.pickaxe = gear.Has(GearId.Pickaxe);\n~ or die "capture";
$v =~ s~(            if \(data\.player\.wrench\) gear\.Give\(GearId\.Wrench\);\n)~$1            if (data.player.axe) gear.Give(GearId.Axe);\n            if (data.player.pickaxe) gear.Give(GearId.Pickaxe);\n~ or die "restore";
escribir("$S/Save/SaveSystem.cs", $v);

# 2) Cómo se mueve cada golpe (en los ejes de la vista): el hacha y el pico, de arriba y con más peso que el martillo.
my $m = leer("$S/Editor/TestScenesBuilder.Viewmodel.cs");
$m =~ s~(                    case GearId\.Wrench: wp = [^\n]*\n)~$1                    case GearId.Axe: wp = new Vector3(0.05f, 0.24f, -0.08f); wr = new Vector3(-80f, 0f, -15f); sp = new Vector3(-0.06f, -0.06f, 0.18f); sr = new Vector3(55f, 0f, 10f); kick = 2.2f; break; // G1
                    case GearId.Pickaxe: wp = new Vector3(0.04f, 0.26f, -0.1f); wr = new Vector3(-85f, 0f, -8f); sp = new Vector3(-0.04f, -0.07f, 0.2f); sr = new Vector3(60f, 0f, 5f); kick = 2.6f; break; // G1
~ or die "swing";
escribir("$S/Editor/TestScenesBuilder.Viewmodel.cs", $m);

# 3) Las casillas 6 y 7, los modelos en la mano y las teclas.
my $t = leer("$S/Editor/TestScenesBuilder.Tools.cs");
my $slots = <<'C';
            // G1 (2026-10-17): el hacha y el pico improvisados en las casillas 6 y 7 (si el archivo es de antes, se agregan).
            foreach ((GearId gear, string label, float swing, float hitAt) in new[] { (GearId.Axe, "HACHA", 0.75f, 0.45f), (GearId.Pickaxe, "PICO", 0.85f, 0.5f) })
            {
                if (System.Array.FindIndex(settings.Slots, s => s.kind == HandSlotKind.Gear && s.gear == gear) >= 0) continue;
                var slotsSo = new SerializedObject(settings);
                var slotList = slotsSo.FindProperty("slots");
                slotList.arraySize++;
                SerializedProperty slot = slotList.GetArrayElementAtIndex(slotList.arraySize - 1);
                slot.FindPropertyRelative("label").stringValue = label;
                slot.FindPropertyRelative("kind").enumValueIndex = (int)HandSlotKind.Gear;
                slot.FindPropertyRelative("gear").enumValueIndex = (int)gear;
                slot.FindPropertyRelative("swingSeconds").floatValue = swing;
                slot.FindPropertyRelative("hitAt").floatValue = hitAt;
                slot.FindPropertyRelative("reachMeters").floatValue = 2.2f;
                slot.FindPropertyRelative("windupRotation").vector3Value = Vector3.zero; // MigrateSwingPoses le pone el golpe
                slotsSo.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(settings);
            }

            // Las manos en primera persona (2026-10-16): su raíz, su capa y su cámara aparte (BuildViewmodel, abajo).
C
$t =~ s~            // Las manos en primera persona \(2026-10-16\): su raíz, su capa y su cámara aparte \(BuildViewmodel, abajo\)\.\n~$slots~ or die "slots";
my $models = <<'C';
            // Hacha y pico improvisados (G1): una piedra atada a un palo (Fuentes/Scripts_Manos/hacha_pico.py, hasta el arte final),
            // agarrados como el martillo: la cabeza a la vista y el lado que pega hacia adelante.
            Material stick = LibraryMaterial("MT_MaderaIntemperie") ?? handle;
            Material rock = LibraryMaterial("MT_RocaGris") ?? steel;
            var axe = Group("Mano_Hacha", hands);
            axe.localPosition = new Vector3(0.26f, -0.22f, 0.52f);
            axe.localRotation = GripPose(new Vector3(-0.1f, 1f, 0.4f), new Vector3(-0.32f, 0.28f, 0.9f));
            if (!ArtTool(axe, "Hacha"))
            {
                BoxMinMax("Mango", axe, new Vector3(-0.015f, -0.08f, -0.015f), new Vector3(0.015f, 0.42f, 0.015f), stick, false);
                BoxMinMax("Piedra", axe, new Vector3(-0.012f, 0.35f, -0.04f), new Vector3(0.012f, 0.43f, 0.11f), rock, false);
            }
            var pick = Group("Mano_Pico", hands);
            pick.localPosition = new Vector3(0.26f, -0.22f, 0.52f);
            pick.localRotation = GripPose(new Vector3(-0.1f, 1f, 0.4f), new Vector3(-0.32f, 0.28f, 0.9f));
            if (!ArtTool(pick, "Pico"))
            {
                BoxMinMax("Mango", pick, new Vector3(-0.016f, -0.08f, -0.016f), new Vector3(0.016f, 0.48f, 0.016f), stick, false);
                BoxMinMax("Piedra", pick, new Vector3(-0.015f, 0.43f, -0.13f), new Vector3(0.015f, 0.47f, 0.155f), rock, false);
            }
            // La sierra del arte no trae agarre:
C
$t =~ s~            // La sierra del arte no trae agarre:~$models~ or die "models";
$t =~ s~foreach \(Transform model in new\[\] \{ crowbar, saw, hammer, wrench \}\)~foreach (Transform model in new[] { crowbar, saw, hammer, wrench, axe, pick })~ or die "list";
$t =~ s~slots\[i\]\.gear == GearId\.Wrench \? wrench : null;~slots[i].gear == GearId.Wrench ? wrench :\n                      slots[i].gear == GearId.Axe ? axe : slots[i].gear == GearId.Pickaxe ? pick : null;~ or die "map";
$t =~ s~            keys\.arraySize = 5;\n            for \(int i = 0; i < 5; i\+\+\) keys\.GetArrayElementAtIndex\(i\)\.objectReferenceValue = FindActionReference\(\$"Player/Tool\{i \+ 1\}"\);~            int keyCount = Mathf.Min(slots.Length, 7); // teclas 1 a 7 (G1: 6 hacha, 7 pico)\n            keys.arraySize = keyCount;\n            for (int i = 0; i < keyCount; i++) keys.GetArrayElementAtIndex(i).objectReferenceValue = FindActionReference(\$"Player/Tool{i + 1}");~ or die "keys";
escribir("$S/Editor/TestScenesBuilder.Tools.cs", $t);

# 4) Las recetas del banco: hacha y pico improvisados, conocidos desde el principio (piedras sueltas + madera + tela).
my $b = leer("$S/Editor/TestScenesBuilder.cs");
$b =~ s~            public Blueprint Saw, Jerrycan, ShelfProject, Hammer, Wrench;~            public Blueprint Saw, Jerrycan, ShelfProject, Hammer, Wrench, Axe, Pickaxe;~ or die "set";
$b =~ s~(                    new\[\] \{ \(materials\.Metal, 1\) \}, new GearId\[0\], 6f, CraftOutputKind\.Gear, GearId\.Wrench, "queda al cinto: casilla 5"\),\n)~$1                // G1 (2026-10-17): las herramientas improvisadas, como al empezar en 7 Days (lista del grindeo, sección 4). PROPUESTA.
                Axe = LoadOrCreateBlueprint("HachaImprovisada", "plano_hacha_improvisada", "hacha improvisada",
                    "Una piedra en cuña atada a un palo: corta árboles muertos y troncos.", true, BlueprintKind.Craft,
                    new[] { (materials.Stone, 2), (materials.Wood, 1), (materials.Cloth, 1) }, new GearId[0], 6f, CraftOutputKind.Gear, GearId.Axe, "queda al cinto: casilla 6"),
                Pickaxe = LoadOrCreateBlueprint("PicoImprovisado", "plano_pico_improvisado", "pico improvisado",
                    "Una piedra alargada atada a un palo: pica rocas, mineral y carbón.", true, BlueprintKind.Craft,
                    new[] { (materials.Stone, 3), (materials.Wood, 1), (materials.Cloth, 1) }, new GearId[0], 6f, CraftOutputKind.Gear, GearId.Pickaxe, "queda al cinto: casilla 7"),
~ or die "planos";
$b =~ s~            Blueprint\[\] recipes = \{ plans\.Saw, plans\.Jerrycan, plans\.Hammer, plans\.Wrench, plans\.ShelfProject \};~            Blueprint[] recipes = { plans.Saw, plans.Jerrycan, plans.Hammer, plans.Wrench, plans.Axe, plans.Pickaxe, plans.ShelfProject };~ or die "catalogo";
escribir("$S/Editor/TestScenesBuilder.cs", $b);
print "ok\n";

# 5) Piedras sueltas para empezar (en TestScenesBuilder.Grind.cs) y su llamada al armar las señales.
my $gr = leer("$S/Editor/TestScenesBuilder.Grind.cs");
$gr =~ s~using UnityEngine;\n~using SurvivalEnTren.Items;\nusing SurvivalEnTren.Rail;\nusing UnityEngine;\n~ or die "usings";
my $stones = <<'C';
        // Piedras sueltas para empezar (G1): como en 7 Days, lo primero se junta del piso con E. Bultos de 1 o 2 piedras a pocos
        // metros de la vía, a los costados del tramo donde arranca el tren: alcanzan para el hacha y el pico improvisados.
        private static void BuildLooseStones(RailSegment segment, Transform root)
        {
            MaterialSet materials = LoadMaterials();
            BackpackSettings settings = LoadBackpackSettings();
            var group = new GameObject("Piedras_Sueltas").transform;
            group.SetParent(root, false);
            var random = new System.Random(17);
            for (int i = 0; i < 14; i++)
            {
                float along = Mathf.Min(30f + i * 9f + (float)random.NextDouble() * 5f, segment.Length - 2f);
                Vector3 at = segment.GetPosition(along);
                Vector3 forward = (segment.GetPosition(along + 1f) - at).normalized;
                float offset = (i % 2 == 0 ? 1f : -1f) * (4f + (float)random.NextDouble() * 6f);
                Vector3 side = Vector3.Cross(Vector3.up, forward) * offset;
                PlaceBundle(group, settings, materials.Stone, 1 + random.Next(2), group.InverseTransformPoint(at + side + Vector3.up * 0.3f));
            }
        }

        // Una piedra: un bulto irregular hecho de discos, con luz arriba a la izquierda.
C
$gr =~ s~        // Una piedra: un bulto irregular hecho de discos, con luz arriba a la izquierda\.\n~$stones~ or die "stones";
escribir("$S/Editor/TestScenesBuilder.Grind.cs", $gr);
my $b2 = leer("$S/Editor/TestScenesBuilder.cs");
$b2 =~ s~(            SetReference\(root\.Find\("Senal_Bifurcacion"\)\.GetComponent<ProbeSignal>\(\), "network", damagedSwitch\);\n)~$1            BuildLooseStones(straight1a, root); // G1: las primeras piedras, para el hacha y el pico\n~ or die "llamada";
escribir("$S/Editor/TestScenesBuilder.cs", $b2);
print "ok piedras\n";
