using System;
using System.Collections.Generic;
using System.Linq;
using AutismVR.Characters;
using UnityEngine;

namespace AutismVR.Comfort
{
    // The editable list of triggers and calming behaviours (spec §5.4). It is JSON so the same rows can
    // come from Supabase later; the built-in copy is the offline default.
    public sealed class TriggerCatalog
    {
        public const string DefaultResourcePath = "Comfort/trigger-catalog";

        readonly List<TriggerDefinition> m_Entries;

        TriggerCatalog(List<TriggerDefinition> entries)
        {
            m_Entries = entries;
        }

        public IReadOnlyList<TriggerDefinition> Entries => m_Entries;

        public TriggerDefinition Find(string id) => m_Entries.FirstOrDefault(entry => entry.Id == id);

        public bool IsActive(string id) => Find(id) is { Enabled: true };

        public static TriggerCatalog LoadDefault()
        {
            var asset = Resources.Load<TextAsset>(DefaultResourcePath);
            if (asset == null || !TryParse(asset.text, out var catalog))
                throw new InvalidOperationException($"Built-in trigger catalog Resources/{DefaultResourcePath} is missing or invalid");
            return catalog;
        }

        // False when the JSON is unreadable or has no usable entries. Entries with an unknown kind, channel or
        // reaction, an effect of the wrong sign, or a repeated id, are skipped so one bad row does not discard
        // the rest. Channel parameters are checked by the detector that uses them.
        public static bool TryParse(string json, out TriggerCatalog catalog)
        {
            catalog = null;
            if (string.IsNullOrWhiteSpace(json))
                return false;

            CatalogData data;
            try
            {
                data = JsonUtility.FromJson<CatalogData>(json);
            }
            catch (ArgumentException)
            {
                return false;
            }

            var entries = new List<TriggerDefinition>();
            foreach (var row in data?.entries ?? Array.Empty<EntryData>())
                if (TryConvert(row, out var entry) && entries.All(e => e.Id != entry.Id))
                    entries.Add(entry);

            if (entries.Count == 0)
                return false;
            catalog = new TriggerCatalog(entries);
            return true;
        }

        public string ToJson()
        {
            var data = new CatalogData
            {
                version = 1,
                entries = m_Entries.Select(entry => new EntryData
                {
                    id = entry.Id,
                    name = entry.Name,
                    description = entry.Description,
                    kind = entry.Kind == TriggerKind.Calming ? "calming" : "trigger",
                    channels = ChannelNames.Where(pair => entry.Channels.HasFlag(pair.Value)).Select(pair => pair.Key).ToArray(),
                    effect = entry.Effect,
                    reaction = entry.Reaction?.ToString() ?? "",
                    enabled = entry.Enabled,
                    provisional = entry.Provisional,
                    threshold_dbfs = entry.ThresholdDbfs,
                    hold_seconds = entry.HoldSeconds,
                    refractory_seconds = entry.RefractorySeconds,
                    repeat_seconds = entry.RepeatSeconds,
                    cone_degrees = entry.ConeDegrees,
                    distance_m = entry.DistanceMeters,
                    rearm_distance_m = entry.RearmDistanceMeters,
                    speed_mps = entry.SpeedMetersPerSecond,
                    range_m = entry.RangeMeters,
                }).ToArray(),
            };
            return JsonUtility.ToJson(data, true);
        }

        static readonly Dictionary<string, Channel> ChannelNames = new Dictionary<string, Channel>
        {
            ["words"] = Channel.Words,
            ["loudness"] = Channel.Loudness,
            ["gaze"] = Channel.Gaze,
            ["proximity"] = Channel.Proximity,
            ["hands"] = Channel.Hands,
        };

        static bool TryConvert(EntryData row, out TriggerDefinition entry)
        {
            entry = null;
            if (row == null || string.IsNullOrEmpty(row.id))
                return false;

            TriggerKind kind;
            if (row.kind == "trigger") kind = TriggerKind.Trigger;
            else if (row.kind == "calming") kind = TriggerKind.Calming;
            else return false;

            var channels = Channel.None;
            foreach (var name in row.channels ?? Array.Empty<string>())
            {
                if (!ChannelNames.TryGetValue(name, out var channel))
                    return false;
                channels |= channel;
            }
            if (channels == Channel.None)
                return false;

            // A trigger must lower comfort and a calming behaviour raise it.
            if (kind == TriggerKind.Trigger ? row.effect >= 0f : row.effect <= 0f)
                return false;

            ChildReaction? reaction = null;
            if (!string.IsNullOrEmpty(row.reaction))
            {
                if (!Enum.TryParse(row.reaction, false, out ChildReaction parsed) || !Enum.IsDefined(typeof(ChildReaction), parsed))
                    return false;
                reaction = parsed;
            }

            entry = new TriggerDefinition
            {
                Id = row.id,
                Name = row.name ?? "",
                Description = row.description ?? "",
                Kind = kind,
                Channels = channels,
                Effect = row.effect,
                Reaction = reaction,
                Enabled = row.enabled,
                Provisional = row.provisional,
                ThresholdDbfs = row.threshold_dbfs,
                HoldSeconds = row.hold_seconds,
                RefractorySeconds = row.refractory_seconds,
                RepeatSeconds = row.repeat_seconds,
                ConeDegrees = row.cone_degrees,
                DistanceMeters = row.distance_m,
                RearmDistanceMeters = row.rearm_distance_m,
                SpeedMetersPerSecond = row.speed_mps,
                RangeMeters = row.range_m,
            };
            return true;
        }

        // JSON shape, snake_case to match the backend columns. Field names are the wire format.
        [Serializable]
        class CatalogData
        {
            public int version;
            public EntryData[] entries;
        }

        [Serializable]
        class EntryData
        {
            public string id;
            public string name;
            public string description;
            public string kind;
            public string[] channels;
            public float effect;
            public string reaction;
            public bool enabled;
            public bool provisional;
            public float threshold_dbfs;
            public float hold_seconds;
            public float refractory_seconds;
            public float repeat_seconds;
            public float cone_degrees;
            public float distance_m;
            public float rearm_distance_m;
            public float speed_mps;
            public float range_m;
        }
    }
}
