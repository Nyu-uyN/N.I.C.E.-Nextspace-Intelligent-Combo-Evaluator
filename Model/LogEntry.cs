using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Immutable record representing a specific point in the computation timeline.
    /// </summary>
    public class LogEntry
    {
        public string Timestamp { get; }
        public string Message { get; }
        public LogEventId EventId { get; }

        public LogEntry(string timestamp, string message, LogEventId id)
        {
            Timestamp = timestamp;
            Message = message;
            EventId = id;
        }

        public override string ToString() => $"[{Timestamp}] {Message}";
    }
}
