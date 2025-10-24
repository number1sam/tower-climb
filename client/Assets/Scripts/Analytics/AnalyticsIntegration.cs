using UnityEngine;
using TowerClimb.Core;
using TowerClimb.Gameplay;

namespace TowerClimb.Analytics
{
    /// <summary>
    /// Hooks up game events to analytics tracking
    /// Attach this to a persistent GameObject in your main scene
    /// </summary>
    public class AnalyticsIntegration : MonoBehaviour
    {
        private GameStateMachine gameStateMachine;
        private MissionsManager missionsManager;
        private PracticeMode practiceMode;
        private AnalyticsManager analytics;

        private void Start()
        {
            analytics = AnalyticsManager.Instance;
            gameStateMachine = GameStateMachine.Instance;
            missionsManager = MissionsManager.Instance;
            practiceMode = PracticeMode.Instance;

            SubscribeToEvents();
        }

        private void OnDestroy()
        {
            UnsubscribeFromEvents();
        }

        private void SubscribeToEvents()
        {
            if (gameStateMachine != null)
            {
                gameStateMachine.OnRunStarted += HandleRunStarted;
                gameStateMachine.OnRunEnded += HandleRunEnded;
                gameStateMachine.OnPatternCompleted += HandlePatternCompleted;
                gameStateMachine.OnStateChanged += HandleStateChanged;
            }

            if (missionsManager != null)
            {
                missionsManager.OnMissionCompleted += HandleMissionCompleted;
            }
        }

        private void UnsubscribeFromEvents()
        {
            if (gameStateMachine != null)
            {
                gameStateMachine.OnRunStarted -= HandleRunStarted;
                gameStateMachine.OnRunEnded -= HandleRunEnded;
                gameStateMachine.OnPatternCompleted -= HandlePatternCompleted;
                gameStateMachine.OnStateChanged -= HandleStateChanged;
            }

            if (missionsManager != null)
            {
                missionsManager.OnMissionCompleted -= HandleMissionCompleted;
            }
        }

        #region Event Handlers

        private void HandleRunStarted()
        {
            if (analytics == null || gameStateMachine == null) return;

            // Track run start
            analytics.TrackRunStart(gameStateMachine.weekId, gameStateMachine.currentSeed);
        }

        private void HandleRunEnded()
        {
            if (analytics == null || gameStateMachine == null) return;

            var stats = gameStateMachine.GetRunStats();

            // Track run end with full stats
            analytics.TrackRunEnd(
                stats.floors,
                stats.perfectCount,
                stats.failCount,
                stats.runtimeSeconds,
                stats.averageReactionMs
            );

            // Check for milestone floors (10, 25, 50, 100, etc.)
            if (IsMilestoneFloor(stats.floors))
            {
                analytics.TrackMilestoneReached(stats.floors);
            }

            // Check for personal best
            int previousBest = PlayerPrefs.GetInt("PersonalBest", 0);
            if (stats.floors > previousBest)
            {
                analytics.TrackPersonalBest(stats.floors, previousBest);
                PlayerPrefs.SetInt("PersonalBest", stats.floors);
            }
        }

        private void HandlePatternCompleted(PatternResult result)
        {
            if (analytics == null) return;

            // Track every pattern completion (can be filtered/sampled server-side if too many events)
            analytics.TrackPatternCompleted(
                result.patternType,
                result.success,
                result.accuracy,
                result.reactionMs,
                result.floor
            );
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (analytics == null) return;

            // Track screen views based on state
            string screenName = newState switch
            {
                GameState.MainMenu => "main_menu",
                GameState.PreRun => "pre_run",
                GameState.PlayingFloor => "gameplay",
                GameState.Results => "results",
                _ => null
            };

            if (screenName != null)
            {
                analytics.TrackScreenView(screenName);
            }
        }

        private void HandleMissionCompleted(Mission mission)
        {
            if (analytics == null) return;

            analytics.TrackMissionCompleted(mission.id, mission.reward);
        }

        #endregion

        #region Shop/Unlocks Integration

        /// <summary>
        /// Call this when player unlocks a shop item
        /// </summary>
        public void TrackItemUnlock(string itemId, string category, int unlockFloor)
        {
            if (analytics == null) return;
            analytics.TrackItemUnlocked(itemId, category, unlockFloor);
        }

        /// <summary>
        /// Call this when player equips a cosmetic
        /// </summary>
        public void TrackItemEquip(string itemId, string category)
        {
            if (analytics == null) return;
            analytics.TrackItemEquipped(itemId, category);
        }

        #endregion

        #region Practice Mode Integration

        /// <summary>
        /// Call this when practice session starts
        /// </summary>
        public void TrackPracticeSessionStart(PatternType patternType, float speed)
        {
            if (analytics == null) return;
            analytics.TrackPracticeStart(patternType, speed);
        }

        /// <summary>
        /// Call this when practice session ends
        /// </summary>
        public void TrackPracticeSessionEnd(PatternType patternType, int attempts, float successRate, float avgReactionMs)
        {
            if (analytics == null) return;
            analytics.TrackPracticeEnd(patternType, attempts, successRate, avgReactionMs);
        }

        #endregion

        #region Leaderboard Integration

        /// <summary>
        /// Call this when player views leaderboard
        /// </summary>
        public void TrackLeaderboardView(string scope)
        {
            if (analytics == null) return;
            analytics.TrackLeaderboardViewed(scope);
        }

        #endregion

        #region Helpers

        private bool IsMilestoneFloor(int floor)
        {
            // Define milestone floors: 10, 25, 50, 75, 100, 150, 200, etc.
            int[] milestones = { 10, 25, 50, 75, 100, 150, 200, 250, 300, 400, 500, 750, 1000 };
            foreach (int milestone in milestones)
            {
                if (floor == milestone)
                    return true;
            }
            return false;
        }

        #endregion
    }
}
