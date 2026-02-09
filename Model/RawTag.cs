using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    public class RawTag
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public int Rarity { get; init; }
        public string[] Categories { get; init; } = Array.Empty<string>();
        public bool IsControversial { get; init; }
        public bool IsStoryMission { get; init; }
        public int[] IncompatibleIds { get; init; } = Array.Empty<int>();
        public long MaxPotentialScore { get; set; }
    }
}
