using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Represents a simplified, serializable version of a combo for storage purposes.
    /// Stores only tag identifiers and the calculated score.
    /// </summary>
    public class RawCombo
    {
        /// <summary>
        /// Gets or sets the unique indices of the tags used in this combo.
        /// </summary>
        public List<int> TagIds { get; set; } = new();

        /// <summary>
        /// Gets or sets the total subscriber score achieved by this combo.
        /// </summary>
        public int Score { get; set; }
    }
}
