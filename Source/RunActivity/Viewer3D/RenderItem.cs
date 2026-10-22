// COPYRIGHT 2009, 2010, 2011, 2012, 2013 by the Open Rails project.
//
// This file is part of Open Rails.
//
// Open Rails is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// Open Rails is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with Open Rails.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Xna.Framework;

namespace Orts.Viewer3D
{
    [DebuggerDisplay("{Material} {RenderPrimitive} {Flags}")]
    public struct RenderItem
    {
        public Material Material;
        public RenderPrimitive RenderPrimitive;
        public Matrix XNAMatrix;
        public ShapeFlags Flags;
        public object ItemData;

        public RenderItem(Material material, RenderPrimitive renderPrimitive, ref Matrix xnaMatrix, ShapeFlags flags, object itemData = null)
        {
            Material = material;
            RenderPrimitive = renderPrimitive;
            XNAMatrix = xnaMatrix;
            Flags = flags;
            ItemData = itemData;
        }

        public class Comparer : IComparer<RenderItem>
        {
            readonly Vector3 XNAViewerPos;
            internal readonly IComparer<SortKey> CachedDistances;

            public Comparer(Vector3 viewerPos)
            {
                XNAViewerPos = viewerPos;
                XNAViewerPos.Z *= -1;
                CachedDistances = new CachedDistanceComparer(this);
            }

            #region IComparer<RenderItem> Members

            public int Compare(RenderItem x, RenderItem y)
            {
                // For unknown reasons, this would crash with an ArgumentException (saying Compare(x, x) != 0)
                // sometimes when calculated as two values and subtracted. Presumed cause is floating point.
                var xd = (x.XNAMatrix.Translation - XNAViewerPos).Length();
                var yd = (y.XNAMatrix.Translation - XNAViewerPos).Length();
                // The following avoids water levels flashing, by forcing that higher water levels are nearer to the
                // camera, which is always true except when camera is under water level, which is quite abnormal
                if (x.Material is WaterMaterial && y.Material is WaterMaterial && Math.Abs(yd - xd) < 1.0 && x.XNAMatrix.Translation.Y < XNAViewerPos.Y)
                {
                    return Math.Sign(x.XNAMatrix.Translation.Y - y.XNAMatrix.Translation.Y);
                }
                // If the absolute difference is >= 1mm use that; otherwise, they're effectively in the same
                // place so fall back to the SortIndex.
                if (Math.Abs(yd - xd) >= 0.001)
                    return Math.Sign(yd - xd);
                return Math.Sign(x.RenderPrimitive.SortIndex - y.RenderPrimitive.SortIndex);
            }

            internal struct SortKey
            {
                internal float Distance;
                internal float Height;
                internal float SortIndex;
                internal bool Water;
                internal int SourceIndex;
            }

            internal SortKey GetSortKey(RenderItem item, int index)
            {
                return new SortKey
                {
                    Distance = (item.XNAMatrix.Translation - XNAViewerPos).Length(),
                    Height = item.XNAMatrix.Translation.Y,
                    SortIndex = item.RenderPrimitive.SortIndex,
                    Water = item.Material is WaterMaterial,
                    SourceIndex = index,
                };
            }

            sealed class CachedDistanceComparer : IComparer<SortKey>
            {
                readonly float CameraHeight;

                internal CachedDistanceComparer(Comparer owner)
                {
                    CameraHeight = owner.XNAViewerPos.Y;
                }

                public int Compare(SortKey x, SortKey y)
                {
                    // Keep the same ordering rules as Compare(RenderItem, RenderItem).
                    if (x.Water && y.Water && Math.Abs(y.Distance - x.Distance) < 1.0 && x.Height < CameraHeight)
                        return Math.Sign(x.Height - y.Height);
                    if (Math.Abs(y.Distance - x.Distance) >= 0.001)
                        return Math.Sign(y.Distance - x.Distance);
                    return Math.Sign(x.SortIndex - y.SortIndex);
                }
            }

            #endregion
        }
    }

	public class RenderItemCollection : IList<RenderItem>, IEnumerator<RenderItem>
	{
		RenderItem[] Items = new RenderItem[4];
        RenderItem.Comparer.SortKey[] SortKeys;
		int ItemCount;
		int EnumeratorIndex;

		public RenderItemCollection()
		{
		}

        public int Capacity
        {
            get
            {
                return Items.Length;
            }
        }

		public int Count
		{
            get
            {
                return ItemCount;
            }
		}

		public void Sort(IComparer<RenderItem> comparer)
		{
			if (ItemCount < 2)
				return;

            // Sorting takes O(n log n) comparisons. Compute the expensive vector length
            // once per item instead of twice per comparison. Keep actual distances:
            // the existing 1mm tie and 1m water rules cannot use squared distances.
            // Derived comparers may supply their own interface implementation.
            if (comparer != null && comparer.GetType() == typeof(RenderItem.Comparer))
            {
                var cameraComparer = (RenderItem.Comparer)comparer;
                if (SortKeys == null || SortKeys.Length < ItemCount)
                    SortKeys = new RenderItem.Comparer.SortKey[Items.Length];
                for (var i = 0; i < ItemCount; i++)
                    SortKeys[i] = cameraComparer.GetSortKey(Items[i], i);
                // Sort compact, reference-free keys instead of moving matrices and
                // material references on every comparison and swap.
                Array.Sort(SortKeys, 0, ItemCount, cameraComparer.CachedDistances);
                for (var i = 0; i < ItemCount; i++)
                {
                    if (SortKeys[i].SourceIndex == i)
                        continue;
                    var saved = Items[i];
                    var destination = i;
                    var source = SortKeys[destination].SourceIndex;
                    while (source != i)
                    {
                        Items[destination] = Items[source];
                        SortKeys[destination].SourceIndex = destination;
                        destination = source;
                        source = SortKeys[destination].SourceIndex;
                    }
                    Items[destination] = saved;
                    SortKeys[destination].SourceIndex = destination;
                }
                return;
            }
			Array.Sort(Items, 0, ItemCount, comparer);
		}

		#region IList<RenderItem> Members

		public int IndexOf(RenderItem item)
		{
            throw new NotSupportedException();
		}

		public void Insert(int index, RenderItem item)
		{
            throw new NotSupportedException();
		}

		public void RemoveAt(int index)
		{
            throw new NotSupportedException();
		}

		public RenderItem this[int index]
		{
			get
			{
				throw new NotSupportedException();
			}
			set
			{
                throw new NotSupportedException();
			}
		}

		#endregion

		#region ICollection<RenderItem> Members

		public void Add(RenderItem item)
		{
			if (ItemCount == Items.Length)
			{
				var items = new RenderItem[Items.Length * 2];
				Array.Copy(Items, 0, items, 0, Items.Length);
                Items = items;
			}
			Items[ItemCount] = item;
			ItemCount++;
		}

		public void Clear()
		{
			Array.Clear(Items, 0, ItemCount);
			ItemCount = 0;
		}

		public bool Contains(RenderItem item)
		{
            throw new NotSupportedException();
		}

		public void CopyTo(RenderItem[] array, int arrayIndex)
		{
            throw new NotSupportedException();
		}

		int ICollection<RenderItem>.Count
		{
            get
            {
                throw new NotSupportedException();
            }
		}

		public bool IsReadOnly
		{
            get
            {
                throw new NotSupportedException();
            }
		}

		public bool Remove(RenderItem item)
		{
			throw new NotSupportedException();
		}

		#endregion

		#region IEnumerable<RenderItem> Members

		public IEnumerator<RenderItem> GetEnumerator()
		{
			Reset();
			return this;
		}

		#endregion

		#region IEnumerable Members

		System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
		{
			return GetEnumerator();
		}

		#endregion

		#region IEnumerator<RenderItem> Members

		public RenderItem Current
		{
            get
            {
                return Items[EnumeratorIndex];
            }
		}

		#endregion

		#region IEnumerator Members

		object System.Collections.IEnumerator.Current
		{
            get
            {
                return Current;
            }
		}

		public bool MoveNext()
		{
			EnumeratorIndex++;
			return EnumeratorIndex < ItemCount;
		}

		public void Reset()
		{
			EnumeratorIndex = -1;
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			// No op.
		}

		#endregion
    }

}
