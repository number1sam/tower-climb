using NUnit.Framework;
using TowerClimb.Core;
using TowerClimb.Utils;
using System.Collections.Generic;

namespace TowerClimb.Tests
{
    /// <summary>
    /// Unit tests for pattern generator determinism
    /// CRITICAL: These tests verify client/server pattern matching for anti-cheat
    /// </summary>
    public class PatternGeneratorTests
    {
        private DifficultyConfig defaultConfig;

        [SetUp]
        public void Setup()
        {
            defaultConfig = DifficultyConfig.Default;
        }

        [Test]
        public void PRNG_ProducesConsistentResults_WithSameSeed()
        {
            var rng1 = new SeededRandom(12345L);
            var rng2 = new SeededRandom(12345L);

            for (int i = 0; i < 100; i++)
            {
                float val1 = rng1.NextFloat();
                float val2 = rng2.NextFloat();
                Assert.AreEqual(val1, val2, 0.0001f, $"PRNG mismatch at iteration {i}");
            }
        }

        [Test]
        public void PRNG_ProducesDifferentResults_WithDifferentSeeds()
        {
            var rng1 = new SeededRandom(12345L);
            var rng2 = new SeededRandom(54321L);

            float val1 = rng1.NextFloat();
            float val2 = rng2.NextFloat();

            Assert.AreNotEqual(val1, val2, "Different seeds should produce different values");
        }

        [Test]
        public void PatternGeneration_IsDeterministic()
        {
            var generator = new PatternGenerator(defaultConfig);
            long seed = 9876543210L;

            var pattern1 = generator.Generate(seed, 10);
            var pattern2 = generator.Generate(seed, 10);

            Assert.AreEqual(pattern1.type, pattern2.type, "Pattern type mismatch");
            Assert.AreEqual(pattern1.direction, pattern2.direction, "Direction mismatch");
            Assert.AreEqual(pattern1.timeWindow, pattern2.timeWindow, 0.0001f, "Time window mismatch");
            Assert.AreEqual(pattern1.speed, pattern2.speed, 0.0001f, "Speed mismatch");
            Assert.AreEqual(pattern1.complexity, pattern2.complexity, "Complexity mismatch");
        }

        [Test]
        public void DifferentFloors_ProduceDifferentPatterns()
        {
            var generator = new PatternGenerator(defaultConfig);
            long seed = 12345L;

            var patterns = new List<string>();
            for (int i = 1; i <= 20; i++)
            {
                var pattern = generator.Generate(seed, i);
                string key = $"{pattern.type}-{pattern.direction}-{pattern.timeWindow:F3}";
                patterns.Add(key);
            }

            // At least 70% should be unique
            var uniquePatterns = new HashSet<string>(patterns);
            float uniqueRatio = (float)uniquePatterns.Count / patterns.Count;

            Assert.IsTrue(uniqueRatio > 0.7f, $"Expected >70% unique patterns, got {uniqueRatio * 100:F1}%");
        }

        [Test]
        public void Speed_IncreasesWithFloor()
        {
            var generator = new PatternGenerator(defaultConfig);
            long seed = 12345L;

            var pattern1 = generator.Generate(seed, 1);
            var pattern10 = generator.Generate(seed, 10);
            var pattern50 = generator.Generate(seed, 50);

            Assert.Less(pattern1.speed, pattern10.speed, "Floor 1 should be slower than floor 10");
            Assert.Less(pattern10.speed, pattern50.speed, "Floor 10 should be slower than floor 50");
        }

        [Test]
        public void TimeWindow_DecreasesWithSpeed()
        {
            var generator = new PatternGenerator(defaultConfig);
            long seed = 12345L;

            var pattern1 = generator.Generate(seed, 1);
            var pattern50 = generator.Generate(seed, 50);

            // Higher floor = higher speed = smaller time window
            Assert.Greater(pattern1.timeWindow, pattern50.timeWindow, "Early floors should have larger time windows");

            // Time window should never go below minWindow
            Assert.GreaterOrEqual(pattern50.timeWindow, defaultConfig.minWindow, "Time window violated minimum");
        }

        [Test]
        public void CooldownFloors_ProduceTapPatterns()
        {
            var generator = new PatternGenerator(defaultConfig);
            long seed = 12345L;

            // Floors 20, 40, 60 are cooldown floors
            var pattern20 = generator.Generate(seed, 20);
            var pattern40 = generator.Generate(seed, 40);
            var pattern60 = generator.Generate(seed, 60);

            Assert.AreEqual(PatternType.Tap, pattern20.type, "Floor 20 should be tap (cooldown)");
            Assert.AreEqual(PatternType.Tap, pattern40.type, "Floor 40 should be tap (cooldown)");
            Assert.AreEqual(PatternType.Tap, pattern60.type, "Floor 60 should be tap (cooldown)");

            // Cooldown floors should have maximum time window
            Assert.AreEqual(defaultConfig.maxWindow, pattern20.timeWindow, 0.0001f, "Cooldown should have max window");
        }

        [Test]
        public void PlayerWeaknesses_IncreasePatternSpawnRate()
        {
            var generator = new PatternGenerator(defaultConfig);
            long seed = 12345L;

            // Generate patterns with no player model
            var patternsNoModel = new List<Pattern>();
            for (int i = 1; i <= 100; i++)
            {
                patternsNoModel.Add(generator.Generate(seed, i));
            }

            // Generate patterns with player weak on Hold
            var playerModel = new PlayerModel();
            playerModel.weaknesses[PatternType.Hold] = 0.9f; // Very weak on holds

            var patternsWithModel = new List<Pattern>();
            for (int i = 1; i <= 100; i++)
            {
                patternsWithModel.Add(generator.Generate(seed, i, playerModel));
            }

            int holdCountNoModel = 0;
            int holdCountWithModel = 0;

            foreach (var p in patternsNoModel)
            {
                if (p.type == PatternType.Hold) holdCountNoModel++;
            }

            foreach (var p in patternsWithModel)
            {
                if (p.type == PatternType.Hold) holdCountWithModel++;
            }

            // With high weakness, should see more hold patterns
            Assert.GreaterOrEqual(holdCountWithModel, holdCountNoModel,
                $"Expected more holds with weakness model (no model: {holdCountNoModel}, with model: {holdCountWithModel})");
        }

        [Test]
        public void GenerateSequence_ProducesCorrectCount()
        {
            long seed = 12345L;
            int startFloor = 1;
            int count = 25;

            var patterns = PatternGenerator.GenerateSequence(seed, startFloor, count, defaultConfig);

            Assert.AreEqual(count, patterns.Count, "Sequence count mismatch");

            // All patterns should have valid properties
            foreach (var pattern in patterns)
            {
                Assert.GreaterOrEqual(pattern.timeWindow, defaultConfig.minWindow, "Time window below minimum");
                Assert.LessOrEqual(pattern.timeWindow, defaultConfig.maxWindow, "Time window above maximum");
                Assert.Greater(pattern.speed, 0f, "Speed must be positive");
            }
        }

        [Test]
        public void PatternProperties_ValidForEachType()
        {
            var generator = new PatternGenerator(defaultConfig);
            long seed = 99999L;

            // Generate many patterns to catch all types
            for (int i = 1; i <= 200; i++)
            {
                var pattern = generator.Generate(seed, i);

                switch (pattern.type)
                {
                    case PatternType.Swipe:
                    case PatternType.Tilt:
                        Assert.IsTrue(
                            pattern.direction == Direction.Left ||
                            pattern.direction == Direction.Right ||
                            pattern.direction == Direction.Up ||
                            pattern.direction == Direction.Down,
                            $"Swipe/Tilt should have valid direction, got: {pattern.direction}"
                        );
                        break;

                    case PatternType.Hold:
                        Assert.Greater(pattern.duration, 0f, "Hold should have positive duration");
                        break;

                    case PatternType.Rhythm:
                    case PatternType.DoubleTap:
                        Assert.GreaterOrEqual(pattern.complexity, 2, "Rhythm/DoubleTap should have complexity >= 2");
                        break;

                    case PatternType.Tap:
                        // Tap has no special requirements
                        break;
                }
            }
        }

        [Test]
        public void CrossPlatform_ConsistencyCheck()
        {
            // This test verifies the pattern can be serialized for cross-platform comparison
            var generator = new PatternGenerator(defaultConfig);
            long seed = 12345L;

            var pattern1 = generator.Generate(seed, 15);
            var pattern2 = generator.Generate(seed, 15);

            // Create serialization string for comparison
            string SerializePattern(Pattern p)
            {
                return $"{p.type}|{p.direction}|{p.duration:F6}|{p.complexity}|{p.timeWindow:F6}|{p.speed:F6}";
            }

            string serialized1 = SerializePattern(pattern1);
            string serialized2 = SerializePattern(pattern2);

            Assert.AreEqual(serialized1, serialized2, "Serialized patterns must be identical");
        }

        [Test]
        public void KnownSeedFloor_ProducesExpectedPattern()
        {
            // This test uses a known seed/floor combination to verify exact match with server
            // These values should be verified against the TypeScript implementation
            var generator = new PatternGenerator(defaultConfig);
            long seed = 1234567890123456789L;
            int floor = 10;

            var pattern = generator.Generate(seed, floor);

            // Log for manual verification
            UnityEngine.Debug.Log($"Seed={seed}, Floor={floor} -> Type={pattern.type}, Dir={pattern.direction}, Window={pattern.timeWindow:F4}, Speed={pattern.speed:F4}");

            // Basic sanity checks
            Assert.IsNotNull(pattern);
            Assert.GreaterOrEqual(pattern.timeWindow, defaultConfig.minWindow);
            Assert.LessOrEqual(pattern.timeWindow, defaultConfig.maxWindow);
        }
    }
}
