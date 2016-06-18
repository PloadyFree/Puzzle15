﻿using System;
using System.Collections.Generic;

namespace Puzzle15
{
    public class CellInfo<T>
    {
        public CellLocation Location { get; }
        public T Value { get; }

        public CellInfo(CellLocation location, T value)
        {
            Location = location;
            Value = value;
        }

        #region Equals, GetHashCode and ToString methods

        protected bool Equals(CellInfo<T> other)
        {
            return
                Equals(Location, other.Location) &&
                Equals(Value, other.Value);
        }

        public override bool Equals(object obj)
        {
            var other = obj as CellInfo<T>;
            return other != null && Equals(other);
        }

        public override int GetHashCode()
        {
            return Location.GetHashCode() ^ Value.GetHashCode();
        }

        public override string ToString()
        {
            return $"Position: {{{Location}}}, Value: {Value}";
        }

        #endregion
    }
}