using System;
using System.Collections.Generic;

namespace TowerClimb.Core
{
    public enum PatternType
    {
        Tap,
        Swipe,
        Hold,
        Rhythm,
        Tilt,
        DoubleTap
    }

    public enum Direction
    {
        None,
        Left,
        Right,
        Up,
        Down
    }

    [Serializable]
    public class Pattern
    {
        public PatternType type;
        public Direction direction;
        public float duration;      // for hold patterns
        public int complexity;      // for rhythm patterns (number of taps)
        public float timeWindow;    // seconds allowed to complete
        public float speed;         // current difficulty multiplier

        public override string ToString()
        {
            return $"Pattern({type}, {direction}, window={timeWindow:F2}s, speed={speed:F2})";
        }
    }

    [Serializable]
    public class DifficultyConfig
    {
        public float v0;                    // base speed
        public float deltaV;                // speed increment per floor
        public float minWindow;             // minimum time window
        public float maxWindow;             // maximum time window
        public float baseWindow;            // base time window before speed adjustment
        public float adaptiveEpsilon;       // extra speed boost for skilled players
        public PatternWeights baseWeights;

        public static DifficultyConfig Default => new DifficultyConfig
        {
            v0 = 1.0f,
            deltaV = 0.05f,
            minWindow = 0.3f,
            maxWindow = 2.0f,
            baseWindow = 1.5f,
            adaptiveEpsilon = 0.1f,
            baseWeights = new PatternWeights
            {
                tap = 0.3f,
                swipe = 0.3f,
                hold = 0.2f,
                rhythm = 0.1f,
                tilt = 0.05f,
                doubleTap = 0.05f
            }
        };
    }

    [Serializable]
    public class PatternWeights
    {
        public float tap;
        public float swipe;
        public float hold;
        public float rhythm;
        public float tilt;
        public float doubleTap;

        public float GetWeight(PatternType type)
        {
            switch (type)
            {
                case PatternType.Tap: return tap;
                case PatternType.Swipe: return swipe;
                case PatternType.Hold: return hold;
                case PatternType.Rhythm: return rhythm;
                case PatternType.Tilt: return tilt;
                case PatternType.DoubleTap: return doubleTap;
                default: return 0.1f;
            }
        }
    }

    [Serializable]
    public class PlayerModel
    {
        public Dictionary<PatternType, float> weaknesses = new Dictionary<PatternType, float>();
        public List<FloorStats> last5 = new List<FloorStats>();
    }

    [Serializable]
    public class FloorStats
    {
        public int floor;
        public int reactionMs;
        public bool success;
        public float accuracy;      // 0-1, perfect = 1.0
    }

    [Serializable]
    public class PatternResult
    {
        public int floor;
        public PatternType patternType;
        public int reactionMs;
        public bool success;
        public float accuracy;
        public long timestamp;      // Unix timestamp in ms
    }

    [Serializable]
    public class RunSubmission
    {
        public int weekId;
        public int floors;
        public float runtimeSeconds;
        public int avgReactionMs;
        public Dictionary<PatternType, PatternBreakdown> breakdown;
        public List<PatternResult> timings;
        public PlayerModel playerModel;
        public string clientVersion;
    }

    [Serializable]
    public class PatternBreakdown
    {
        public int attempts;
        public int perfects;
        public int goods;
        public int misses;
        public int avgReactionMs;
    }

    [Serializable]
    public class SessionData
    {
        public string userId;
        public int weekId;
        public long seed;
        public string startsAt;
        public string endsAt;
        public int? currentBest;
    }

    [Serializable]
    public class LeaderboardEntry
    {
        public int rank;
        public string userId;
        public string handle;
        public int bestFloor;
        public int bestReactionMs;
        public float perfectRate;
        public string country;
    }
}
