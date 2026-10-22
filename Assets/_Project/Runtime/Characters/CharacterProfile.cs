using System;
using System.Collections.Generic;
using UnityEngine;

namespace AutismVR.Characters
{
    [Serializable]
    public struct TriggerSensitivity
    {
        public string TriggerId;
        public float Multiplier;
    }

    // One child's personality and trigger baseline (spec §4.1). Starting values are provisional.
    [CreateAssetMenu(menuName = "AutismVR/Character Profile")]
    public class CharacterProfile : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public GameObject ModelPrefab;
        public AudioClip GreetingClip;
        public string GreetingLine;
        [TextArea(3, 8)] public string Personality;
        [Range(0f, 1f)] public float StartingComfort;
        [Range(0f, 1f)] public float GreetingThreshold;
        [Min(0f)] public float RecoveryPerSecond;
        public List<TriggerSensitivity> TriggerSensitivities = new List<TriggerSensitivity>();
        public List<string> ExcludedTriggers = new List<string>();

        public bool Applies(string triggerId) => !ExcludedTriggers.Contains(triggerId);

        // Scales how strongly a trigger lowers this child's comfort: 1 when unlisted, 0 when excluded.
        public float GetSensitivity(string triggerId)
        {
            if (!Applies(triggerId))
                return 0f;
            foreach (var sensitivity in TriggerSensitivities)
                if (sensitivity.TriggerId == triggerId)
                    return sensitivity.Multiplier;
            return 1f;
        }
    }
}
