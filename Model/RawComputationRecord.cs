using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Represents a complete computation result ready for JSON serialization.
    /// Decouples engine-optimized models from disk persistence.
    /// </summary>
    public class RawComputationRecord
    {
        /// <summary>
        /// Gets or sets the unique configuration hash (MD5).
        /// </summary>
        public string ConfigurationHash { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the target number of slots in the loadout.
        /// </summary>
        public int LoadoutSize { get; set; }

        /// <summary>
        /// Gets or sets the number of tags per individual combo.
        /// </summary>
        public int ComboSize { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the computation used disjoint set packing.
        /// </summary>
        public bool IsDisjoint { get; set; }

        /// <summary>
        /// Gets or sets the raw bitmask of the tag universe (8 x ulong).
        /// </summary>
        public ulong[] UniverseMaskData { get; set; } = new ulong[8];

        /// <summary>
        /// Gets or sets the collection of winning combos in their raw identifier format.
        /// </summary>
        public List<RawCombo> WinningLoadout { get; set; } = new();

        /// <summary>
        /// Gets or sets the timestamp of the computation in ISO 8601 format.
        /// </summary>
        public string ComputationDate { get; set; } = string.Empty;

        /// <summary>
        /// Gets or sets the best execution time recorded in milliseconds.
        /// </summary>
        public long BestComputationTimeMs { get; set; }

        /// <summary>
        /// Gets or sets the total number of successful re-computation validations.
        /// </summary>
        public int ValidationCount { get; set; }
    }
}

