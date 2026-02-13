using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums
{/// <summary>
 /// Defines unique identifiers for computation events.
 /// Used for zero-allocation logging between the solver engine and the UI.
 /// </summary>
    public enum LogEventId : int
    {
        Error = 0,
        #region Engine Lifecycle (100-199)

        /// <summary>
        /// Signal that the computation engine has initialized.
        /// Value: Initial pool size or tag count.
        /// </summary>
        EngineStarted = 100,

        /// <summary>
        /// Signal that the solver is starting the mining phase (finding top combos).
        /// Value: Current pool size being mined.
        /// </summary>
        MiningPhaseStarted = 101,

        /// <summary>
        /// Signal that the solver is starting the packing phase (disjoint set search).
        /// Value: Initial greedy score to beat.
        /// </summary>
        PackingPhaseStarted = 102,

        /// <summary>
        /// Signal that the computation finished successfully.
        /// Value: Final optimal score found.
        /// </summary>
        ComputationCompleted = 103,

        /// <summary>
        /// Signal that the computation was manually aborted by the user.
        /// Value: Last best score found before abort.
        /// </summary>
        ComputationAborted = 104,

        #endregion

        #region Performance and State (200-299)

        /// <summary>
        /// Signal that the search pool has been doubled due to lack of proof or viability.
        /// Value: New pool size.
        /// </summary>
        PoolGrowth = 200,

        /// <summary>
        /// Periodic signal indicating the current progress of the root-level search.
        /// Value: Index of the current root element being evaluated.
        /// </summary>
        SearchDepthChanged = 201,

        #endregion

        #region Progress and Records (300-399)

        /// <summary>
        /// Signal that a new global best score has been identified during packing.
        /// Value: The new best score.
        /// </summary>
        NewGlobalBest = 300,

        #endregion
    }
}
