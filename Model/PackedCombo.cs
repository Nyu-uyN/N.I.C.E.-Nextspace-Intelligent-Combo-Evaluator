using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Structure ultra-légère optimisée pour l'algorithme de Set Packing.
    /// Elle ne contient que les données vitales pour tester les collisions et sommer les scores.
    /// </summary>
    public readonly struct PackedCombo
    {
        /// <summary>
        /// L'index du combo original dans le pool source (permet la reconstruction).
        /// </summary>
        public readonly int OriginalIndex;

        /// <summary>
        /// Le score final du combo (déjà calculé).
        /// </summary>
        public readonly int Score;

        /// <summary>
        /// Le masque binaire des tags utilisés par ce combo.
        /// Permet de tester la collision avec un autre combo en une seule opération bitwise (AND).
        /// </summary>
        public readonly TagMask UsedTagsMask;

        public PackedCombo(int index, Combo sourceCombo)
        {
            OriginalIndex = index;
            Score = sourceCombo.Score;

            // Construction unique du masque pour éviter de le refaire à chaque itération
            var m = TagMask.Empty;
            if (sourceCombo.Tags != null)
            {
                foreach (var t in sourceCombo.Tags)
                {
                    m.SetBit(t.Index);
                }
            }
            UsedTagsMask = m;
        }
    }
}
