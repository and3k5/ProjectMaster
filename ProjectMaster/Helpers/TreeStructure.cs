using System;
using System.Collections.Generic;

namespace ProjectMaster.Helpers
{
    public class TreeStructure<T> : ICloneable
    {
        public T Key { get; set; }
        public List<TreeStructure<T>> Values { get; set; }

        public object Clone()
        {
            var clone = new TreeStructure<T>
            {
                Key = (T) (Key as ICloneable)?.Clone(),
                Values = Values == null ? null : new List<TreeStructure<T>>(),
            };


            for (var i = 0; i < Values?.Count; i++)
                clone.Values.Add((TreeStructure<T>) (Values[i] as ICloneable).Clone());

            return clone;
        }
    }
}