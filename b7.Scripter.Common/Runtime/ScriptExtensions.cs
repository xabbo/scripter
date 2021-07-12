using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace b7.Scripter.Runtime
{
    /// <summary>
    /// Provides extension methods for use within the scripting context.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class ScriptExtensions
    {
        /// <summary>
        /// Deconstructs a grouping to its key and elements.
        /// </summary>
        public static void Deconstruct<TKey, TElement>(this IGrouping<TKey, TElement> grouping,
            out TKey key, out IEnumerable<TElement> elements)
        {
            key = grouping.Key;
            elements = grouping;
        }
    }
}
