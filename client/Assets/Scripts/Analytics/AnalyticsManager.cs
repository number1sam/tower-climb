using System.Collections.Generic;
using UnityEngine;
using TowerClimb.Core;

namespace TowerClimb.Analytics
{
    /// <summary>
    /// Analytics integration wrapper for tracking game events
    /// Supports GameAnalytics, Firebase, Unity Analytics, or custom backend
    /// </summary>
    public class AnalyticsManager : MonoBehaviour
    {
        public static AnalyticsManager Instance { get; private set; }

        [Header("Configuration")]
        public bool enableAnalytics = true;
        public bool logToConsole = true;

        [Header("Integration")]
        public AnalyticsProvider provider = AnalyticsProvider.None;

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
            InitializeProvider();
        }

        private void InitializeProvider()
        {
            if (!enableAnalytics) return;

            switch (provider)
            {
                case AnalyticsProvider.GameAnalytics:
                    // GameAnalytics.Initialize();
                    Log("[Analytics] GameAnalytics initialized");
                    break;

                case AnalyticsProvider.Firebase:
                    // Firebase.Analytics.FirebaseAnalytics.SetAnalyticsCollectionEnabled(true);
                    Log("[Analytics] Firebase Analytics initialized");
                    break;

                case AnalyticsProvider.Unity:
                    // Unity Analytics initialization (deprecated in newer Unity versions)
                    Log("[Analytics] Unity Analytics initialized");
                    break;

                case AnalyticsProvider.Custom:
                    Log("[Analytics] Custom analytics initialized");
                    break;

                case AnalyticsProvider.None:
                    Log("[Analytics] Analytics disabled");
                    break;
            }
        }

        #region Core Events

        /// <summary>
        /// Track run start event
        /// </summary>
        public void TrackRunStart(int weekId, long seed)
        {
            if (!enableAnalytics) return;

            var parameters = new Dictionary<string, object>
            {
                { "week_id", weekId },
                { "seed", seed },
                { "timestamp", System.DateTimeOffset.Now.ToUnixTimeSeconds() }
            };

            TrackEvent("run_start", parameters);
        }

        /// <summary>
        /// Track run end with performance metrics
        /// </summary>
        public void TrackRunEnd(int finalFloor, int perfectCount, int failCount, float runtime, float avgReactionMs)
        {
            if (!enableAnalytics) return;

            var parameters = new Dictionary<string, object>
            {
                { "final_floor", finalFloor },
                { "perfect_count", perfectCount },
                { "fail_count", failCount },
                { "runtime_seconds", (int)runtime },
                { "avg_reaction_ms", (int)avgReactionMs },
                { "success_rate", perfectCount / (float)(perfectCount + failCount) }
            };

            TrackEvent("run_end", parameters);
        }

        /// <summary>
        /// Track pattern completion
        /// </summary>
        public void TrackPatternCompleted(PatternType patternType, bool success, float accuracy, int reactionMs, int floor)
        {
            if (!enableAnalytics) return;

            var parameters = new Dictionary<string, object>
            {
                { "pattern_type", patternType.ToString() },
                { "success", success },
                { "accuracy", accuracy },
                { "reaction_ms", reactionMs },
                { "floor", floor }
            };

            TrackEvent("pattern_completed", parameters);
        }

        /// <summary>
        /// Track milestone floor reached
        /// </summary>
        public void TrackMilestoneReached(int floor)
        {
            if (!enableAnalytics) return;

            var parameters = new Dictionary<string, object>
            {
                { "floor", floor }
            };

            TrackEvent("milestone_reached", parameters);
        }

        #endregion

        #region Progression Events

        /// <summary>
        /// Track shop item unlock
        /// </summary>
        public void TrackItemUnlocked(string itemId, string category, int unlockFloor)
        {
            if (!enableAnalytics) return;

            var parameters = new Dictionary<string, object>
            {
                { "item_id", itemId },
                { "category", category },
                { "unlock_floor", unlockFloor }
            };

            TrackEvent("item_unlocked", parameters);
        }

        /// <summary>
        /// Track cosmetic equipped
        /// </summary>
        public void TrackItemEquipped(string itemId, string category)
        {
            if (!enableAnalytics) return;

            var parameters = new Dictionary<string, object>
            {
                { "item_id", itemId },
                { "category", category }
            };

            TrackEvent("item_equipped", parameters);
        }

        /// <summary>
        /// Track mission completion
        /// </summary>
        public void TrackMissionCompleted(string missionId, string reward)
        {
            if (!enableAnalytics) return;

            var parameters = new Dictionary<string, object>
            {
                { "mission_id", missionId },
                { "reward", reward }
            };

            TrackEvent("mission_completed", parameters);
        }

        #endregion

        #region Practice Mode

        /// <summary>
        /// Track practice session start
        /// </summary>
        public void TrackPracticeStart(PatternType patternType, float speed)
        {
            if (!enableAnalytics) return;

            var parameters = new Dictionary<string, object>
            {
                { "pattern_type", patternType.ToString() },
                { "speed", speed }
            };

            TrackEvent("practice_start", parameters);
        }

        /// <summary>
        /// Track practice session end
        /// </summary>
        public void TrackPracticeEnd(PatternType patternType, int attempts, float successRate, float avgReactionMs)
        {
            if (!enableAnalytics) return;

            var parameters = new Dictionary<string, object>
            {
                { "pattern_type", patternType.ToString() },
                { "attempts", attempts },
                { "success_rate", successRate },
                { "avg_reaction_ms", (int)avgReactionMs }
            };

            TrackEvent("practice_end", parameters);
        }

        #endregion

        #region Leaderboard

        /// <summary>
        /// Track leaderboard view
        /// </summary>
        public void TrackLeaderboardViewed(string scope)
        {
            if (!enableAnalytics) return;

            var parameters = new Dictionary<string, object>
            {
                { "scope", scope }
            };

            TrackEvent("leaderboard_viewed", parameters);
        }

        /// <summary>
        /// Track new personal best
        /// </summary>
        public void TrackPersonalBest(int newBestFloor, int previousBest)
        {
            if (!enableAnalytics) return;

            var parameters = new Dictionary<string, object>
            {
                { "new_best", newBestFloor },
                { "previous_best", previousBest },
                { "improvement", newBestFloor - previousBest }
            };

            TrackEvent("personal_best", parameters);
        }

        #endregion

        #region Screen Navigation

        /// <summary>
        /// Track screen views
        /// </summary>
        public void TrackScreenView(string screenName)
        {
            if (!enableAnalytics) return;

            var parameters = new Dictionary<string, object>
            {
                { "screen_name", screenName }
            };

            TrackEvent("screen_view", parameters);
        }

        #endregion

        #region Generic Event Tracking

        /// <summary>
        /// Generic event tracking with custom parameters
        /// </summary>
        public void TrackEvent(string eventName, Dictionary<string, object> parameters = null)
        {
            if (!enableAnalytics) return;

            Log($"[Analytics] Event: {eventName} | Params: {FormatParameters(parameters)}");

            switch (provider)
            {
                case AnalyticsProvider.GameAnalytics:
                    TrackGameAnalytics(eventName, parameters);
                    break;

                case AnalyticsProvider.Firebase:
                    TrackFirebase(eventName, parameters);
                    break;

                case AnalyticsProvider.Unity:
                    TrackUnityAnalytics(eventName, parameters);
                    break;

                case AnalyticsProvider.Custom:
                    TrackCustom(eventName, parameters);
                    break;
            }
        }

        #endregion

        #region Provider-Specific Implementations

        private void TrackGameAnalytics(string eventName, Dictionary<string, object> parameters)
        {
            // GameAnalytics implementation
            // GameAnalytics.NewDesignEvent(eventName, parameters);
        }

        private void TrackFirebase(string eventName, Dictionary<string, object> parameters)
        {
            // Firebase implementation
            /*
            var firebaseParams = new Firebase.Analytics.Parameter[parameters?.Count ?? 0];
            if (parameters != null)
            {
                int i = 0;
                foreach (var kvp in parameters)
                {
                    firebaseParams[i++] = new Firebase.Analytics.Parameter(kvp.Key, kvp.Value.ToString());
                }
            }
            Firebase.Analytics.FirebaseAnalytics.LogEvent(eventName, firebaseParams);
            */
        }

        private void TrackUnityAnalytics(string eventName, Dictionary<string, object> parameters)
        {
            // Unity Analytics implementation (deprecated in newer Unity versions)
            // UnityEngine.Analytics.Analytics.CustomEvent(eventName, parameters);
        }

        private void TrackCustom(string eventName, Dictionary<string, object> parameters)
        {
            // Custom backend implementation
            // Could send to your own analytics endpoint via HTTP
            // StartCoroutine(SendToCustomBackend(eventName, parameters));
        }

        #endregion

        #region Helpers

        private string FormatParameters(Dictionary<string, object> parameters)
        {
            if (parameters == null || parameters.Count == 0)
                return "{}";

            var parts = new List<string>();
            foreach (var kvp in parameters)
            {
                parts.Add($"{kvp.Key}={kvp.Value}");
            }

            return "{ " + string.Join(", ", parts) + " }";
        }

        private void Log(string message)
        {
            if (logToConsole)
            {
                Debug.Log(message);
            }
        }

        #endregion
    }

    public enum AnalyticsProvider
    {
        None,
        GameAnalytics,
        Firebase,
        Unity,
        Custom
    }
}
