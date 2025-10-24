using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerClimb.Gameplay;
using TowerClimb.API;

namespace TowerClimb.UI
{
    /// <summary>
    /// Results screen shown after run ends
    /// Displays: floors reached, stats, comparison to PB, retry button
    /// </summary>
    public class ResultsScreen : MonoBehaviour
    {
        [Header("UI Elements")]
        public TextMeshProUGUI floorsReachedText;
        public TextMeshProUGUI runtimeText;
        public TextMeshProUGUI avgReactionText;
        public TextMeshProUGUI perfectRateText;
        public TextMeshProUGUI comparisonText;
        public TextMeshProUGUI tipText;

        public Button retryButton;
        public Button homeButton;
        public Button shareButton;

        public GameObject newBestIndicator;
        public GameObject submittingPanel;

        private GameStateMachine gameStateMachine;
        private SessionManager sessionManager;
        private RunStats lastRunStats;

        private void Start()
        {
            gameStateMachine = GameStateMachine.Instance;
            sessionManager = SessionManager.Instance;

            // Setup button listeners
            if (retryButton != null)
            {
                retryButton.onClick.AddListener(OnRetryClicked);
            }

            if (homeButton != null)
            {
                homeButton.onClick.AddListener(OnHomeClicked);
            }

            if (shareButton != null)
            {
                shareButton.onClick.AddListener(OnShareClicked);
            }

            // Subscribe to events
            if (gameStateMachine != null)
            {
                gameStateMachine.OnStateChanged += HandleStateChanged;
            }

            if (sessionManager != null)
            {
                sessionManager.OnRunSubmitted += HandleRunSubmitted;
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (gameStateMachine != null)
            {
                gameStateMachine.OnStateChanged -= HandleStateChanged;
            }

            if (sessionManager != null)
            {
                sessionManager.OnRunSubmitted -= HandleRunSubmitted;
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.Results)
            {
                Show();
            }
            else if (newState == GameState.Idle || newState == GameState.PreRun)
            {
                Hide();
            }
        }

        private void Show()
        {
            gameObject.SetActive(true);

            // Get stats from game state machine
            lastRunStats = gameStateMachine.GetRunStats();

            UpdateUI();
            ShowSubmitting(true);
        }

        private void Hide()
        {
            gameObject.SetActive(false);
        }

        private void UpdateUI()
        {
            // Floors reached
            if (floorsReachedText != null)
            {
                floorsReachedText.text = $"Floor {lastRunStats.floors}";
            }

            // Runtime
            if (runtimeText != null)
            {
                int minutes = Mathf.FloorToInt(lastRunStats.runtimeSeconds / 60);
                int seconds = Mathf.FloorToInt(lastRunStats.runtimeSeconds % 60);
                runtimeText.text = $"{minutes:00}:{seconds:00}";
            }

            // Average reaction
            if (avgReactionText != null)
            {
                avgReactionText.text = $"{lastRunStats.avgReactionMs}ms";
            }

            // Perfect rate
            if (perfectRateText != null)
            {
                perfectRateText.text = $"{lastRunStats.perfectRate * 100:F1}%";
            }

            // Comparison to PB
            int previousBest = PlayerPrefs.GetInt("PersonalBest", 0);
            bool isNewBest = lastRunStats.floors > previousBest;

            if (comparisonText != null)
            {
                if (isNewBest)
                {
                    int improvement = lastRunStats.floors - previousBest;
                    comparisonText.text = previousBest > 0
                        ? $"+{improvement} floors!"
                        : "First run!";
                }
                else if (previousBest > 0)
                {
                    int deficit = previousBest - lastRunStats.floors;
                    comparisonText.text = $"-{deficit} from PB ({previousBest})";
                }
                else
                {
                    comparisonText.text = "Keep climbing!";
                }
            }

            if (newBestIndicator != null)
            {
                newBestIndicator.SetActive(isNewBest);
            }

            // Update PB if needed
            if (isNewBest)
            {
                PlayerPrefs.SetInt("PersonalBest", lastRunStats.floors);
                PlayerPrefs.Save();
            }

            // Show coaching tip
            if (tipText != null)
            {
                tipText.text = GetCoachingTip();
            }
        }

        private void HandleRunSubmitted(bool success, string error)
        {
            ShowSubmitting(false);

            if (success)
            {
                Debug.Log("[ResultsScreen] Run submitted successfully");
                // Refresh UI with server response if needed
            }
            else
            {
                Debug.LogError($"[ResultsScreen] Failed to submit run: {error}");
                // Show error message
            }
        }

        private string GetCoachingTip()
        {
            // Analyze player model to give actionable tip
            var playerModel = gameStateMachine.playerModel;

            if (playerModel.weaknesses.Count > 0)
            {
                // Find weakest pattern
                Core.PatternType weakest = Core.PatternType.Tap;
                float maxWeakness = 0f;

                foreach (var kvp in playerModel.weaknesses)
                {
                    if (kvp.Value > maxWeakness)
                    {
                        maxWeakness = kvp.Value;
                        weakest = kvp.Key;
                    }
                }

                if (maxWeakness > 0.5f)
                {
                    return $"💡 Tip: Practice {weakest} patterns in Practice Mode";
                }
            }

            // Generic tips based on stats
            if (lastRunStats.avgReactionMs > 500)
            {
                return "💡 Tip: Try to react faster - aim for under 400ms";
            }
            else if (lastRunStats.perfectRate < 0.5f)
            {
                return "💡 Tip: Focus on timing - wait for the perfect moment";
            }
            else
            {
                return "💡 Great run! Keep climbing!";
            }
        }

        private void ShowSubmitting(bool show)
        {
            if (submittingPanel != null)
            {
                submittingPanel.SetActive(show);
            }
        }

        private void OnRetryClicked()
        {
            Debug.Log("[ResultsScreen] Retry clicked");

            if (sessionManager != null)
            {
                sessionManager.StartNewRun();
            }
        }

        private void OnHomeClicked()
        {
            Debug.Log("[ResultsScreen] Home clicked");

            if (gameStateMachine != null)
            {
                gameStateMachine.ReturnToIdle();
            }
        }

        private void OnShareClicked()
        {
            Debug.Log("[ResultsScreen] Share clicked");

            // Share screenshot with stats
            string shareText = $"I reached floor {lastRunStats.floors} in Tower Climb! Can you beat it?";
            // TODO: Implement native sharing
            Debug.Log($"Share: {shareText}");
        }
    }
}
