using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Static utility to manage application file paths.
    /// Centralizes the location of data files and ensures directory existence.
    /// </summary>
    public static class AppPaths
    {
        public static readonly string DataFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Data");
        public static readonly string RecordsJson = Path.Combine(DataFolder, "ComputationRecords.json");
        public static readonly string TagsJson = Path.Combine(DataFolder, "tags.json");
        public static readonly string UserOverrideJson = Path.Combine(DataFolder, "user_overrides.json");
        public static readonly string LogFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "latest.log");

        /// <summary>
        /// Ensures that the required directory structure exists.
        /// Called once at application startup.
        /// </summary>
        public static void InitializeStructure()
        {
            if (!Directory.Exists(DataFolder)) Directory.CreateDirectory(DataFolder);
        }
    }
}
