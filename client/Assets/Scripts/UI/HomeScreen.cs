using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerClimb.Gameplay;
using TowerClimb.API;

namespace TowerClimb.UI
{
    /// <summary>
    /// Home screen UI controller
    /// Shows: Play button, personal best, missions, leaderboard link
    /// </summary>
    public class HomeScreen : MonoBehaviour
    {
        public static HomeScreen Instance { get; private set; }

        [Header("UI Elements")]
        public Button startButton;
        public Button practiceButton;
        public Button leaderboardButton;
        public Button missionsButton;
        public Button shopButton;
        public Button settingsButton;
        public TextMeshProUGUI personalBestText;
        public TextMeshProUGUI weekInfoText;
        public GameObject loadingPanel;

        [Header("Screen References")]
        public GameObject gameScreen;
        public GameObject leaderboardScreen;
        public GameObject missionsScreen;
        public GameObject practiceScreen;
        public GameObject shopScreen;
        public GameObject settingsScreen;

        private SessionManager sessionManager;
        private GameStateMachine gameStateMachine;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            sessionManager = SessionManager.Instance;
            gameStateMachine = GameStateMachine.Instance;

            // Setup button listeners
            if (startButton != null)
            {
                startButton.onClick.AddListener(OnStartClicked);
            }

            if (practiceButton != null)
            {
                practiceButton.onClick.AddListener(OnPracticeClicked);
            }

            if (leaderboardButton != null)
            {
                leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
            }

            if (missionsButton != null)
            {
                missionsButton.onClick.AddListener(OnMissionsClicked);
            }

            if (shopButton != null)
            {
                shopButton.onClick.AddListener(OnShopClicked);
            }

            if (settingsButton != null)
            {
                settingsButton.onClick.AddListener(OnSettingsClicked);
            }

            // Subscribe to session events
            if (sessionManager != null)
            {
                sessionManager.OnAuthComplete += HandleAuthComplete;
                sessionManager.OnRunStarted += HandleRunStarted;
            }

            // Subscribe to game state changes
            if (gameStateMachine != null)
            {
                gameStateMachine.OnStateChanged += HandleStateChanged;
            }

            // Initial state
            UpdateUI();
            Show();
        }

        private void OnDestroy()
        {
            if (sessionManager != null)
            {
                sessionManager.OnAuthComplete -= HandleAuthComplete;
                sessionManager.OnRunStarted -= HandleRunStarted;
            }

            if (gameStateMachine != null)
            {
                gameStateMachine.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            // Show HomeScreen when returning to Idle state
            if (newState == GameState.Idle)
            {
                Show();
            }
        }

        private void UpdateUI()
        {
            // Update personal best
            if (personalBestText != null)
            {
                // TODO: Load from PlayerPrefs or session data
                int pb = PlayerPrefs.GetInt("PersonalBest", 0);
                personalBestText.text = pb > 0 ? $"Personal Best: Floor {pb}" : "No runs yet - Start climbing!";
            }

            // Update week info
            if (weekInfoText != null && sessionManager?.currentSession != null)
            {
                weekInfoText.text = $"Week #{sessionManager.currentSession.weekId}";
            }
        }

        private void OnStartClicked()
        {
            Debug.Log("[HomeScreen] Start clicked");

            if (sessionManager == null)
            {
                Debug.LogError("[HomeScreen] SessionManager not found!");
                return;
            }

            ShowLoading(true);
            sessionManager.StartNewRun();
        }

        private void OnPracticeClicked()
        {
            Debug.Log("[HomeScreen] Practice clicked");
            ShowScreen(practiceScreen);
        }

        private void OnLeaderboardClicked()
        {
            Debug.Log("[HomeScreen] Leaderboard clicked");
            ShowScreen(leaderboardScreen);
        }

        private void OnMissionsClicked()
        {
            Debug.Log("[HomeScreen] Missions clicked");
            ShowScreen(missionsScreen);
        }

        private void OnShopClicked()
        {
            Debug.Log("[HomeScreen] Shop clicked");
            ShowScreen(shopScreen);
        }

        private void OnSettingsClicked()
        {
            Debug.Log("[HomeScreen] Settings clicked");
            ShowScreen(settingsScreen);
        }

        private void ShowScreen(GameObject screen)
        {
            if (screen != null)
            {
                Hide();
                screen.SetActive(true);
            }
            else
            {
                Debug.LogWarning($"[HomeScreen] Screen reference is null - assign in Inspector");
            }
        }

        private void HandleAuthComplete(bool success, string error)
        {
            if (success)
            {
                Debug.Log("[HomeScreen] Auth complete");
                UpdateUI();
            }
            else
            {
                Debug.LogError($"[HomeScreen] Auth failed: {error}");
                ShowLoading(false);
            }
        }

        private void HandleRunStarted(bool success, string error)
        {
            ShowLoading(false);

            if (success)
            {
                Debug.Log("[HomeScreen] Run started - hiding home screen");
                Hide();
            }
            else
            {
                Debug.LogError($"[HomeScreen] Failed to start run: {error}");
                // Show error message to user
            }
        }

        private void ShowLoading(bool show)
        {
            if (loadingPanel != null)
            {
                loadingPanel.SetActive(show);
            }

            if (startButton != null)
            {
                startButton.interactable = !show;
            }
        }

        public void Show()
        {
            gameObject.SetActive(true);
            UpdateUI();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
