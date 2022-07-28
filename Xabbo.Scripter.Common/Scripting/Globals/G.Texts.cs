using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Xabbo.Scripter.Scripting
{
    public partial class G
    {
        /// <summary>
        /// Attempts to get a badge name by its code from the external texts.
        /// </summary>
        public bool TryGetBadgeName(string code, out string? name)
            => Texts.TryGetValue(code, out name);

        /// <summary>
        /// Gets a badge name by its code from the external texts. Returns <c>null</c> if it is not found.
        /// </summary>
        public string? GetBadgeName(string code)
            => TryGetBadgeName($"badge_name_{code}", out string? name) ? name : null;

        /// <summary>
        /// Attempts to get a badge description by its code from the external texts.
        /// </summary>
        public bool TryGetBadgeDescription(string code, out string? description)
          => Texts.TryGetValue($"badge_desc_{code}", out description);

        /// <summary>
        /// Gets a badge description by its code from the external texts. Returns <c>null</c> if it is not found.
        /// </summary>
        public string? GetBadgeDescription(string code)
          => TryGetBadgeDescription(code, out string? description) ? description : null;

        /// <summary>
        /// Attempts to get an effect name by its ID from the external texts.
        /// </summary>
        public bool TryGetEffectName(int id, out string? name)
          => Texts.TryGetValue($"fx_{id}", out name);

        /// <summary>
        /// Gets an effect name by its ID from the external texts. Returns <c>null</c> if it is not found.
        /// </summary>
        public string? GetEffectName(int id)
          => TryGetEffectName(id, out string? name) ? name : null;

        /// <summary>
        /// Attempts to get an effect description by its ID from the external texts.
        /// </summary>
        public bool TryGetEffectDescription(int id, out string? description)
          => Texts.TryGetValue($"fx_{id}_desc", out description);

        /// <summary>
        /// Gets an effect description by its ID from the external texts. Returns <c>null</c> if it is not found.
        /// </summary>
        public string? GetEffectDescription(int id)
          => TryGetEffectDescription(id, out string? description) ? description : null;

        /// <summary>
        /// Attempts to get a hand item name by its ID from the external texts.
        /// </summary>
        public bool TryGetHandItemName(int id, [NotNullWhen(true)] out string? name) => Texts.TryGetValue($"handitem{id}", out name);

        /// <summary>
        /// Gets a hand item name by its ID from the external texts. Returns <c>null</c> if it is not found.
        /// </summary>
        public string? GetHandItemName(int id) => TryGetHandItemName(id, out string? name) ? name : null;

        /// <summary>
        /// Gets all hand item IDs matching the specified name from the external texts.
        /// </summary>
        public IEnumerable<int> GetHandItemIds(string name)
        {
            foreach (var (key, value) in Texts
                .Where(x => x.Value.Equals(name, StringComparison.OrdinalIgnoreCase)))
            {
                if (key.StartsWith("handitem"))
                    if (int.TryParse(key[8..], out int id))
                        yield return id;
            }
        }
    }
}
