using System;
using System.Collections.Generic;
using TowerClimb.Utils;

namespace TowerClimb.Core
{
    /// <summary>
    /// Deterministic pattern generator for client-side gameplay
    /// CRITICAL: Must produce identical results to server for anti-cheat validation
    /// </summary>
    public class PatternGenerator
    {
        private static readonly PatternType[] ALL_PATTERN_TYPES = new PatternType[]
        {
            PatternType.Tap,
            PatternType.Swipe,
            PatternType.Hold,
            PatternType.Rhythm,
            PatternType.Tilt,
            PatternType.DoubleTap
        };

        private static readonly Direction[] SWIPE_DIRECTIONS = new Direction[]
        {
            Direction.Left,
            Direction.Right,
            Direction.Up,
            Direction.Down
        };

        private DifficultyConfig config;

        public PatternGenerator(DifficultyConfig config)
        {
            this.config = config;
        }

        /// <summary>
        /// Main generation function - deterministic based on seed and floor
        /// </summary>
        public Pattern Generate(long seed, int floor, PlayerModel playerModel = null)
        {
            // Mix seed with floor for unique RNG state per floor
            long floorSeed = seed ^ ((long)floor * 0x9e3779b9L);
            var rng = new SeededRandom(floorSeed);

            // Calculate pattern weights (adapt based on player weaknesses)
            float[] weights = CalculateWeights(playerModel);

            // Choose pattern type
            PatternType patternType = WeightedChoice.Choose(ALL_PATTERN_TYPES, weights, rng);

            // Calculate speed and time window
            float speed = CalculateSpeed(floor, playerModel);
            float timeWindow = CalculateTimeWindow(speed);

            // Build pattern
            Pattern pattern = new Pattern
            {
                type = patternType,
                direction = Direction.None,
                duration = 0f,
                complexity = 1,
                timeWindow = timeWindow,
                speed = speed
            };

            // Set type-specific properties
            switch (patternType)
            {
                case PatternType.Swipe:
                    pattern.direction = SWIPE_DIRECTIONS[rng.NextInt(4)];
                    break;

                case PatternType.Hold:
                    // Hold duration: 0.5s to 1.5s based on floor
                    pattern.duration = 0.5f + Math.Min(floor / 50f, 1.0f);
                    break;

                case PatternType.Rhythm:
                    // Complexity: 2-4 taps, increases with floor
                    pattern.complexity = 2 + Math.Min((int)Math.Floor(floor / 15f), 2);
                    break;

                case PatternType.Tilt:
                    // Tilt direction
                    pattern.direction = SWIPE_DIRECTIONS[rng.NextInt(4)];
                    break;

                case PatternType.DoubleTap:
                    pattern.complexity = 2;
                    break;

                case PatternType.Tap:
                default:
                    // Simple tap - no extra properties
                    break;
            }

            // Apply cooldown pattern every N floors to prevent fatigue deaths
            if (IsCooldownFloor(floor))
            {
                pattern.type = PatternType.Tap;
                pattern.direction = Direction.None;
                pattern.timeWindow = config.maxWindow; // generous window
            }

            return pattern;
        }

        /// <summary>
        /// Calculate pattern weights with player weakness bias
        /// </summary>
        private float[] CalculateWeights(PlayerModel playerModel)
        {
            PatternWeights baseWeights = config.baseWeights;
            float[] weights = new float[ALL_PATTERN_TYPES.Length];

            for (int i = 0; i < ALL_PATTERN_TYPES.Length; i++)
            {
                PatternType type = ALL_PATTERN_TYPES[i];
                float weight = baseWeights.GetWeight(type);

                // Increase weight for patterns player struggles with
                if (playerModel != null && playerModel.weaknesses.ContainsKey(type))
                {
                    float weakness = playerModel.weaknesses[type];
                    // weakness is 0-1, where 1 = many mistakes
                    // Increase spawn rate by up to 50% for weak patterns
                    weight *= 1f + weakness * 0.5f;
                }

                weights[i] = weight;
            }

            return weights;
        }

        /// <summary>
        /// Calculate speed based on floor and recent player performance
        /// </summary>
        private float CalculateSpeed(int floor, PlayerModel playerModel)
        {
            float v0 = config.v0;
            float deltaV = config.deltaV;
            float adaptiveEpsilon = config.adaptiveEpsilon;

            // Base speed progression
            float speed = v0 + floor * deltaV;

            // Adaptive difficulty boost for skilled players
            if (playerModel != null && playerModel.last5 != null && playerModel.last5.Count >= 3)
            {
                var recent = playerModel.last5;
                int count = Math.Min(recent.Count, 5);

                float totalAccuracy = 0f;
                float totalReaction = 0f;

                for (int i = Math.Max(0, recent.Count - 5); i < recent.Count; i++)
                {
                    totalAccuracy += recent[i].accuracy;
                    totalReaction += recent[i].reactionMs;
                }

                float avgAccuracy = totalAccuracy / count;
                float avgReaction = totalReaction / count;

                // If player is performing well (>80% accuracy, <400ms reaction), increase speed
                if (avgAccuracy > 0.8f && avgReaction < 400f)
                {
                    speed += adaptiveEpsilon;
                }
            }

            return speed;
        }

        /// <summary>
        /// Calculate time window based on speed
        /// </summary>
        private float CalculateTimeWindow(float speed)
        {
            float baseWindow = config.baseWindow;
            float minWindow = config.minWindow;
            float maxWindow = config.maxWindow;

            float window = baseWindow / speed;
            return Math.Max(minWindow, Math.Min(maxWindow, window));
        }

        /// <summary>
        /// Check if this floor should be a cooldown (easier pattern)
        /// </summary>
        private bool IsCooldownFloor(int floor)
        {
            // Every 20 floors, give a break
            return floor > 0 && floor % 20 == 0;
        }

        /// <summary>
        /// Pre-generate patterns for multiple floors (for smooth gameplay)
        /// </summary>
        public static List<Pattern> GenerateSequence(long seed, int startFloor, int count, DifficultyConfig config, PlayerModel playerModel = null)
        {
            var generator = new PatternGenerator(config);
            var patterns = new List<Pattern>(count);

            for (int i = 0; i < count; i++)
            {
                int floor = startFloor + i;
                patterns.Add(generator.Generate(seed, floor, playerModel));
            }

            return patterns;
        }
    }
}
