using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEnTren.Items
{
    /// <summary>Una cantidad de un material dentro de la mochila.</summary>
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
    /// La mochila del jugador (secciones 9 y 22.2, "Inventory / Carry"): materiales con peso y un límite
    /// de peso total. Solo guarda cuánto hay de cada material; quien saca o pone (desmontar, recoger un
    /// bulto, soltar) le pide o le da unidades. Nunca inventa ni pierde unidades: Add y Remove devuelven
    /// cuántas se movieron de verdad, y quien llama se queda con el resto.
    /// Los objetos enteros (cajas, bidones) no van acá: se llevan en la mano (PlayerCarrier).
    /// </summary>
    public class Backpack : MonoBehaviour
    {
        // Margen para que un peso que "justo entra" no se rechace por redondeo de decimales.
        private const float EpsilonKg = 1e-4f;

        [SerializeField] private BackpackSettings settings;

        private readonly List<ItemStack> _stacks = new List<ItemStack>();

        public float CapacityKg => settings.CapacityKg;
        /// <summary>Peso total de lo que hay, en kg.</summary>
        public float MassKg
        {
            get
            {
                float total = 0f;
                foreach (ItemStack stack in _stacks) total += stack.MassKg;
                return total;
            }
        }
        public float FreeKg => Mathf.Max(0f, CapacityKg - MassKg);
        /// <summary>Lo que hay, en el orden en que se fue guardando cada material.</summary>
        public IReadOnlyList<ItemStack> Stacks => _stacks;

        /// <summary>Se dispara cuando cambia el contenido (para el panel, el caminar, etc.).</summary>
        public event Action Changed;

        public int CountOf(ItemDefinition item)
        {
            int index = IndexOf(item);
            return index < 0 ? 0 : _stacks[index].Count;
        }

        /// <summary>Cuántas unidades de este material entran todavía, por el límite de peso.</summary>
        public int MaxAddable(ItemDefinition item) => Mathf.FloorToInt((FreeKg + EpsilonKg) / item.WeightKg);

        /// <summary>Guarda unidades; devuelve cuántas entraron de verdad (el resto no se guardó).</summary>
        public int Add(ItemDefinition item, int count)
        {
            int added = Mathf.Clamp(count, 0, MaxAddable(item));
            if (added <= 0) return 0;
            int index = IndexOf(item);
            if (index < 0) _stacks.Add(new ItemStack(item, added));
            else _stacks[index] = new ItemStack(item, _stacks[index].Count + added);
            Changed?.Invoke();
            return added;
        }

        /// <summary>Saca unidades; devuelve cuántas salieron de verdad (no se puede sacar lo que no hay).</summary>
        public int Remove(ItemDefinition item, int count)
        {
            int index = IndexOf(item);
            if (index < 0) return 0;
            int removed = Mathf.Clamp(count, 0, _stacks[index].Count);
            if (removed <= 0) return 0;
            int left = _stacks[index].Count - removed;
            if (left == 0) _stacks.RemoveAt(index);
            else _stacks[index] = new ItemStack(item, left);
            Changed?.Invoke();
            return removed;
        }

        private int IndexOf(ItemDefinition item)
        {
            for (int i = 0; i < _stacks.Count; i++)
                if (_stacks[i].Item == item) return i;
            return -1;
        }
    }
}
