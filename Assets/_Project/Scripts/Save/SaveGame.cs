using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEnTren.Save
{
    /// <summary>
    /// La partida guardada (Anexo B.4: "un modelo versionado con IDs persistentes, seed del mundo y deltas de estado").
    /// La región de la V1 está hecha a mano: su "seed" es la escena misma. Se guarda solo lo que cambió respecto de cómo
    /// arranca la escena (lo desmontado, lo construido, lo que se movió) y al cargar se recarga la escena y se aplica encima.
    /// Se escribe como JSON legible (JsonUtility, sin paquetes nuevos).
    /// </summary>
    [Serializable]
    public class SaveGame
    {
        /// <summary>Versión del formato: si cambia, una partida vieja se puede convertir o rechazar con un aviso.</summary>
        public int version = SaveGame.CurrentVersion;
        public const int CurrentVersion = 1;
        /// <summary>La región (escena) donde se guardó: en la V1 hace de "seed" del mundo.</summary>
        public string scene;
        public string savedAtUtc;
        public double gameHours;

        public PlayerState player = new PlayerState();
        public TrainState train = new TrainState();
        /// <summary>Planos aprendidos (por ID): los iniciales no se guardan, se saben siempre.</summary>
        public List<string> blueprints = new List<string>();
        public List<GridEntry> backpack = new List<GridEntry>();
        public List<StorageState> storages = new List<StorageState>();
        public List<SiteState> sites = new List<SiteState>();
        public List<SwitchState> switches = new List<SwitchState>();
        /// <summary>Objetos desmontables que ya no están (por ID estable).</summary>
        public List<string> salvageDone = new List<string>();
        /// <summary>Objetos desmontables a medio desarmar (por ID estable).</summary>
        public List<ProgressEntry> salvageProgress = new List<ProgressEntry>();
        /// <summary>Todos los bultos de material del mundo (los de la escena y los que se soltaron).</summary>
        public List<BundleState> bundles = new List<BundleState>();
        /// <summary>Objetos que se llevan en la mano (cajas, bidones, también los fabricados).</summary>
        public List<CarryState> carryables = new List<CarryState>();
        /// <summary>Raciones que todavía no se comieron (por su lugar en la escena).</summary>
        public List<string> foodLeft = new List<string>();
        /// <summary>Hielos ya rotos (puertas, cambios) y los que están a medio romper.</summary>
        public List<string> iceBroken = new List<string>();
        public List<ProgressEntry> iceProgress = new List<ProgressEntry>();
        public List<DoorState> doors = new List<DoorState>();
        /// <summary>Combustible que queda en las fuentes del mundo (vehículo, depósito).</summary>
        public List<ProgressEntry> fuelSources = new List<ProgressEntry>();
        /// <summary>La señal fijada en la sonda (ID), o vacío.</summary>
        public string probeTarget = "";
        /// <summary>La forja del tren (H4): el fuego y la pieza que se está trabajando.</summary>
        public ForgeState forge = new ForgeState();
    }

    [Serializable]
    public class PlayerState
    {
        /// <summary>Si estaba dentro del tren, la posición es relativa al tren; si no, absoluta en el mundo.</summary>
        public bool inTrain;
        public Vector3 position;
        public float yaw;
        public float pitch;
        public float health, satiety, energy, warmth;
        public bool crowbar, handPump, saw, suit;
        public bool hammer;
    }

    [Serializable]
    public class ForgeState
    {
        public bool present;
        public float woodHours;
        public bool valveOpen;
        public float temperature;
        public int pieceAt;
        public float pieceHeat;
        public int pieceHits;
    }

    [Serializable]
    public class TrainState
    {
        public string segment;
        public float distance;
        public float speed;
        public int reverser;
        public int power;
        public int brake;
        public float fuelLiters;
        public float interiorC;
    }

    [Serializable]
    public class GridEntry
    {
        public string item;
        public int x, y;
        public bool rotated;
    }

    [Serializable]
    public class StorageState
    {
        public string id;
        public List<GridEntry> items = new List<GridEntry>();
    }

    [Serializable]
    public class SiteState
    {
        public string id;
        public int level;
        public int[] contributed;
    }

    [Serializable]
    public class SwitchState
    {
        public string id;
        public int condition;
        public bool diverging;
    }

    [Serializable]
    public class ProgressEntry
    {
        public string id;
        public float value;
    }

    [Serializable]
    public class BundleState
    {
        public string item;
        public int count;
        public bool inTrain;
        public Vector3 position;
    }

    [Serializable]
    public class CarryState
    {
        /// <summary>Dónde está en la escena (ruta de nombres); los fabricados no tienen lugar fijo y se crean de nuevo.</summary>
        public string key;
        public bool crafted;
        public bool inTrain;
        public Vector3 position;
        public Quaternion rotation;
        /// <summary>Litros si es un bidón; -1 si no.</summary>
        public float liters = -1f;
    }

    [Serializable]
    public class DoorState
    {
        public string key;
        public bool open;
    }
}
