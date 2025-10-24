using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerClimb.Gameplay;

namespace TowerClimb.UI
{
    /// <summary>
    /// Displays daily missions with progress bars
    /// </summary>
    public class MissionsScreen : MonoBehaviour
    {
        [Header("UI References")]
        public Button closeButton;
        public Transform missionContainer;
        public GameObject missionEntryPrefab;
        public TextMeshProUGUI titleText;

        private MissionsManager missionsManager;
        private List<GameObject> missionEntries = new List<GameObject>();

        private void Start()
        {
            missionsManager = MissionsManager.Instance;

            if (missionsManager != null)
            {
                missionsManager.OnMissionCompleted += HandleMissionCompleted;
                missionsManager.OnMissionProgress += HandleMissionProgress;
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (missionsManager != null)
            {
                missionsManager.OnMissionCompleted -= HandleMissionCompleted;
                missionsManager.OnMissionProgress -= HandleMissionProgress;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            RefreshMissions();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void RefreshMissions()
        {
            ClearMissionEntries();

            if (missionsManager == null) return;

            foreach (var mission in missionsManager.dailyMissions)
            {
                CreateMissionEntry(mission);
            }
        }

        private void CreateMissionEntry(Mission mission)
        {
            if (missionEntryPrefab == null || missionContainer == null) return;

            GameObject entryObj = Instantiate(missionEntryPrefab, missionContainer);
            missionEntries.Add(entryObj);

            // Get components from prefab
            var descriptionText = entryObj.transform.Find("DescriptionText")?.GetComponent<TextMeshProUGUI>();
            var progressText = entryObj.transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
            var progressBar = entryObj.transform.Find("ProgressBar")?.GetComponent<Slider>();
            var rewardText = entryObj.transform.Find("RewardText")?.GetComponent<TextMeshProUGUI>();
            var completedIcon = entryObj.transform.Find("CompletedIcon")?.gameObject;

            bool isCompleted = missionsManager.IsMissionCompleted(mission.id);
            int currentProgress = missionsManager.GetMissionProgress(mission.id);

            if (descriptionText != null)
            {
                descriptionText.text = mission.description;
            }

            if (progressText != null)
            {
                progressText.text = $"{currentProgress}/{mission.target}";
            }

            if (progressBar != null)
            {
                progressBar.maxValue = mission.target;
                progressBar.value = currentProgress;
            }

            if (rewardText != null)
            {
                rewardText.text = $"Reward: {FormatReward(mission.reward)}";
            }

            if (completedIcon != null)
            {
                completedIcon.SetActive(isCompleted);
            }

            // Store mission ID for updates
            var missionEntry = entryObj.AddComponent<MissionEntry>();
            missionEntry.missionId = mission.id;

            entryObj.SetActive(true);
        }

        private void ClearMissionEntries()
        {
            foreach (var obj in missionEntries)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            missionEntries.Clear();
        }

        private string FormatReward(string reward)
        {
            // Format reward ID into readable text
            return reward.Replace("_", " ").Replace("sfx pack", "SFX Pack").Replace("theme", "Theme");
        }

        private void HandleMissionCompleted(Mission mission)
        {
            Debug.Log($"[MissionsScreen] Mission completed: {mission.description}");

            // Show completion notification
            ShowNotification($"Mission Complete!\n{mission.description}");

            // Refresh display
            RefreshMissions();
        }

        private void HandleMissionProgress(Mission mission, int progress)
        {
            // Find the entry for this mission and update it
            foreach (var entryObj in missionEntries)
            {
                var missionEntry = entryObj.GetComponent<MissionEntry>();
                if (missionEntry != null && missionEntry.missionId == mission.id)
                {
                    var progressText = entryObj.transform.Find("ProgressText")?.GetComponent<TextMeshProUGUI>();
                    var progressBar = entryObj.transform.Find("ProgressBar")?.GetComponent<Slider>();

                    if (progressText != null)
                    {
                        progressText.text = $"{progress}/{mission.target}";
                    }

                    if (progressBar != null)
                    {
                        progressBar.value = progress;
                    }

                    break;
                }
            }
        }

        private void ShowNotification(string message)
        {
            // TODO: Show popup notification
            Debug.Log($"[MissionsScreen] Notification: {message}");
        }

        private void OnCloseClicked()
        {
            Hide();

            // Return to home screen
            if (HomeScreen.Instance != null)
            {
                HomeScreen.Instance.Show();
            }
        }
    }

    /// <summary>
    /// Helper component to store mission ID on entry GameObject
    /// </summary>
    public class MissionEntry : MonoBehaviour
    {
        public string missionId;
    }
}
