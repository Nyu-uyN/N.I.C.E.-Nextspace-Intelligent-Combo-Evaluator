using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Represents a combo of tags and its associated score.
    /// Immutable and JSON-friendly.
    /// </summary>
    public sealed class Combo
    {
        /// <summary>
        /// tags that compose this combo.
        /// </summary>
        public IReadOnlyList<Tag> Tags { get; init; }

        /// <summary>
        /// Score of the combo.
        /// Higher is better.
        /// </summary>
        public int Score { get; init; }

        public Combo(IEnumerable<Tag> tags, int score)
        {
            if (tags is null)
                throw new ArgumentNullException(nameof(tags));

            Tags = new List<Tag>(tags).AsReadOnly();
            Score = score;
        }

        public override string ToString()
        {
            return $"Combo[{string.Join(", ", Tags.Select(t => t.Name))}] Score: {Score}";
        }
    }
}
