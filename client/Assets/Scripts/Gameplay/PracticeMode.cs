using System.Collections.Generic;
using UnityEngine;
using TowerClimb.Core;

namespace TowerClimb.Gameplay
{
    /// <summary>
    /// Practice Mode - Drill specific patterns at chosen speed (no leaderboard impact)
    /// </summary>
    public class PracticeMode : MonoBehaviour
    {
        public static PracticeMode Instance { get; private set; }

        [Header("Practice Settings")]
        public PatternType selectedPattern = PatternType.Tap;
        public float selectedSpeed = 1.0f;
        public bool endlessMode = true;

        [Header("Stats")]
        public int totalAttempts = 0;
        public int successCount = 0;
        public int perfectCount = 0;
        public List<int> reactionTimes = new List<int>();

        private GameStateMachine gameStateMachine;
        private DifficultyConfig practiceConfig;
        private bool isPracticing = false;

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
        }

        private void Start()
        {
            gameStateMachine = GameStateMachine.Instance;

            if (gameStateMachine != null)
            {
                gameStateMachine.OnPatternCompleted += HandlePatternCompleted;
            }
        }

        private void OnDestroy()
        {
            if (gameStateMachine != null)
            {
                gameStateMachine.OnPatternCompleted -= HandlePatternCompleted;
            }
        }

        /// <summary>
        /// Start practice session with specific pattern and speed
        /// </summary>
        public void StartPractice(PatternType pattern, float speed)
        {
            selectedPattern = pattern;
            selectedSpeed = speed;
            isPracticing = true;

            // Reset stats
            totalAttempts = 0;
            successCount = 0;
            perfectCount = 0;
            reactionTimes.Clear();

            // Create custom config for practice
            practiceConfig = new DifficultyConfig
            {
                v0 = speed,
                deltaV = 0, // No speed increase in practice
                minWindow = 0.3f,
                maxWindow = 2.0f,
                baseWindow = 1.5f,
                adaptiveEpsilon = 0,
                baseWeights = new PatternWeights()
            };

            // Set weight to 1.0 for selected pattern, 0 for others
            SetPatternWeight(selectedPattern, 1.0f);

            // Initialize game with practice seed
            long practiceSeed = System.DateTimeOffset.Now.ToUnixTimeMilliseconds();
            gameStateMachine.difficultyConfig = practiceConfig;
            gameStateMachine.InitializeRun(practiceSeed, -1); // -1 indicates practice mode

            Debug.Log($"[PracticeMode] Started practice: {pattern} at speed {speed}x");
        }

        private void SetPatternWeight(PatternType pattern, float weight)
        {
            // Set selected pattern to 1.0, all others to 0
            practiceConfig.baseWeights.tap = (pattern == PatternType.Tap) ? weight : 0f;
            practiceConfig.baseWeights.swipe = (pattern == PatternType.Swipe) ? weight : 0f;
            practiceConfig.baseWeights.hold = (pattern == PatternType.Hold) ? weight : 0f;
            practiceConfig.baseWeights.rhythm = (pattern == PatternType.Rhythm) ? weight : 0f;
            practiceConfig.baseWeights.tilt = (pattern == PatternType.Tilt) ? weight : 0f;
            practiceConfig.baseWeights.doubleTap = (pattern == PatternType.DoubleTap) ? weight : 0f;
        }

        /// <summary>
        /// End practice session and restore normal mode
        /// </summary>
        public void EndPractice()
        {
            if (!isPracticing) return;

            isPracticing = false;

            // Restore default config
            gameStateMachine.difficultyConfig = DifficultyConfig.Default;

            Debug.Log($"[PracticeMode] Ended practice: {totalAttempts} attempts, {successCount} success, {perfectCount} perfect");
            Debug.Log($"[PracticeMode] Avg reaction: {GetAverageReaction()}ms");
        }

        private void HandlePatternCompleted(PatternResult result)
        {
            if (!isPracticing) return;

            totalAttempts++;
            reactionTimes.Add(result.reactionMs);

            if (result.success)
            {
                successCount++;

                if (result.accuracy >= 0.95f)
                {
                    perfectCount++;
                }
            }

            // In endless mode, continue generating patterns
            if (endlessMode && result.success)
            {
                // Game will automatically advance to next floor
            }
            else if (!endlessMode && !result.success)
            {
                // In non-endless mode, stop on first failure
                EndPractice();
            }
        }

        public float GetSuccessRate()
        {
            if (totalAttempts == 0) return 0f;
            return (float)successCount / totalAttempts;
        }

        public float GetPerfectRate()
        {
            if (totalAttempts == 0) return 0f;
            return (float)perfectCount / totalAttempts;
        }

        public int GetAverageReaction()
        {
            if (reactionTimes.Count == 0) return 0;

            int sum = 0;
            foreach (int time in reactionTimes)
            {
                sum += time;
            }

            return sum / reactionTimes.Count;
        }

        public bool IsPracticing()
        {
            return isPracticing;
        }
    }
}
