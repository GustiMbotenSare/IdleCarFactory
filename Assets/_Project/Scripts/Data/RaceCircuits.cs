using System.Collections.Generic;

namespace CarFactoryIdle.Data
{
    public enum SegmentKind { Straight, Corner }

    /// <summary>A piece of track. Straights let cars run to top speed; corners cap speed based on
    /// the car's handling vs the corner's sharpness. The racer only tracks one "distance" number;
    /// the segment list is the "waypoint system" in a 1-D, deterministic form.</summary>
    public struct TrackSegment
    {
        public SegmentKind kind;
        public float length;     // meters
        public float sharpness;  // corners only, ~1 (gentle) .. 3 (hairpin)

        public TrackSegment(SegmentKind k, float len, float sharp)
        { kind = k; length = len; sharpness = sharp; }
    }

    public class CircuitDef
    {
        public string id;
        public string name;
        public int tier;
        public int laps;
        public long entryFee;
        public long cashReward;
        public int trophyReward;
        public int prestigeReward;
        public float aiStrength;            // AI stat budget vs the player's car (1 = matched)
        public List<TrackSegment> segments = new();

        public float LapDistance
        {
            get { float s = 0f; foreach (var seg in segments) s += seg.length; return s; }
        }

        /// <summary>Which segment a car is on, given how far into the current lap it is.</summary>
        public TrackSegment SegmentAt(float distanceIntoLap)
        {
            float d = distanceIntoLap;
            for (int i = 0; i < segments.Count; i++)
            {
                if (d < segments[i].length) return segments[i];
                d -= segments[i].length;
            }
            return segments.Count > 0 ? segments[segments.Count - 1]
                                      : new TrackSegment(SegmentKind.Straight, 1f, 0f);
        }
    }

    /// <summary>The three race tiers, authored in code (like Economy) so the skeleton needs no extra
    /// ScriptableObject assets. Tune freely. Higher tiers = longer, sharper, richer, tougher AI.</summary>
    public static class RaceCircuits
    {
        public static readonly List<CircuitDef> All = Build();

        public static CircuitDef Get(int index)
            => (index >= 0 && index < All.Count) ? All[index] : null;

        private static List<CircuitDef> Build()
        {
            return new List<CircuitDef>
            {
                new CircuitDef {
                    id = "local", name = "Local Circuit", tier = 1, laps = 2,
                    entryFee = 500, cashReward = 4000, trophyReward = 1, prestigeReward = 1,
                    aiStrength = 0.80f,
                    segments = new List<TrackSegment> {
                        new TrackSegment(SegmentKind.Straight, 320f, 0f),
                        new TrackSegment(SegmentKind.Corner,    90f, 1.2f),
                        new TrackSegment(SegmentKind.Straight, 200f, 0f),
                        new TrackSegment(SegmentKind.Corner,    70f, 1.8f),
                        new TrackSegment(SegmentKind.Straight, 160f, 0f),
                        new TrackSegment(SegmentKind.Corner,   100f, 1.0f),
                    }
                },
                new CircuitDef {
                    id = "national", name = "National Circuit", tier = 2, laps = 3,
                    entryFee = 4000, cashReward = 22000, trophyReward = 2, prestigeReward = 3,
                    aiStrength = 0.95f,
                    segments = new List<TrackSegment> {
                        new TrackSegment(SegmentKind.Straight, 420f, 0f),
                        new TrackSegment(SegmentKind.Corner,    80f, 2.0f),
                        new TrackSegment(SegmentKind.Straight, 260f, 0f),
                        new TrackSegment(SegmentKind.Corner,    60f, 2.6f),
                        new TrackSegment(SegmentKind.Straight, 300f, 0f),
                        new TrackSegment(SegmentKind.Corner,    90f, 1.6f),
                        new TrackSegment(SegmentKind.Straight, 180f, 0f),
                        new TrackSegment(SegmentKind.Corner,    75f, 2.2f),
                    }
                },
                new CircuitDef {
                    id = "international", name = "International Circuit", tier = 3, laps = 3,
                    entryFee = 20000, cashReward = 120000, trophyReward = 4, prestigeReward = 6,
                    aiStrength = 1.08f,
                    segments = new List<TrackSegment> {
                        new TrackSegment(SegmentKind.Straight, 600f, 0f),
                        new TrackSegment(SegmentKind.Corner,    70f, 2.8f),
                        new TrackSegment(SegmentKind.Straight, 320f, 0f),
                        new TrackSegment(SegmentKind.Corner,    55f, 3.0f),
                        new TrackSegment(SegmentKind.Straight, 380f, 0f),
                        new TrackSegment(SegmentKind.Corner,    80f, 2.4f),
                        new TrackSegment(SegmentKind.Straight, 240f, 0f),
                        new TrackSegment(SegmentKind.Corner,    65f, 2.6f),
                        new TrackSegment(SegmentKind.Straight, 200f, 0f),
                    }
                },
            };
        }
    }
}
