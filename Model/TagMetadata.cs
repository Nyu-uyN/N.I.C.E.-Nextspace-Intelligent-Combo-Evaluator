using System.Collections.Generic;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Centralized repository for tag-related metadata, including names, descriptions, 
    /// and bitmasks for global attributes.
    /// </summary>
    public static class TagMetadata
    {
        private static readonly Dictionary<int, string> _names = new();
        private static readonly Dictionary<int, string> _descriptions = new();

        public static IReadOnlyDictionary<int, string> Names => _names;
        public static IReadOnlyDictionary<int, string> Descriptions => _descriptions;

        /// <summary>
        /// Bitmask identifying tags flagged as controversial.
        /// </summary>
        public static TagMask ControversialMask { get; private set; } = TagMask.Empty;

        /// <summary>
        /// Bitmask identifying tags belonging to story missions.
        /// </summary>
        public static TagMask StoryMissionMask { get; private set; } = TagMask.Empty;

        /// <summary>
        /// Clears all existing metadata and reinitializes masks.
        /// Required before reloading data to prevent duplication.
        /// </summary>
        public static void Reset()
        {
            _names.Clear();
            _descriptions.Clear();

            // Re-instantiate masks to ensure a clean state
            ControversialMask = TagMask.Empty;
            StoryMissionMask = TagMask.Empty;
        }

        /// <summary>
        /// Populates metadata for a specific tag index and updates global attribute masks.
        /// </summary>
        public static void Add(int index, string name, string description, bool isControversial, bool isStoryMission)
        {
            _names[index] = name;
            _descriptions[index] = description;

            if (isControversial)
            {
                var mask = ControversialMask;
                mask.SetBit(index);
                ControversialMask = mask; 
            }
            if (isStoryMission)
            {
                var mask = StoryMissionMask;
                mask.SetBit(index);
                StoryMissionMask = mask; 
            }
        }
    }
}
