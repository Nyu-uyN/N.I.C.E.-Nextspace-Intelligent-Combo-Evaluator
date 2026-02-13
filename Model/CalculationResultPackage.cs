using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Transient container used to pass results and session-specific telemetry 
    /// to the Results Window without persisting the logs.
    /// </summary>
    public class CalculationResultPackage
    {
        public ComputationRecord Record { get; set; }
        public LogEntry[] SessionLogs { get; set; } // Will be null for history or Top N
    }
}
