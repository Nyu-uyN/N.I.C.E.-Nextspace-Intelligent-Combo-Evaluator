using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Handles persistent logging to a local text file.
    /// Overwrites existing log file upon initialization.
    /// </summary>
    public class DiskLogger : IDisposable
    {
        private readonly StreamWriter _writer;
        private const string FileName = "latest.log";

        public DiskLogger(bool isDisjoint)
        {
            try
            {
                if (isDisjoint)
                {// Overwrite file and enable auto-flush for real-time persistence
                    _writer = new StreamWriter(new FileStream(FileName, FileMode.Create, FileAccess.Write, FileShare.Read));
                    _writer.AutoFlush = true;
                }
            }
            catch (Exception)
            {
                // Fallback or silent failure if file is locked
            }
        }

        public void WriteEntry(string timestamp, string message)
        {
            _writer?.WriteLine($"[{timestamp}] {message}");
        }

        public void Dispose()
        {
            _writer?.Close();
            _writer?.Dispose();
        }
    }
}
