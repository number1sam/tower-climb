using System;
using System.Collections.Generic;
using UnityEngine;
using TowerClimb.Core;

namespace TowerClimb.Gameplay
{
    public enum GameState
    {
        Idle,           // Home screen
        PreRun,         // Countdown before run
        PlayingFloor,   // Active floor gameplay
        Success,        // Pattern completed successfully
        Failed,         // Pattern failed - run ended
        Results         // Post-run results screen
    }

    /// <summary>
    /// Central game state machine managing flow: Idle → PreRun → PlayFloor → (Success → NextFloor | Failed → Results) → Idle
    /// </summary>
    public class GameStateMachine : MonoBehaviour
    {
        public static GameStateMachine Instance { get; private set; }

        [Header("State")]
        public GameState currentState = GameState.Idle;

        [Header("Run Data")]
        public long currentSeed;
        public int currentFloor = 1;
        public int weekId;
        public float runStartTime;
        public List<PatternResult> runTimings = new List<PatternResult>();
        public PlayerModel playerModel = new PlayerModel();

        [Header("Pattern Queue")]
        public List<Pattern> preGeneratedPatterns = new List<Pattern>();
        public Pattern currentPattern;

        [Header("Configuration")]
        public DifficultyConfig difficultyConfig;

        // Events
        public event Action<GameState, GameState> OnStateChanged;
        public event Action<Pattern> OnNewPattern;
        public event Action<PatternResult> OnPatternCompleted;
        public event Action<int> OnFloorChanged;
        public event Action OnRunStarted;
        public event Action OnRunEnded;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else
            {
                Destroy(gameObject);
            }

            difficultyConfig = DifficultyConfig.Default;
        }

        /// <summary>
        /// Initialize a new run with seed from server
        /// </summary>
        public void InitializeRun(long seed, int week)
        {
            currentSeed = seed;
            weekId = week;
            currentFloor = 1;
            runStartTime = Time.time;
            runTimings.Clear();

            // Pre-generate 100 patterns for smooth gameplay (no network mid-run)
            preGeneratedPatterns = PatternGenerator.GenerateSequence(
                seed,
                1,
                100,
                difficultyConfig,
                playerModel
            );

            Debug.Log($"[GameStateMachine] Initialized run with seed={seed}, week={week}, patterns={preGeneratedPatterns.Count}");

            OnRunStarted?.Invoke();
            ChangeState(GameState.PreRun);

            // Auto-start first floor after short delay
            Invoke(nameof(StartFloor), 1.0f);
        }

        /// <summary>
        /// Start playing the current floor
        /// </summary>
        public void StartFloor()
        {
            if (currentFloor > preGeneratedPatterns.Count)
            {
                Debug.LogWarning($"[GameStateMachine] Floor {currentFloor} exceeds pre-generated patterns. Generating more...");
                var newPatterns = PatternGenerator.GenerateSequence(
                    currentSeed,
                    preGeneratedPatterns.Count + 1,
                    50,
                    difficultyConfig,
                    playerModel
                );
                preGeneratedPatterns.AddRange(newPatterns);
            }

            currentPattern = preGeneratedPatterns[currentFloor - 1];
            ChangeState(GameState.PlayingFloor);
            OnNewPattern?.Invoke(currentPattern);
            OnFloorChanged?.Invoke(currentFloor);

            Debug.Log($"[GameStateMachine] Floor {currentFloor}: {currentPattern}");
        }

        /// <summary>
        /// Called when player completes a pattern successfully
        /// </summary>
        public void PatternSuccess(int reactionMs, float accuracy)
        {
            var result = new PatternResult
            {
                floor = currentFloor,
                patternType = currentPattern.type,
                reactionMs = reactionMs,
                success = true,
                accuracy = accuracy,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            runTimings.Add(result);
            OnPatternCompleted?.Invoke(result);

            // Update player model for adaptive difficulty
            UpdatePlayerModel(result);

            ChangeState(GameState.Success);

            // Move to next floor
            currentFloor++;
            StartFloor();
        }

        /// <summary>
        /// Called when player fails a pattern
        /// </summary>
        public void PatternFailed(int reactionMs, float accuracy)
        {
            var result = new PatternResult
            {
                floor = currentFloor,
                patternType = currentPattern.type,
                reactionMs = reactionMs,
                success = false,
                accuracy = accuracy,
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            runTimings.Add(result);
            OnPatternCompleted?.Invoke(result);

            UpdatePlayerModel(result);

            ChangeState(GameState.Failed);
            EndRun();
        }

        /// <summary>
        /// End the current run and prepare results
        /// </summary>
        private void EndRun()
        {
            float runTime = Time.time - runStartTime;

            Debug.Log($"[GameStateMachine] Run ended. Floors: {currentFloor - 1}, Time: {runTime:F1}s, Timings: {runTimings.Count}");

            OnRunEnded?.Invoke();
            ChangeState(GameState.Results);
        }

        /// <summary>
        /// Return to idle state (home screen)
        /// </summary>
        public void ReturnToIdle()
        {
            ChangeState(GameState.Idle);
        }

        /// <summary>
        /// Update player model with latest performance data
        /// </summary>
        private void UpdatePlayerModel(PatternResult result)
        {
            // Track last 5 floors for adaptive difficulty
            var stats = new FloorStats
            {
                floor = result.floor,
                reactionMs = result.reactionMs,
                success = result.success,
                accuracy = result.accuracy
            };

            playerModel.last5.Add(stats);
            if (playerModel.last5.Count > 5)
            {
                playerModel.last5.RemoveAt(0);
            }

            // Track weaknesses per pattern type
            if (!playerModel.weaknesses.ContainsKey(result.patternType))
            {
                playerModel.weaknesses[result.patternType] = 0f;
            }

            // Update weakness score (moving average)
            float currentWeakness = playerModel.weaknesses[result.patternType];
            float failScore = result.success ? 0f : 1f;
            playerModel.weaknesses[result.patternType] = currentWeakness * 0.8f + failScore * 0.2f;
        }

        /// <summary>
        /// Change game state and notify listeners
        /// </summary>
        private void ChangeState(GameState newState)
        {
            if (currentState == newState) return;

            GameState oldState = currentState;
            currentState = newState;

            Debug.Log($"[GameStateMachine] State: {oldState} → {newState}");

            OnStateChanged?.Invoke(oldState, newState);
        }

        /// <summary>
        /// Get current run statistics
        /// </summary>
        public RunStats GetRunStats()
        {
            int totalTimings = runTimings.Count;
            if (totalTimings == 0)
            {
                return new RunStats();
            }

            int totalReaction = 0;
            int perfectCount = 0;
            int goodCount = 0;
            int missCount = 0;

            foreach (var timing in runTimings)
            {
                totalReaction += timing.reactionMs;

                if (timing.accuracy >= 0.95f)
                    perfectCount++;
                else if (timing.accuracy >= 0.7f)
                    goodCount++;
                else
                    missCount++;
            }

            int avgReaction = totalReaction / totalTimings;
            return new RunStats
            {
                floors = currentFloor - 1,
                runtimeSeconds = Time.time - runStartTime,
                avgReactionMs = avgReaction,
                averageReactionMs = avgReaction,
                perfectCount = perfectCount,
                goodCount = goodCount,
                missCount = missCount,
                failCount = missCount,
                perfectRate = (float)perfectCount / totalTimings
            };
        }
    }

    [Serializable]
    public class RunStats
    {
        public int floors;
        public float runtimeSeconds;
        public float averageReactionMs;
        public int avgReactionMs; // Deprecated: use averageReactionMs
        public int perfectCount;
        public int goodCount;
        public int missCount;
        public int failCount; // Same as missCount
        public float perfectRate;
    }
}
