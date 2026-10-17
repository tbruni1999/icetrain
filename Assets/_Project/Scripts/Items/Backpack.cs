using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEnTren.Items
{
    /// <summary>Una cantidad de piezas de un mismo material (para resúmenes y textos).</summary>
    public readonly struct ItemStack
    {
        public readonly ItemDefinition Item;
        public readonly int Count;

        public ItemStack(ItemDefinition item, int count)
        {
            Item = item;
            Count = count;
        }

        public float MassKg => Item.WeightKg * Count;
    }

    /// <summary>
    /// La mochila del jugador (secciones 9 y 22.2, "Inventory / Carry"): una cuadrícula de casillas donde cada pieza
    /// ocupa un rectángulo (DECISIÓN de Tomi). El límite duro es el espacio; el peso de lo que se lleva sigue frenando
    /// el caminar (PlayerCarrier). Solo guarda: quien saca o pone (desmontar, recoger un bulto, soltar, el panel) le
    /// pide o le da piezas. Nunca inventa ni pierde piezas: Add y Remove devuelven cuántas se movieron de verdad, y
    /// quien llama se queda con el resto.
    /// Los objetos enteros (cajas, bidones) no van acá: se llevan en la mano (PlayerCarrier).
    /// </summary>
    public class Backpack : MonoBehaviour
    {
        [SerializeField] private BackpackSettings settings;

        private ItemGrid _grid;

        /// <summary>La cuadrícula de la mochila (la usa el panel para acomodar piezas).</summary>
        public ItemGrid Grid
        {
            get
            {
                if (_grid == null)
                {
                    _grid = new ItemGrid(settings.GridWidth, settings.GridHeight);
                    _grid.Changed += () => Changed?.Invoke();
                }
                return _grid;
            }
        }

        /// <summary>Peso total de lo que hay, en kg.</summary>
        public float MassKg => Grid.MassKg;
        public int UsedCells => Grid.UsedCells;
        public int CellCount => Grid.CellCount;

        /// <summary>Se dispara cuando cambia el contenido (para el panel, el caminar, etc.).</summary>
        public event Action Changed;

        private void Awake() => _ = Grid;

        /// <summary>Lo que hay, agrupado por material, en el orden en que se fue guardando cada uno.</summary>
        public IReadOnlyList<ItemStack> Stacks
        {
            get
            {
                var order = new List<ItemDefinition>();
                var counts = new Dictionary<ItemDefinition, int>();
                foreach (GridItem g in Grid.Items)
                {
                    if (!counts.ContainsKey(g.Item)) { counts[g.Item] = 0; order.Add(g.Item); }
                    counts[g.Item] += g.Count;
                }
                var stacks = new List<ItemStack>(order.Count);
                foreach (ItemDefinition item in order) stacks.Add(new ItemStack(item, counts[item]));
                return stacks;
            }
        }

        public int CountOf(ItemDefinition item) => Grid.CountOf(item);

        /// <summary>Cuántas piezas más de este material entran todavía (según su tamaño y los huecos que quedan).</summary>
        public int MaxAddable(ItemDefinition item)
        {
            ItemGrid copy = Grid.Clone();
            int fits = 0;
            while (copy.TryAutoPlace(item) != null) fits++;
            return fits;
        }

        /// <summary>¿Entran todas estas piezas juntas? (para avisar antes de desmontar algo).</summary>
        public bool WouldFitAll(IEnumerable<(ItemDefinition item, int count)> pieces)
        {
            ItemGrid copy = Grid.Clone();
            foreach ((ItemDefinition item, int count) in pieces)
                for (int i = 0; i < count; i++)
                    if (copy.TryAutoPlace(item) == null) return false;
            return true;
        }

        /// <summary>
        /// Guarda piezas donde haya lugar (completando pilas del mismo material primero); devuelve cuántas entraron de verdad
        /// (el resto no se guardó).
        /// </summary>
        public int Add(ItemDefinition item, int count) => count > 0 ? Grid.AddUnits(item, count) : 0;

        /// <summary>Saca piezas (de las últimas pilas que entraron); devuelve cuántas salieron de verdad.</summary>
        public int Remove(ItemDefinition item, int count) => count > 0 ? Grid.Take(item, count) : 0;
    }
}
