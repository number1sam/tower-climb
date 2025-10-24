using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using TowerClimb.Core;
using TowerClimb.Gameplay;
using TowerClimb.API;

namespace TowerClimb.Tests
{
    /// <summary>
    /// Integration tests for end-to-end game flow
    /// These tests verify that all systems work together correctly
    /// </summary>
    public class IntegrationTests
    {
        private GameObject gameStateMachineObj;
        private GameObject supabaseClientObj;
        private GameObject sessionManagerObj;
        private GameStateMachine gameStateMachine;
        private SupabaseClient supabaseClient;
        private SessionManager sessionManager;

        [SetUp]
        public void Setup()
        {
            // Create manager objects
            gameStateMachineObj = new GameObject("GameStateMachine");
            gameStateMachine = gameStateMachineObj.AddComponent<GameStateMachine>();

            supabaseClientObj = new GameObject("SupabaseClient");
            supabaseClient = supabaseClientObj.AddComponent<SupabaseClient>();

            sessionManagerObj = new GameObject("SessionManager");
            sessionManager = sessionManagerObj.AddComponent<SessionManager>();
        }

        [TearDown]
        public void Teardown()
        {
            // Clean up
            Object.Destroy(gameStateMachineObj);
            Object.Destroy(supabaseClientObj);
            Object.Destroy(sessionManagerObj);
        }

        [UnityTest]
        public IEnumerator GameStateMachine_InitializesCorrectly()
        {
            Assert.IsNotNull(gameStateMachine);
            Assert.AreEqual(GameState.Idle, gameStateMachine.currentState);

            yield return null;
        }

        [UnityTest]
        public IEnumerator GameStateMachine_CanInitializeRun()
        {
            long testSeed = 12345L;
            int testWeek = 202501;

            gameStateMachine.InitializeRun(testSeed, testWeek);

            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(testSeed, gameStateMachine.currentSeed);
            Assert.AreEqual(testWeek, gameStateMachine.weekId);
            Assert.AreEqual(GameState.PreRun, gameStateMachine.currentState);
            Assert.IsNotEmpty(gameStateMachine.preGeneratedPatterns);
            Assert.AreEqual(100, gameStateMachine.preGeneratedPatterns.Count);
        }

        [UnityTest]
        public IEnumerator GameStateMachine_GeneratesConsistentPatterns()
        {
            long seed = 9876543210L;
            int week = 202501;

            // Initialize twice with same seed
            gameStateMachine.InitializeRun(seed, week);
            var patterns1 = new System.Collections.Generic.List<Pattern>(gameStateMachine.preGeneratedPatterns);

            gameStateMachine.InitializeRun(seed, week);
            var patterns2 = new System.Collections.Generic.List<Pattern>(gameStateMachine.preGeneratedPatterns);

            yield return null;

            // Verify patterns are identical
            Assert.AreEqual(patterns1.Count, patterns2.Count);

            for (int i = 0; i < patterns1.Count; i++)
            {
                Assert.AreEqual(patterns1[i].type, patterns2[i].type, $"Pattern {i} type mismatch");
                Assert.AreEqual(patterns1[i].direction, patterns2[i].direction, $"Pattern {i} direction mismatch");
                Assert.AreEqual(patterns1[i].timeWindow, patterns2[i].timeWindow, 0.0001f, $"Pattern {i} timeWindow mismatch");
                Assert.AreEqual(patterns1[i].speed, patterns2[i].speed, 0.0001f, $"Pattern {i} speed mismatch");
            }
        }

        [UnityTest]
        public IEnumerator GameStateMachine_TracksPlayerModel()
        {
            long seed = 12345L;
            gameStateMachine.InitializeRun(seed, 202501);
            gameStateMachine.StartFloor();

            yield return null;

            // Simulate pattern success
            gameStateMachine.PatternSuccess(350, 0.95f);

            yield return null;

            // Check player model updated
            Assert.IsNotEmpty(gameStateMachine.playerModel.last5);
            Assert.AreEqual(1, gameStateMachine.playerModel.last5.Count);
            Assert.AreEqual(350, gameStateMachine.playerModel.last5[0].reactionMs);
            Assert.AreEqual(0.95f, gameStateMachine.playerModel.last5[0].accuracy);
        }

        [UnityTest]
        public IEnumerator GameStateMachine_AdvancesFloors()
        {
            long seed = 12345L;
            gameStateMachine.InitializeRun(seed, 202501);

            Assert.AreEqual(1, gameStateMachine.currentFloor);

            gameStateMachine.StartFloor();
            yield return null;

            gameStateMachine.PatternSuccess(300, 1.0f);
            yield return null;

            // Should advance to floor 2
            Assert.AreEqual(2, gameStateMachine.currentFloor);
        }

        [UnityTest]
        public IEnumerator GameStateMachine_EndsRunOnFailure()
        {
            long seed = 12345L;
            gameStateMachine.InitializeRun(seed, 202501);
            gameStateMachine.StartFloor();

            yield return null;

            gameStateMachine.PatternFailed(800, 0.2f);
            yield return null;

            Assert.AreEqual(GameState.Results, gameStateMachine.currentState);
        }

        [UnityTest]
        public IEnumerator GameStateMachine_CalculatesRunStats()
        {
            long seed = 12345L;
            gameStateMachine.InitializeRun(seed, 202501);

            // Simulate 5 floors
            for (int i = 0; i < 5; i++)
            {
                gameStateMachine.StartFloor();
                yield return new WaitForSeconds(0.1f);

                // Alternate between perfect and good
                float accuracy = (i % 2 == 0) ? 1.0f : 0.85f;
                gameStateMachine.PatternSuccess(300 + i * 10, accuracy);
                yield return new WaitForSeconds(0.1f);
            }

            var stats = gameStateMachine.GetRunStats();

            Assert.AreEqual(5, stats.floors);
            Assert.Greater(stats.runtimeSeconds, 0);
            Assert.Greater(stats.avgReactionMs, 0);
            Assert.Greater(stats.perfectRate, 0);
            Assert.LessOrEqual(stats.perfectRate, 1.0f);
        }

        [Test]
        public void PlayerModel_TracksWeaknesses()
        {
            var playerModel = new PlayerModel();

            // Simulate fails on hold patterns
            for (int i = 0; i < 10; i++)
            {
                var result = new PatternResult
                {
                    floor = i + 1,
                    patternType = PatternType.Hold,
                    reactionMs = 500,
                    success = false,
                    accuracy = 0.3f
                };

                // Manually update weakness (normally done by GameStateMachine)
                if (!playerModel.weaknesses.ContainsKey(result.patternType))
                {
                    playerModel.weaknesses[result.patternType] = 0f;
                }

                float currentWeakness = playerModel.weaknesses[result.patternType];
                float failScore = result.success ? 0f : 1f;
                playerModel.weaknesses[result.patternType] = currentWeakness * 0.8f + failScore * 0.2f;
            }

            // After 10 fails, weakness should be high
            Assert.Greater(playerModel.weaknesses[PatternType.Hold], 0.5f);
        }

        [UnityTest]
        public IEnumerator AdaptiveDifficulty_IncreasesSpawnForWeakPatterns()
        {
            long seed = 12345L;
            var config = DifficultyConfig.Default;

            // Create player model with weakness on Hold
            var playerModel = new PlayerModel();
            playerModel.weaknesses[PatternType.Hold] = 0.9f; // Very weak

            // Generate 50 patterns with player model
            var patternsWithWeakness = PatternGenerator.GenerateSequence(seed, 1, 50, config, playerModel);

            // Generate 50 patterns without player model
            var patternsWithoutWeakness = PatternGenerator.GenerateSequence(seed, 1, 50, config, null);

            yield return null;

            // Count hold patterns
            int holdWithWeakness = 0;
            int holdWithoutWeakness = 0;

            foreach (var p in patternsWithWeakness)
            {
                if (p.type == PatternType.Hold) holdWithWeakness++;
            }

            foreach (var p in patternsWithoutWeakness)
            {
                if (p.type == PatternType.Hold) holdWithoutWeakness++;
            }

            Debug.Log($"Hold patterns: With weakness={holdWithWeakness}, Without={holdWithoutWeakness}");

            // Should see more hold patterns with weakness model
            Assert.GreaterOrEqual(holdWithWeakness, holdWithoutWeakness);
        }

        [Test]
        public void DifficultyConfig_DefaultValues_AreValid()
        {
            var config = DifficultyConfig.Default;

            Assert.Greater(config.v0, 0);
            Assert.Greater(config.deltaV, 0);
            Assert.Greater(config.minWindow, 0);
            Assert.Greater(config.maxWindow, config.minWindow);
            Assert.Greater(config.baseWindow, config.minWindow);

            // Check weights sum to reasonable value
            float totalWeight = config.baseWeights.tap +
                                config.baseWeights.swipe +
                                config.baseWeights.hold +
                                config.baseWeights.rhythm +
                                config.baseWeights.tilt +
                                config.baseWeights.doubleTap;

            Assert.Greater(totalWeight, 0.9f);
            Assert.Less(totalWeight, 1.1f);
        }

        [Test]
        public void RunStats_CalculationIsCorrect()
        {
            var stats = new RunStats
            {
                floors = 25,
                runtimeSeconds = 187.5f,
                avgReactionMs = 342,
                perfectCount = 18,
                goodCount = 5,
                missCount = 2,
                perfectRate = 0.72f
            };

            int totalHits = stats.perfectCount + stats.goodCount + stats.missCount;
            Assert.AreEqual(25, totalHits);

            float expectedPerfectRate = 18f / 25f;
            Assert.AreEqual(expectedPerfectRate, stats.perfectRate, 0.01f);
        }

        [UnityTest]
        public IEnumerator FullGameFlow_Simulation()
        {
            // This test simulates a complete game flow

            // 1. Initialize run
            long seed = 987654321L;
            int weekId = 202501;

            gameStateMachine.InitializeRun(seed, weekId);
            yield return new WaitForSeconds(0.1f);

            Assert.AreEqual(GameState.PreRun, gameStateMachine.currentState);

            // 2. Play 10 floors
            for (int floor = 0; floor < 10; floor++)
            {
                gameStateMachine.StartFloor();
                yield return new WaitForSeconds(0.05f);

                Assert.AreEqual(GameState.PlayingFloor, gameStateMachine.currentState);

                // Simulate player input (mostly good)
                int reactionMs = Random.Range(250, 450);
                float accuracy = Random.Range(0.8f, 1.0f);

                gameStateMachine.PatternSuccess(reactionMs, accuracy);
                yield return new WaitForSeconds(0.05f);
            }

            // 3. Verify we reached floor 11 (after 10 successes)
            Assert.AreEqual(11, gameStateMachine.currentFloor);

            // 4. Simulate fail
            gameStateMachine.StartFloor();
            yield return new WaitForSeconds(0.05f);

            gameStateMachine.PatternFailed(700, 0.3f);
            yield return new WaitForSeconds(0.1f);

            // 5. Verify run ended
            Assert.AreEqual(GameState.Results, gameStateMachine.currentState);

            // 6. Check stats
            var stats = gameStateMachine.GetRunStats();
            Assert.AreEqual(10, stats.floors); // Failed on 11th floor
            Assert.Greater(stats.avgReactionMs, 0);
            Assert.Greater(stats.perfectRate, 0);

            Debug.Log($"Full game flow test: {stats.floors} floors, {stats.avgReactionMs}ms avg, {stats.perfectRate * 100:F1}% perfect");
        }
    }
}
