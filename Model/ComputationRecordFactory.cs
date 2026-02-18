using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Controller;
using N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Provides static methods to handle the serialization and deserialization 
    /// of computation records to and from the local file system.
    /// </summary>
    public static class ComputationRecordFactory
    {
        /// <summary>
        /// The default filename for storing computation records.
        /// </summary>
        private static string FilePath => AppPaths.RecordsJson;
        
        

        /// <summary>
        /// Configures the JSON serializer options for readable output and compatibility.
        /// </summary>
        private static readonly JsonSerializerOptions SerializerOptions = new JsonSerializerOptions
        {
            
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        };

        /// <summary>
        /// Serializes a collection of computation records and saves them to a JSON file.
        /// </summary>
        /// <param name="records">The collection of records to persist.</param>
        /// <returns>True if the operation succeeded, otherwise false.</returns>
        public static bool SaveRecords(IEnumerable<ComputationRecord> records)
        {
            try
            {
                // Ensure the directory exists (useful if a custom path is ever used)
                string directory = Path.GetDirectoryName(FilePath);
                if (directory != null && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }
                var raws = new List<RawComputationRecord>();
                foreach (var record in records) {
                    var raw = ComputationRecordFactory.MapToRaw(record);
                    raws.Add(raw);
                }



                string jsonContent = JsonSerializer.Serialize(raws, SerializerOptions);
                File.WriteAllText(FilePath, jsonContent);
                return true;
            }
            catch (Exception)
            {
                // Errors should be handled by the caller or logged via the future Log Window
                return false;
            }
        }

        /// <summary>
        /// Loads computation records from the local JSON file.
        /// </summary>
        /// <returns>
        /// A dictionary where the key is the ConfigurationHash for rapid lookup.
        /// Returns an empty dictionary if the file does not exist or is corrupted.
        /// </returns>
        public static Dictionary<string, ComputationRecord> LoadRecords()
        {
            var dictionary = new Dictionary<string, ComputationRecord>();

            if (!File.Exists(FilePath))
            {
                return dictionary;
            }

            try
            {
                string jsonContent = File.ReadAllText(FilePath);
                var recordsList = JsonSerializer.Deserialize<List<RawComputationRecord>>(jsonContent, SerializerOptions);

                if (recordsList != null)
                {
                    foreach (var raw in recordsList)
                    {
                        ComputationRecord record = ComputationRecordFactory.FromRaw(raw, TagController.Tags);
                        // Ensure the hash is used as the unique key for O(1) access
                        if (!string.IsNullOrEmpty(record.ConfigurationHash))
                        {
                            dictionary[record.ConfigurationHash] = record;
                        }
                    }
                }
            }
            catch (JsonException)
            {
                // Return empty dictionary on deserialization failure to prevent app crash
                return dictionary;
            }
            catch (IOException)
            {
                // Return empty dictionary on file access issues
                return dictionary;
            }

            return dictionary;
        }


        /// <summary>
        /// Maps a domain ComputationRecord to a serializable RawComputationRecord.
        /// </summary>
        /// <param name="record">The source domain record.</param>
        /// <returns>A raw record suitable for JSON serialization.</returns>
        public static RawComputationRecord MapToRaw(ComputationRecord record)
        {
            return new RawComputationRecord
            {
                ConfigurationHash = record.ConfigurationHash,
                LoadoutSize = record.LoadoutSize,
                ComboSize = record.ComboSize,
                IsDisjoint = record.IsDisjoint,
                UniverseMaskData = (ulong[])record.UniverseMaskData.Clone(),
                ComputationDate = record.ComputationDate.ToString("o"), // ISO 8601
                BestComputationTimeMs = (long)record.BestComputationTime.TotalMilliseconds,
                ValidationCount = record.ValidationCount,
                WinningLoadout = record.WinningLoadout.Select(c => new RawCombo
                {
                    Score = c.Score,
                    TagIds = c.Tags.Select(t => t.Index).ToList()
                }).ToList()
            };
        }

        /// <summary>
        /// Reconstructs a domain ComputationRecord from raw storage data.
        /// Ensures all internal logic (masks, multipliers) is re-initialized via constructors.
        /// </summary>
        /// <param name="raw">The raw data loaded from storage.</param>
        /// <param name="masterPool">The full set of available tags to resolve references.</param>
        /// <returns>A fully hydrated domain record.</returns>
        /// <exception cref="KeyNotFoundException">Thrown if a tag index in the raw data is missing from the master pool.</exception>
        public static ComputationRecord FromRaw(RawComputationRecord raw, IReadOnlyList<Tag> masterPool)
        {
            var tagLookup = masterPool.ToDictionary(t => t.Index);

            var restoredCombos = raw.WinningLoadout.Select(rc =>
            {
                var tags = rc.TagIds.Select(id => tagLookup[id]);
                // Re-calculates internal state via domain constructor to ensure data consistency.
                return new Combo(tags, rc.Score);
            }).ToList();

            return new ComputationRecord
            {
                ConfigurationHash = raw.ConfigurationHash,
                LoadoutSize = raw.LoadoutSize,
                ComboSize = raw.ComboSize,
                IsDisjoint = raw.IsDisjoint,
                UniverseMaskData = (ulong[])raw.UniverseMaskData.Clone(),
                WinningLoadout = restoredCombos,
                ComputationDate = DateTime.Parse(raw.ComputationDate),
                BestComputationTime = TimeSpan.FromMilliseconds(raw.BestComputationTimeMs),
                ValidationCount = raw.ValidationCount
            };
        }




    }
}