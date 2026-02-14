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
        private readonly object _lock = new();
        private bool _isDisposed = false;
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
            lock (_lock) // Personne d'autre ne peut écrire ou fermer en même temps
            {
                if (_isDisposed || _writer == null) return;

                _writer.WriteLine($"[{timestamp}] {message}");
                _writer.Flush();
            }
        }

        public void Dispose()
        {
            lock (_lock) // On attend que la dernière écriture en cours finisse
            {
                if (_isDisposed) return;
                _isDisposed = true;

                _writer?.Close();
                _writer?.Dispose();
                
            }
        }
    }
}
