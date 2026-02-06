using System;
using System.Collections.Generic;
using System.Text;

namespace N.I.C.E.___Nextspace_Intelligent_Combo_Evaluator.Model
{
    public static class TagMetadata
    {
        private static readonly Dictionary<int, string> _names = new();
        private static readonly Dictionary<int, string> _descriptions = new();

        public static IReadOnlyDictionary<int, string> Names => _names;
        public static IReadOnlyDictionary<int, string> Descriptions => _descriptions;

        public static readonly TagMask ControversialMask = TagMask.Empty;
        public static readonly TagMask StoryMissionMask = TagMask.Empty;

        public static void Add(int index, string name, string description, bool isControversial, bool isStoryMission)
        {
            _names[index] = name;
            _descriptions[index] = description;

            if (isControversial) ControversialMask.SetBit(index);
            if (isStoryMission) StoryMissionMask.SetBit(index);
        }
    }
}
