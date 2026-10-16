using SurvivalEnTren.Building;
using SurvivalEnTren.Crafting;
using SurvivalEnTren.Interaction;
using SurvivalEnTren.Items;
using SurvivalEnTren.Poi;
using SurvivalEnTren.Rail;
using SurvivalEnTren.Train;
using UnityEngine;

namespace SurvivalEnTren.Audio
{
    /// <summary>
    /// Los golpes y ruidos de las cosas que pasan en el mundo (M13): las palancas del pupitre, los cambios de vía, las
    /// puertas corredizas, el hielo que se rompe, lo que se desarma, lo que se aporta a una obra, el banco de trabajo y un
    /// plano nuevo. No es un objeto global: está en la escena y escucha los avisos (eventos) que esas cosas ya dan.
    /// Cada sonido sale del lugar donde pasa (3D), menos los de la mano del jugador.
    /// </summary>
    public class WorldSounds : MonoBehaviour
    {
        [SerializeField] private AudioMixSettings settings;
        [SerializeField] private TrainControls controls;
        [Tooltip("Dónde están las palancas del pupitre.")]
        [SerializeField] private Transform cab;
        [SerializeField] private BlueprintBook book;
        [Tooltip("La cabeza del jugador: los sonidos que hace él (aportar, aprender un plano) salen de acá.")]
        [SerializeField] private Transform listener;

        private AudioClip _lever, _switch, _door, _ice, _build, _salvage, _paper, _craft;
        private AudioClip _hitWood, _hitMetal, _hitCloth, _hitElectric, _hitIce, _anvilHot, _anvilCold;

        /// <summary>Cuántos sonidos de cosas sonaron (lo usan las pruebas).</summary>
        public int Played { get; private set; }
        public string LastSound { get; private set; } = "";

        private void Start()
        {
            _lever = ProceduralClips.Clank("Palanca", 260f, 0.25f, 71);
            _switch = ProceduralClips.Clank("Cambio_Via", 140f, 0.6f, 73);
            _door = ProceduralClips.Slide("Puerta_Corrediza", 0.6f, 79);
            _ice = ProceduralClips.Crack("Hielo", 0.7f, 83);
            _build = ProceduralClips.Thud("Obra", 0.25f, 89);
            _salvage = ProceduralClips.Crack("Desarme", 0.5f, 97);
            _paper = ProceduralClips.Slide("Papel", 0.3f, 101);
            _craft = ProceduralClips.Clank("Banco", 330f, 0.4f, 103);
            // Golpes con herramientas (H2): un ruido por material.
            _hitWood = ProceduralClips.Crack("Golpe_Madera", 0.16f, 107);
            _hitMetal = ProceduralClips.Clank("Golpe_Metal", 420f, 0.3f, 109);
            _hitCloth = ProceduralClips.Thud("Golpe_Tela", 0.14f, 113);
            _hitElectric = ProceduralClips.Clank("Golpe_Tablero", 690f, 0.2f, 127);
            _hitIce = ProceduralClips.Crack("Golpe_Hielo", 0.22f, 131);
            _anvilHot = ProceduralClips.Clank("Yunque_Caliente", 610f, 0.45f, 137);
            _anvilCold = ProceduralClips.Clank("Yunque_Frio", 880f, 0.3f, 139);

            if (controls != null) controls.Changed += () => Play(_lever, cab != null ? cab.position : listener.position, 0.5f, "palanca");
            foreach (RailSwitch rail in FindObjectsByType<RailSwitch>())
            {
                RailSwitch captured = rail;
                captured.Changed += () => Play(_switch, captured.Point, 1f, "cambio de vía");
            }
            foreach (SlidingDoor door in FindObjectsByType<SlidingDoor>())
            {
                SlidingDoor captured = door;
                captured.Moved += open => Play(_door, captured.transform.position, 0.7f, open ? "puerta abre" : "puerta cierra");
            }
            foreach (FrozenDoor ice in FindObjectsByType<FrozenDoor>())
            {
                FrozenDoor captured = ice;
                Vector3 where = captured.transform.position;
                captured.Broken += () => Play(_ice, where, 1f, "hielo");
                captured.Hit += point => Play(_hitIce, point, 0.6f, "golpe hielo");
            }
            foreach (SalvageProp prop in FindObjectsByType<SalvageProp>())
            {
                SalvageProp captured = prop;
                Vector3 where = captured.transform.position;
                captured.Salvaged += () => Play(_salvage, where, 0.9f, "desarme");
                captured.Hit += (point, surface) => Play(HitClip(surface), point, 0.7f, "golpe " + surface.ToString().ToLowerInvariant());
            }
            foreach (BuildSite site in FindObjectsByType<BuildSite>())
                site.Changed += () => Play(_build, listener.position, 0.6f, "obra");
            foreach (Workbench bench in FindObjectsByType<Workbench>())
            {
                Workbench captured = bench;
                captured.Crafted += () => Play(_craft, captured.transform.position, 0.8f, "banco");
            }
            if (book != null) book.Changed += () => Play(_paper, listener.position, 0.6f, "plano");
            // La forja (H4): el martillo en el yunque (en caliente suena distinto que en frío) y la planchuela terminada.
            foreach (Forge forge in FindObjectsByType<Forge>())
            {
                Forge captured = forge;
                captured.Hammered += (point, hot) => Play(hot ? _anvilHot : _anvilCold, point, 0.8f, hot ? "yunque" : "yunque en frío");
                captured.ForgedOne += () => Play(_craft, captured.AnvilSlot.position, 0.8f, "planchuela");
            }
        }

        private AudioClip HitClip(SalvageSurface surface) =>
            surface == SalvageSurface.Metal ? _hitMetal : surface == SalvageSurface.Tela ? _hitCloth : surface == SalvageSurface.Electrico ? _hitElectric : _hitWood;

        private void Play(AudioClip clip, Vector3 where, float volume, string what)
        {
            if (clip == null || Time.timeSinceLevelLoad < 1.5f) return; // al cargar una partida todo "cambia" de golpe: sin ruido
            AudioSource.PlayClipAtPoint(clip, where, settings.EffectsVolume * volume);
            Played++;
            LastSound = what;
        }
    }
}
