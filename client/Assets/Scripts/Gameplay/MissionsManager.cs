using System;
using System.Collections.Generic;
using UnityEngine;
using TowerClimb.Core;

namespace TowerClimb.Gameplay
{
    /// <summary>
    /// Tracks daily mission progress during gameplay
    /// </summary>
    public class MissionsManager : MonoBehaviour
    {
        public static MissionsManager Instance { get; private set; }

        [Header("Mission Definitions")]
        public List<Mission> dailyMissions = new List<Mission>();

        // Events
        public event Action<Mission> OnMissionCompleted;
        public event Action<Mission, int> OnMissionProgress;

        private GameStateMachine gameStateMachine;
        private Dictionary<string, int> currentProgress = new Dictionary<string, int>();
        private Dictionary<string, int> sessionTracking = new Dictionary<string, int>();

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

            LoadMissions();
        }

        private void Start()
        {
            gameStateMachine = GameStateMachine.Instance;

            if (gameStateMachine != null)
            {
                gameStateMachine.OnPatternCompleted += HandlePatternCompleted;
                gameStateMachine.OnRunEnded += HandleRunEnded;
            }
        }

        private void OnDestroy()
        {
            if (gameStateMachine != null)
            {
                gameStateMachine.OnPatternCompleted -= HandlePatternCompleted;
                gameStateMachine.OnRunEnded -= HandleRunEnded;
            }
        }

        private void LoadMissions()
        {
            // Load from PlayerPrefs or fetch from remote config
            // For now, hardcode some example missions
            dailyMissions = new List<Mission>
            {
                new Mission
                {
                    id = "3_perfect_holds",
                    description = "Achieve 3 perfect holds in a row",
                    target = 3,
                    trackingKey = "consecutive_perfect_holds",
                    reward = "sfx_pack_minimal"
                },
                new Mission
                {
                    id = "survive_60s",
                    description = "Survive for 60 seconds",
                    target = 60,
                    trackingKey = "runtime_seconds",
                    reward = "theme_ocean"
                },
                new Mission
                {
                    id = "floor_20",
                    description = "Reach floor 20",
                    target = 20,
                    trackingKey = "floors_reached",
                    reward = "theme_fire"
                },
                new Mission
                {
                    id = "50_perfect_patterns",
                    description = "Complete 50 perfect patterns",
                    target = 50,
                    trackingKey = "perfect_patterns",
                    reward = "sfx_pack_nature"
                }
            };

            LoadProgress();
        }

        private void LoadProgress()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            foreach (var mission in dailyMissions)
            {
                string key = $"Mission_{mission.id}_{today}";
                currentProgress[mission.id] = PlayerPrefs.GetInt(key, 0);
            }
        }

        private void SaveProgress()
        {
            string today = DateTime.Now.ToString("yyyy-MM-dd");

            foreach (var mission in dailyMissions)
            {
                string key = $"Mission_{mission.id}_{today}";
                PlayerPrefs.SetInt(key, currentProgress[mission.id]);
            }

            PlayerPrefs.Save();
        }

        private void HandlePatternCompleted(PatternResult result)
        {
            // Track consecutive perfect holds
            if (result.patternType == PatternType.Hold && result.accuracy >= 0.95f)
            {
                if (!sessionTracking.ContainsKey("consecutive_perfect_holds"))
                {
                    sessionTracking["consecutive_perfect_holds"] = 0;
                }
                sessionTracking["consecutive_perfect_holds"]++;

                CheckMission("3_perfect_holds", "consecutive_perfect_holds",
                    sessionTracking["consecutive_perfect_holds"]);
            }
            else if (result.patternType == PatternType.Hold)
            {
                sessionTracking["consecutive_perfect_holds"] = 0;
            }

            // Track total perfect patterns
            if (result.success && result.accuracy >= 0.95f)
            {
                IncrementProgress("50_perfect_patterns", "perfect_patterns", 1);
            }
        }

        private void HandleRunEnded()
        {
            var stats = gameStateMachine.GetRunStats();

            // Check runtime mission
            if (stats.runtimeSeconds >= 60)
            {
                CompleteMission("survive_60s");
            }

            // Check floor mission
            if (stats.floors >= 20)
            {
                CompleteMission("floor_20");
            }

            // Reset session tracking
            sessionTracking.Clear();
        }

        private void CheckMission(string missionId, string trackingKey, int value)
        {
            var mission = dailyMissions.Find(m => m.id == missionId);
            if (mission == null) return;

            if (value >= mission.target && !IsMissionCompleted(missionId))
            {
                CompleteMission(missionId);
            }
        }

        private void IncrementProgress(string missionId, string trackingKey, int amount)
        {
            if (!currentProgress.ContainsKey(missionId))
            {
                currentProgress[missionId] = 0;
            }

            currentProgress[missionId] += amount;
            SaveProgress();

            var mission = dailyMissions.Find(m => m.id == missionId);
            if (mission != null)
            {
                OnMissionProgress?.Invoke(mission, currentProgress[missionId]);

                if (currentProgress[missionId] >= mission.target && !IsMissionCompleted(missionId))
                {
                    CompleteMission(missionId);
                }
            }
        }

        private void CompleteMission(string missionId)
        {
            string key = $"MissionCompleted_{missionId}_{DateTime.Now.ToString("yyyy-MM-dd")}";

            if (PlayerPrefs.GetInt(key, 0) == 1)
            {
                return; // Already completed today
            }

            PlayerPrefs.SetInt(key, 1);
            PlayerPrefs.Save();

            var mission = dailyMissions.Find(m => m.id == missionId);
            if (mission != null)
            {
                Debug.Log($"[MissionsManager] Mission completed: {mission.description}");
                OnMissionCompleted?.Invoke(mission);

                // TODO: Grant reward via API
            }
        }

        public bool IsMissionCompleted(string missionId)
        {
            string key = $"MissionCompleted_{missionId}_{DateTime.Now.ToString("yyyy-MM-dd")}";
            return PlayerPrefs.GetInt(key, 0) == 1;
        }

        public int GetMissionProgress(string missionId)
        {
            if (currentProgress.ContainsKey(missionId))
            {
                return currentProgress[missionId];
            }
            return 0;
        }
    }

    [Serializable]
    public class Mission
    {
        public string id;
        public string description;
        public int target;
        public string trackingKey;
        public string reward;
    }
}
