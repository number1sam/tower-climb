using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerClimb.Core;
using TowerClimb.API;

namespace TowerClimb.UI
{
    /// <summary>
    /// Leaderboard display with tabs for Global/Country/Friends
    /// </summary>
    public class LeaderboardScreen : MonoBehaviour
    {
        [Header("UI References")]
        public Button globalButton;
        public Button countryButton;
        public Button friendsButton;
        public Button closeButton;
        public Button refreshButton;

        [Header("Content")]
        public Transform entryContainer;
        public GameObject entryPrefab;
        public GameObject loadingPanel;
        public TextMeshProUGUI weekText;
        public TextMeshProUGUI errorText;

        [Header("User Entry")]
        public GameObject userEntryPanel;
        public TextMeshProUGUI userRankText;
        public TextMeshProUGUI userHandleText;
        public TextMeshProUGUI userFloorText;

        [Header("Colors")]
        public Color activeTabColor = Color.white;
        public Color inactiveTabColor = Color.gray;

        private SessionManager sessionManager;
        private string currentScope = "global";
        private List<GameObject> entryObjects = new List<GameObject>();

        private void Start()
        {
            sessionManager = SessionManager.Instance;

            // Setup button listeners
            if (globalButton != null) globalButton.onClick.AddListener(() => OnTabClicked("global"));
            if (countryButton != null) countryButton.onClick.AddListener(() => OnTabClicked("country"));
            if (friendsButton != null) friendsButton.onClick.AddListener(() => OnTabClicked("friends"));
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);
            if (refreshButton != null) refreshButton.onClick.AddListener(OnRefreshClicked);

            Hide();
        }

        public void Show()
        {
            gameObject.SetActive(true);
            LoadLeaderboard("global");
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void OnTabClicked(string scope)
        {
            currentScope = scope;
            UpdateTabColors(scope);
            LoadLeaderboard(scope);
        }

        private void UpdateTabColors(string activeScope)
        {
            if (globalButton != null)
            {
                var colors = globalButton.colors;
                colors.normalColor = activeScope == "global" ? activeTabColor : inactiveTabColor;
                globalButton.colors = colors;
            }

            if (countryButton != null)
            {
                var colors = countryButton.colors;
                colors.normalColor = activeScope == "country" ? activeTabColor : inactiveTabColor;
                countryButton.colors = colors;
            }

            if (friendsButton != null)
            {
                var colors = friendsButton.colors;
                colors.normalColor = activeScope == "friends" ? activeTabColor : inactiveTabColor;
                friendsButton.colors = colors;
            }
        }

        private void LoadLeaderboard(string scope)
        {
            if (sessionManager == null)
            {
                ShowError("SessionManager not found");
                return;
            }

            ShowLoading(true);
            ClearEntries();

            sessionManager.FetchLeaderboard(null, scope, (response) =>
            {
                ShowLoading(false);

                if (response != null)
                {
                    DisplayLeaderboard(response);
                }
                else
                {
                    ShowError("Failed to load leaderboard");
                }
            });
        }

        private void DisplayLeaderboard(LeaderboardResponse response)
        {
            if (errorText != null) errorText.gameObject.SetActive(false);

            // Update week display
            if (weekText != null)
            {
                weekText.text = $"Week #{response.weekId}";
            }

            // Display entries
            foreach (var entry in response.entries)
            {
                CreateEntry(entry);
            }

            // Display user entry (highlighted)
            if (response.userEntry != null && userEntryPanel != null)
            {
                userEntryPanel.SetActive(true);

                if (userRankText != null)
                    userRankText.text = $"#{response.userEntry.rank}";

                if (userHandleText != null)
                    userHandleText.text = response.userEntry.handle;

                if (userFloorText != null)
                    userFloorText.text = $"Floor {response.userEntry.bestFloor}";
            }
        }

        private void CreateEntry(LeaderboardEntry entry)
        {
            if (entryPrefab == null || entryContainer == null) return;

            GameObject entryObj = Instantiate(entryPrefab, entryContainer);
            entryObjects.Add(entryObj);

            // Get text components (assumes prefab has these children)
            var rankText = entryObj.transform.Find("RankText")?.GetComponent<TextMeshProUGUI>();
            var handleText = entryObj.transform.Find("HandleText")?.GetComponent<TextMeshProUGUI>();
            var floorText = entryObj.transform.Find("FloorText")?.GetComponent<TextMeshProUGUI>();
            var statsText = entryObj.transform.Find("StatsText")?.GetComponent<TextMeshProUGUI>();

            if (rankText != null)
                rankText.text = $"#{entry.rank}";

            if (handleText != null)
                handleText.text = entry.handle;

            if (floorText != null)
                floorText.text = $"Floor {entry.bestFloor}";

            if (statsText != null)
                statsText.text = $"{entry.bestReactionMs}ms • {entry.perfectRate * 100:F1}%";

            entryObj.SetActive(true);
        }

        private void ClearEntries()
        {
            foreach (var obj in entryObjects)
            {
                if (obj != null)
                {
                    Destroy(obj);
                }
            }
            entryObjects.Clear();

            if (userEntryPanel != null)
            {
                userEntryPanel.SetActive(false);
            }
        }

        private void ShowLoading(bool show)
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(show);
            }
        }

        private void ShowError(string message)
        {
            if (errorText != null)
            {
                errorText.text = message;
                errorText.gameObject.SetActive(true);
            }

            Debug.LogError($"[LeaderboardScreen] {message}");
        }

        private void OnRefreshClicked()
        {
            LoadLeaderboard(currentScope);
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
}
