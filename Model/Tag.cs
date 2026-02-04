using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    public sealed class Tag
    {
        public int Id { get; init; }

        public string Name { get; init; } = string.Empty;

        public string Description { get; init; } = string.Empty;

        public Rarity Rarity { get; init; }

        public Category[] Categories { get; init; } = Array.Empty<Category>();

        public bool IsControversial { get; init; }

        public bool IsStoryMission { get; init; }

        /// <summary>
        /// List of tag IDs that are incompatible with this tag.
        /// </summary>
        public int[] IncompatibleTagIds { get; init; } = Array.Empty<int>();
    }
}
