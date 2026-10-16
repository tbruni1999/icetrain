using System;
using SurvivalEnTren.Building;
using SurvivalEnTren.Equipment;
using SurvivalEnTren.Items;
using SurvivalEnTren.Survival;
using SurvivalEnTren.Train;
using UnityEngine;

namespace SurvivalEnTren.Crafting
{
    /// <summary>Dónde está la pieza que se trabaja.</summary>
    public enum ForgePieceAt
    {
        None,
        Fire,
        Anvil
    }

    /// <summary>
    /// La forja del tren (herramientas y forja, paso H4; idea de Tomi: "más survival, que tengas que utilizar una forja ... en algún
    /// momento vamos a usar el metal para las cosas del tren"). Vive en el rincón de la sala de máquinas y se construye por fases
    /// (BuildSite: hogar, fuelle, yunque). Terminada:
    /// - El fuego se alimenta con tablones (por la puerta del fogón) o con combustible del tanque (la válvula). La leña rinde menos.
    /// - Encendida se va calentando (y apagada se enfría); mientras está caliente calienta el vagón (se suma a la calefacción).
    /// - Se mete una chapa al fuego, se calienta hasta el rojo, se pasa al yunque y se trabaja a martillazos. Si se enfría hay que
    ///   volver a calentarla. Terminada, queda una planchuela sobre el yunque (un bulto para juntar con E).
    /// Los números están en ForgeSettings (Data/Crafting/Forja.asset).
    /// </summary>
    public class Forge : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

        [SerializeField] private ForgeSettings settings;
        [SerializeField] private GameClock clock;
        [SerializeField] private FuelTank tank;
        [SerializeField] private TrainInteriorClimate climate;
        [Tooltip("La obra de la forja: hasta que esté terminada no se usa.")]
        [SerializeField] private BuildSite site;
        [SerializeField] private BackpackSettings backpackSettings;
        [Header("Lo que se ve")]
        [Tooltip("Donde queda la pieza en el fuego y en el yunque.")]
        [SerializeField] private Transform fireSlot;
        [SerializeField] private Transform anvilSlot;
        [Tooltip("La pieza (chapa que se va volviendo planchuela).")]
        [SerializeField] private Renderer piece;
        [Tooltip("Las brasas (brillan con el calor de la forja).")]
        [SerializeField] private Renderer embers;
        [SerializeField] private Light fireLight;
        [SerializeField, Range(0f, 10f)] private float fireLightIntensity = 3f;

        private const float SkipStepHours = 0.05f;
        private MaterialPropertyBlock _pieceBlock, _embersBlock; // colores por objeto (la obra cambia los materiales a fantasma y de vuelta)
        private Vector3 _plateScale = new Vector3(0.28f, 0.012f, 0.28f), _barScale = new Vector3(0.05f, 0.018f, 0.46f);

        public ForgeSettings Settings => settings;
        public bool Ready => site == null || site.IsComplete;
        /// <summary>Horas de fuego que quedan en el fogón (de la leña).</summary>
        public float WoodHours { get; private set; }
        public bool ValveOpen { get; private set; }
        /// <summary>Qué tan caliente está la forja, de 0 (fría) a 1 (a pleno).</summary>
        public float Temperature { get; private set; }
        public bool Burning => WoodHours > 0f || (ValveOpen && tank != null && !tank.IsEmpty);
        public ForgePieceAt PieceAt { get; private set; }
        /// <summary>Calor de la pieza, de 0 a 1 (al rojo desde ForgeSettings.workHeat).</summary>
        public float PieceHeat { get; private set; }
        public int PieceHits { get; private set; }
        public bool PieceHot => PieceAt != ForgePieceAt.None && PieceHeat >= settings.WorkHeat;
        public int Forged { get; private set; }
        public Transform AnvilSlot => anvilSlot;

        /// <summary>Cualquier cambio (para el ruido y el guardado).</summary>
        public event Action Changed;
        /// <summary>Un martillazo en el yunque: dónde y si sirvió (la pieza estaba al rojo).</summary>
        public event Action<Vector3, bool> Hammered;
        /// <summary>Salió una planchuela.</summary>
        public event Action ForgedOne;

        private void Awake()
        {
            _pieceBlock = new MaterialPropertyBlock();
            _embersBlock = new MaterialPropertyBlock();
            if (piece != null) piece.gameObject.SetActive(false);
            Show();
        }

        private void OnEnable() { if (clock != null) clock.Skipped += OnSkipped; }
        private void OnDisable()
        {
            if (clock != null) clock.Skipped -= OnSkipped;
            if (climate != null) climate.ExtraHeatCPerHour = 0f;
        }

        private void Update()
        {
            Simulate(clock != null ? clock.DeltaHours : Time.deltaTime / 60f);
            Show();
        }

        private void OnSkipped(float hours)
        {
            for (float done = 0f; done < hours; done += SkipStepHours) Simulate(Mathf.Min(SkipStepHours, hours - done));
        }

        private void Simulate(float hours)
        {
            if (hours <= 0f) return;
            bool wasBurning = Burning;
            // El fuego: primero la leña; si no hay, el combustible (si la válvula está abierta y hay en el tanque).
            if (WoodHours > 0f) WoodHours = Mathf.Max(0f, WoodHours - hours);
            else if (ValveOpen)
            {
                float needed = settings.DieselLitersPerHour * hours;
                if (tank == null || tank.Draw(needed) < needed * 0.999f) { ValveOpen = false; Changed?.Invoke(); } // se acabó: se cierra sola
            }
            float target = Burning ? 1f : 0f;
            Temperature = Mathf.MoveTowards(Temperature, target, (Burning ? settings.HeatUpPerHour : settings.CoolDownPerHour) * hours);
            if (climate != null) climate.ExtraHeatCPerHour = Temperature * settings.CabinHeatCPerHour;

            // La pieza: en el fuego va hacia el calor de la forja; en el yunque se enfría.
            if (PieceAt == ForgePieceAt.Fire)
                PieceHeat = Mathf.MoveTowards(PieceHeat, Temperature, settings.PieceHeatPerHour * hours);
            else if (PieceAt == ForgePieceAt.Anvil)
                PieceHeat = Mathf.Max(0f, PieceHeat - settings.PieceCoolPerHour * hours);
            if (wasBurning != Burning) Changed?.Invoke();
        }

        // ------------------------------------------------------------------ Acciones (las usan ForgePart y Anvil)

        /// <summary>Echa un tablón al fogón (sale de la mochila). False si no hay o no entra.</summary>
        public bool AddWood(Backpack backpack)
        {
            if (!Ready || backpack == null || settings.WoodItem == null) return false;
            if (WoodHours + settings.WoodHoursPerPlank > settings.MaxWoodHours + 0.001f) return false;
            if (backpack.Remove(settings.WoodItem, 1) == 0) return false;
            WoodHours += settings.WoodHoursPerPlank;
            Changed?.Invoke();
            return true;
        }

        public bool CanAddWood => Ready && WoodHours + settings.WoodHoursPerPlank <= settings.MaxWoodHours + 0.001f;

        public void SetValve(bool open)
        {
            if (!Ready || ValveOpen == open) return;
            ValveOpen = open && tank != null && !tank.IsEmpty;
            Changed?.Invoke();
        }

        /// <summary>Mete una chapa de la mochila al fuego.</summary>
        public bool PutPiece(Backpack backpack)
        {
            if (!Ready || PieceAt != ForgePieceAt.None || backpack == null || settings.RawItem == null) return false;
            if (backpack.Remove(settings.RawItem, 1) == 0) return false;
            PieceAt = ForgePieceAt.Fire;
            PieceHeat = 0f;
            PieceHits = 0;
            Changed?.Invoke();
            return true;
        }

        public void MovePiece(ForgePieceAt to)
        {
            if (PieceAt == ForgePieceAt.None || to == ForgePieceAt.None || PieceAt == to) return;
            PieceAt = to;
            Changed?.Invoke();
        }

        /// <summary>Un martillazo en el yunque (el martillo en la mano). Sirve solo con la pieza al rojo.</summary>
        public bool Hammer(in ToolHit hit)
        {
            if (hit.Tool != GearId.Hammer || PieceAt != ForgePieceAt.Anvil) return false;
            bool hot = PieceHeat >= settings.WorkHeat;
            Hammered?.Invoke(hit.Point, hot);
            if (!hot) return false; // fría: rebota (hay que volver a calentarla)
            PieceHits++;
            PieceHeat = Mathf.Max(0f, PieceHeat - settings.HeatLossPerHit);
            if (PieceHits >= settings.HitsToForge) Finish();
            Changed?.Invoke();
            return true;
        }

        private void Finish()
        {
            Vector3 at = anvilSlot.position + Vector3.up * 0.12f;
            PieceAt = ForgePieceAt.None;
            PieceHeat = 0f;
            PieceHits = 0;
            Forged++;
            if (settings.ForgedItem != null && backpackSettings != null)
                MaterialBundle.Spawn(backpackSettings, settings.ForgedItem, 1, at, Vector3.zero);
            ForgedOne?.Invoke();
        }

        /// <summary>Deja la forja como estaba (al cargar una partida).</summary>
        public void Restore(float woodHours, bool valveOpen, float temperature, ForgePieceAt pieceAt, float pieceHeat, int pieceHits)
        {
            WoodHours = Mathf.Max(0f, woodHours);
            ValveOpen = valveOpen;
            Temperature = Mathf.Clamp01(temperature);
            PieceAt = pieceAt;
            PieceHeat = Mathf.Clamp01(pieceHeat);
            PieceHits = Mathf.Max(0, pieceHits);
            Show();
        }

        // ------------------------------------------------------------------ Lo que se ve

        private void Show()
        {
            if (fireLight != null)
            {
                fireLight.intensity = Temperature * fireLightIntensity * (1f + 0.12f * Mathf.Sin(Time.time * 13f) * Mathf.Sin(Time.time * 7.3f));
                fireLight.enabled = Temperature > 0.01f;
            }
            if (embers != null && !Ready) embers.SetPropertyBlock(null); // fantasma de la obra: con su propio color
            else if (embers != null)
            {
                embers.GetPropertyBlock(_embersBlock);
                _embersBlock.SetColor(EmissionId, new Color(1f, 0.38f, 0.08f) * (Temperature * 4f));
                _embersBlock.SetColor(BaseColorId, Color.Lerp(new Color(0.08f, 0.07f, 0.06f), new Color(0.5f, 0.2f, 0.05f), Temperature));
                embers.SetPropertyBlock(_embersBlock);
            }
            if (piece == null) return;
            bool shown = PieceAt != ForgePieceAt.None;
            if (piece.gameObject.activeSelf != shown) piece.gameObject.SetActive(shown);
            if (!shown) return;
            Transform slot = PieceAt == ForgePieceAt.Fire ? fireSlot : anvilSlot;
            if (piece.transform.parent != slot) piece.transform.SetParent(slot, false);
            piece.transform.localPosition = Vector3.up * 0.01f;
            piece.transform.localRotation = Quaternion.identity;
            float shaped = settings.HitsToForge > 0 ? (float)PieceHits / settings.HitsToForge : 0f;
            piece.transform.localScale = Vector3.Lerp(_plateScale, _barScale, shaped);
            // Del gris al rojo y al naranja con el calor.
            float h = PieceHeat;
            Color cold = new Color(0.32f, 0.33f, 0.35f);
            Color red = new Color(0.55f, 0.08f, 0.03f), orange = new Color(1f, 0.55f, 0.15f);
            Color hotColor = h < 0.7f ? Color.Lerp(cold, red, h / 0.7f) : Color.Lerp(red, orange, (h - 0.7f) / 0.3f);
            piece.GetPropertyBlock(_pieceBlock);
            _pieceBlock.SetColor(BaseColorId, hotColor);
            _pieceBlock.SetColor(EmissionId, h > 0.3f ? hotColor * ((h - 0.3f) * 2.5f) : Color.black);
            piece.SetPropertyBlock(_pieceBlock);
        }
    }
}
