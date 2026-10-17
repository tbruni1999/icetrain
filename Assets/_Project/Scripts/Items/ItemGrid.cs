using System;
using System.Collections.Generic;
using UnityEngine;

namespace SurvivalEnTren.Items
{
    /// <summary>
    /// Una pieza dentro de una cuadrícula: qué es, dónde está, si está girada y cuántas hay apiladas (G1, 2026-10-17: los
    /// materiales se apilan, DECISIÓN de Tomi; lo que no se apila tiene siempre 1).
    /// </summary>
    public sealed class GridItem
    {
        public readonly int Id;
        public readonly ItemDefinition Item;
        public int X, Y;
        public bool Rotated;
        /// <summary>Cuántas hay en esta pila (1 a Item.MaxStack).</summary>
        public int Count = 1;

        public GridItem(int id, ItemDefinition item, int x, int y, bool rotated)
        {
            Id = id;
            Item = item;
            X = x;
            Y = y;
            Rotated = rotated;
        }

        /// <summary>Cuántas casillas de ancho y de alto ocupa ahora (girada o no).</summary>
        public Vector2Int Size => Rotated ? new Vector2Int(Item.SizeCells.y, Item.SizeCells.x) : Item.SizeCells;
        public RectInt Rect => new RectInt(X, Y, Size.x, Size.y);
        /// <summary>Cuántas más entran en esta pila.</summary>
        public int Room => Mathf.Max(0, Item.MaxStack - Count);
        /// <summary>Peso de la pila, en kg.</summary>
        public float MassKg => Item.WeightKg * Count;
    }

    /// <summary>
    /// Una cuadrícula de casillas con piezas de distintos tamaños (la mochila, un estante). Es lógica pura: no sabe
    /// de pantalla ni de mundo. Nunca deja dos piezas encimadas ni fuera de los bordes: toda operación devuelve
    /// si pudo hacerse, y si no pudo la cuadrícula queda como estaba.
    /// El origen (0, 0) es la esquina de arriba a la izquierda.
    /// </summary>
    public sealed class ItemGrid
    {
        private readonly List<GridItem> _items = new List<GridItem>();
        private int _nextId = 1;

        public int Width { get; private set; }
        public int Height { get; private set; }
        public IReadOnlyList<GridItem> Items => _items;
        public int CellCount => Width * Height;

        /// <summary>Se dispara cuando cambia algo (se agrega, saca o mueve una pieza, o cambia el tamaño).</summary>
        public event Action Changed;

        public ItemGrid(int width, int height)
        {
            Width = Mathf.Max(0, width);
            Height = Mathf.Max(0, height);
        }

        /// <summary>Peso de todo lo que hay, en kg.</summary>
        public float MassKg
        {
            get
            {
                float total = 0f;
                foreach (GridItem g in _items) total += g.MassKg;
                return total;
            }
        }

        /// <summary>Casillas ocupadas por piezas.</summary>
        public int UsedCells
        {
            get
            {
                int used = 0;
                foreach (GridItem g in _items) used += g.Size.x * g.Size.y;
                return used;
            }
        }

        public int CountOf(ItemDefinition item)
        {
            int count = 0;
            foreach (GridItem g in _items) if (g.Item == item) count += g.Count;
            return count;
        }

        /// <summary>La pieza que ocupa una casilla, o null si está libre o fuera de la cuadrícula.</summary>
        public GridItem ItemAt(int x, int y)
        {
            foreach (GridItem g in _items)
            {
                RectInt r = g.Rect;
                if (x >= r.xMin && x < r.xMax && y >= r.yMin && y < r.yMax) return g;
            }
            return null;
        }

        /// <summary>
        /// ¿Entra esta pieza en esta posición, girada o no? "ignore" es una pieza que ya está en la cuadrícula y que se
        /// va a mover (no cuenta como obstáculo para sí misma).
        /// </summary>
        public bool CanPlace(ItemDefinition item, int x, int y, bool rotated, GridItem ignore = null)
        {
            if (rotated && !item.CanRotate) return false;
            Vector2Int size = rotated ? new Vector2Int(item.SizeCells.y, item.SizeCells.x) : item.SizeCells;
            if (x < 0 || y < 0 || x + size.x > Width || y + size.y > Height) return false;
            var wanted = new RectInt(x, y, size.x, size.y);
            foreach (GridItem g in _items)
            {
                if (g == ignore) continue;
                RectInt r = g.Rect;
                if (wanted.xMin < r.xMax && r.xMin < wanted.xMax && wanted.yMin < r.yMax && r.yMin < wanted.yMax) return false;
            }
            return true;
        }

        /// <summary>
        /// Pone una pieza (una pila de "count", hasta el máximo del material) en una posición exacta. Devuelve la pieza, o null
        /// si no entra ahí.
        /// </summary>
        public GridItem TryPlace(ItemDefinition item, int x, int y, bool rotated, int count = 1)
        {
            if (count < 1 || !CanPlace(item, x, y, rotated)) return null;
            var placed = new GridItem(_nextId++, item, x, y, rotated) { Count = Mathf.Min(count, item.MaxStack) };
            _items.Add(placed);
            Changed?.Invoke();
            return placed;
        }

        /// <summary>
        /// Guarda UNA pieza: primero en una pila del mismo material que tenga lugar y, si no hay, en el primer lugar libre (de
        /// arriba a abajo y de izquierda a derecha; primero sin girar y, si no hay, girada). Devuelve la pila donde quedó, o null
        /// si no hay lugar.
        /// </summary>
        public GridItem TryAutoPlace(ItemDefinition item)
        {
            foreach (GridItem g in _items)
                if (g.Item == item && g.Room > 0)
                {
                    g.Count++;
                    Changed?.Invoke();
                    return g;
                }
            if (FindFreeSpot(item, out int x, out int y, out bool rotated)) return TryPlace(item, x, y, rotated);
            return null;
        }

        /// <summary>Guarda varias piezas (completando pilas y abriendo nuevas). Devuelve cuántas entraron de verdad.</summary>
        public int AddUnits(ItemDefinition item, int count)
        {
            int added = 0;
            foreach (GridItem g in _items)
            {
                if (added >= count) break;
                if (g.Item != item || g.Room <= 0) continue;
                int put = Mathf.Min(g.Room, count - added);
                g.Count += put;
                added += put;
            }
            while (added < count && FindFreeSpot(item, out int x, out int y, out bool rotated))
            {
                var placed = new GridItem(_nextId++, item, x, y, rotated) { Count = Mathf.Min(item.MaxStack, count - added) };
                _items.Add(placed);
                added += placed.Count;
            }
            if (added > 0) Changed?.Invoke();
            return added;
        }

        /// <summary>Saca piezas de un material (de las últimas pilas que entraron). Devuelve cuántas salieron de verdad.</summary>
        public int Take(ItemDefinition item, int count)
        {
            int taken = 0;
            for (int i = _items.Count - 1; i >= 0 && taken < count; i--)
            {
                GridItem g = _items[i];
                if (g.Item != item) continue;
                int fromThis = Mathf.Min(g.Count, count - taken);
                g.Count -= fromThis;
                taken += fromThis;
                if (g.Count <= 0) _items.RemoveAt(i);
            }
            if (taken > 0) Changed?.Invoke();
            return taken;
        }

        /// <summary>Suma piezas a una pila que ya está (hasta su máximo). Devuelve cuántas entraron.</summary>
        public int AddToStack(GridItem stack, int count)
        {
            if (!_items.Contains(stack) || count <= 0) return 0;
            int put = Mathf.Min(stack.Room, count);
            if (put <= 0) return 0;
            stack.Count += put;
            Changed?.Invoke();
            return put;
        }

        /// <summary>Saca piezas de una pila en particular (si queda vacía, se quita). Devuelve cuántas salieron.</summary>
        public int TakeFrom(GridItem piece, int count)
        {
            if (!_items.Contains(piece) || count <= 0) return 0;
            int taken = Mathf.Min(piece.Count, count);
            piece.Count -= taken;
            if (piece.Count <= 0) _items.Remove(piece);
            Changed?.Invoke();
            return taken;
        }

        /// <summary>Dónde entraría esta pieza (sin ponerla).</summary>
        public bool FindFreeSpot(ItemDefinition item, out int x, out int y, out bool rotated)
        {
            for (int pass = 0; pass < (item.CanRotate ? 2 : 1); pass++)
            {
                rotated = pass == 1;
                for (y = 0; y < Height; y++)
                    for (x = 0; x < Width; x++)
                        if (CanPlace(item, x, y, rotated)) return true;
            }
            x = y = 0;
            rotated = false;
            return false;
        }

        /// <summary>Mueve (y gira) una pieza que ya está en la cuadrícula. Si no entra, queda donde estaba.</summary>
        public bool TryMove(GridItem piece, int x, int y, bool rotated)
        {
            if (!_items.Contains(piece) || !CanPlace(piece.Item, x, y, rotated, piece)) return false;
            piece.X = x;
            piece.Y = y;
            piece.Rotated = rotated;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Gira una pieza en el lugar (si girada entra sin pisar nada). Devuelve si pudo.</summary>
        public bool TryRotate(GridItem piece) => TryMove(piece, piece.X, piece.Y, !piece.Rotated);

        public bool Remove(GridItem piece)
        {
            if (!_items.Remove(piece)) return false;
            Changed?.Invoke();
            return true;
        }

        /// <summary>
        /// Agranda la cuadrícula (una mejora del estante). No se puede achicar: se perdería lugar de piezas.
        /// Devuelve false si el tamaño pedido es menor que el actual.
        /// </summary>
        public bool Resize(int width, int height)
        {
            if (width < Width || height < Height) return false;
            if (width == Width && height == Height) return true;
            Width = width;
            Height = height;
            Changed?.Invoke();
            return true;
        }

        /// <summary>Una copia con las mismas piezas en los mismos lugares, para probar "¿entra?" sin tocar la original.</summary>
        public ItemGrid Clone()
        {
            var copy = new ItemGrid(Width, Height);
            foreach (GridItem g in _items) copy._items.Add(new GridItem(copy._nextId++, g.Item, g.X, g.Y, g.Rotated) { Count = g.Count });
            return copy;
        }
    }
}
