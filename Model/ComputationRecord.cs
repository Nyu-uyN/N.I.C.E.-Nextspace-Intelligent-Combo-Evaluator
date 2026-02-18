using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Represents a persistent record of a completed computation.
    /// Stores the input parameters, the resulting loadout, and performance metadata.
    /// </summary>
    public class ComputationRecord
    {
        #region Structural Parameters (Immutable)

        /// <summary>
        /// Gets the unique MD5 hash identifying this specific configuration.
        /// </summary>
        public string ConfigurationHash { get; init; } = string.Empty;

        /// <summary>
        /// Gets the number of slots in the loadout for this computation.
        /// </summary>
        public int LoadoutSize { get; init; }

        /// <summary>
        /// Gets the number of tags per combo for this computation.
        /// </summary>
        public int ComboSize { get; init; }

        /// <summary>
        /// Gets a value indicating whether this record represents a disjoint set packing result.
        /// </summary>
        public bool IsDisjoint { get; init; }

        /// <summary>
        /// Gets the raw bitmask data representing the universe of tags used.
        /// Array must contain exactly 8 ulong elements (A to H).
        /// </summary>
        public ulong[] UniverseMaskData { get; init; } = new ulong[8];

        #endregion

        #region Results and Metadata

        /// <summary>
        /// Gets or sets the optimal list of combos found during the computation.
        /// </summary>
        public List<Combo> WinningLoadout { get; set; } = new();

        /// <summary>
        /// Gets or sets the date and time when the computation was performed.
        /// </summary>
        public DateTime ComputationDate { get; set; }

        /// <summary>
        /// Gets or sets the fastest time recorded to reach this result.
        /// </summary>
        public TimeSpan BestComputationTime { get; set; }

        /// <summary>
        /// Gets or sets the number of times this result has been verified by re-computation.
        /// </summary>
        public int ValidationCount { get; set; }



        #endregion

        #region Static Utilities

        /// <summary>
        /// Generates a unique MD5 hash based on computation parameters and the global state fingerprint.
        /// Any change in TAM or TIM data will result in a different hash, invalidating old records.
        /// </summary>
        /// <param name="maskData">The 8-ulong array representing the TagMask of the pool.</param>
        /// <param name="loadout">The target loadout size.</param>
        /// <param name="combo">The size of individual combos.</param>
        /// <param name="stateHash">The fingerprint of all tags and incompatibilities.</param>
        /// <returns>A unique MD5 hexadecimal string.</returns>
        public static string GenerateHash(ulong[] maskData, int loadout, int combo, string stateHash)
        {
            if (maskData == null || maskData.Length != 8)
                throw new ArgumentException("Universe mask data must consist of exactly 8 ulong values.", nameof(maskData));

            using var ms = new MemoryStream();
            using var writer = new BinaryWriter(ms);

            // 1. Structural parameters of the specific run
            for (int i = 0; i < 8; i++) writer.Write(maskData[i]);
            writer.Write(loadout);
            writer.Write(combo);

            // 2. Global state fingerprint
            writer.Write(Encoding.UTF8.GetBytes(stateHash));

            using var md5 = MD5.Create();
            byte[] hashBytes = md5.ComputeHash(ms.ToArray());

            StringBuilder sb = new StringBuilder();
            foreach (byte b in hashBytes) sb.Append(b.ToString("x2"));
            return sb.ToString();
        }

        /// <summary>
        /// Converts a TagMask struct into a serializable array of 64-bit integers.
        /// </summary>
        /// <param name="mask">The source TagMask.</param>
        /// <returns>An array containing the A, B, C, D, E, F, G, and H fields.</returns>
        public static ulong[] TagMaskToData(TagMask mask)
        {
            return new ulong[] { mask.A, mask.B, mask.C, mask.D, mask.E, mask.F, mask.G, mask.H };
        }

        /// <summary>
        /// Reconstructs a TagMask struct from an array of 64-bit integers.
        /// </summary>
        /// <param name="data">The source array (must contain 8 elements).</param>
        /// <returns>A TagMask initialized with the provided data, or TagMask.Empty if invalid.</returns>
        public static TagMask DataToTagMask(ulong[] data)
        {
            if (data == null || data.Length != 8)
                return TagMask.Empty;

            return new TagMask
            {
                A = data[0],
                B = data[1],
                C = data[2],
                D = data[3],
                E = data[4],
                F = data[5],
                G = data[6],
                H = data[7]
            };
        }

        #endregion
    }
}