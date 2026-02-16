using System.Collections.Generic;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    /// <summary>
    /// Centralized repository for tag-related metadata, including names, descriptions, 
    /// and bitmasks for global attributes.
    /// </summary>
    public static class TagMetadata
    {
        private static readonly Dictionary<int, string> _baseNames = new();
        private static readonly Dictionary<int, string> _baseDescriptions = new();
        private static readonly Dictionary<int, string> _nameOverrides = new();
        private static readonly Dictionary<int, string> _descOverrides = new();

        

        /// <summary>
        /// Bitmask identifying tags flagged as controversial.
        /// </summary>
        private static TagMask _baseControversial = TagMask.Empty;
        public static TagMask ControversialMask { get; private set; } = TagMask.Empty;
        /// <summary>
        /// Bitmask identifying tags belonging to story missions.
        /// </summary>
        private static TagMask _baseStoryMission = TagMask.Empty;
        public static TagMask StoryMissionMask { get; private set; } = TagMask.Empty;
        

        /// <summary>
        /// Clears all existing metadata and reinitializes masks.
        /// Required before reloading data to prevent duplication.
        /// </summary>
        public static void Reset()
        {
            _baseNames.Clear();
            _baseDescriptions.Clear();
            _nameOverrides.Clear();
            _descOverrides.Clear();

            
            _baseControversial = TagMask.Empty;
            _baseStoryMission = TagMask.Empty;
            ControversialMask = TagMask.Empty;
            StoryMissionMask = TagMask.Empty;
        }

        public static void AddBaseData(int index, string name, string description, bool isControversial, bool isStoryMission)
        {
            _baseNames[index] = name;
            _baseDescriptions[index] = description;

            if (isControversial)
            {
                
                SetBitInMask(ref _baseControversial, index);
                var active = ControversialMask;
                active.SetBit(index);
                ControversialMask = active;
            }

            if (isStoryMission)
            {
                SetBitInMask(ref _baseStoryMission, index);
                var active = StoryMissionMask;
                active.SetBit(index);
                StoryMissionMask = active;
            }
        }
        public static void SetNameOverride(int index, string value)
        {
            _nameOverrides[index] = value;
        }

        public static void SetDescriptionOverride(int index, string value)
        {
            _descOverrides[index] = value;
        }
        public static void SetControversial(int index, bool isActive)
        {
            var mask = ControversialMask;
            if (isActive) mask.SetBit(index);
            else ClearBitInMask(ref mask, index);
            ControversialMask = mask;
        }

        public static void SetStoryMission(int index, bool isActive)
        {
            var mask = StoryMissionMask;
            if (isActive) mask.SetBit(index);
            else ClearBitInMask(ref mask, index);
            StoryMissionMask = mask;
        }
        public static string GetName(int index)
        {
            if (_nameOverrides.TryGetValue(index, out var val)) return val;
            return _baseNames.TryGetValue(index, out var baseVal) ? baseVal : $"Unknown_{index}";
        }

        public static string GetDescription(int index)
        {
            if (_descOverrides.TryGetValue(index, out var val)) return val;
            return _baseDescriptions.TryGetValue(index, out var baseVal) ? baseVal : string.Empty;
        }

        public static bool IsControversial(int index) => ControversialMask.IsSet(index);
        public static bool IsStoryMission(int index) => StoryMissionMask.IsSet(index);

        // --- Helpers Bitwise (Car on ne touche pas à TagMask struct) ---

        private static void SetBitInMask(ref TagMask mask, int index)
        {
            // Puisque TagMask est une struct, ref est important ici si on modifie une variable locale
            // Mais ta struct a déjà SetBit, donc on l'utilise
            mask.SetBit(index);
        }

        private static void ClearBitInMask(ref TagMask mask, int index)
        {
            // Manipulation directe des champs publics A-H pour faire un "AND NOT"
            if (index < 64) { mask.A &= ~(1UL << index); return; }
            if (index < 128) { mask.B &= ~(1UL << (index - 64)); return; }
            if (index < 192) { mask.C &= ~(1UL << (index - 128)); return; }
            if (index < 256) { mask.D &= ~(1UL << (index - 192)); return; }
            if (index < 320) { mask.E &= ~(1UL << (index - 256)); return; }
            if (index < 384) { mask.F &= ~(1UL << (index - 320)); return; }
            if (index < 448) { mask.G &= ~(1UL << (index - 384)); return; }
            if (index < 512) { mask.H &= ~(1UL << (index - 448)); return; }
        }
    }
}
