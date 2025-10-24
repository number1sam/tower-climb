using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerClimb.Gameplay;
using TowerClimb.Core;

namespace TowerClimb.UI
{
    /// <summary>
    /// Complete Game Screen controller - manages active gameplay UI
    /// Coordinates PatternExecutor and displays game state
    /// </summary>
    public class GameScreen : MonoBehaviour
    {
        [Header("UI References")]
        public TextMeshProUGUI floorText;
        public TextMeshProUGUI speedText;
        public Slider progressBar;
        public GameObject pauseButton;

        [Header("Pattern Display")]
        public PatternExecutor patternExecutor;

        [Header("HUD")]
        public TextMeshProUGUI comboText;
        public GameObject comboPanel;
        public Image speedIndicator;
        public Color slowColor = Color.green;
        public Color mediumColor = Color.yellow;
        public Color fastColor = Color.red;

        private GameStateMachine gameStateMachine;
        private int currentCombo = 0;

        private void Start()
        {
            gameStateMachine = GameStateMachine.Instance;

            if (gameStateMachine != null)
            {
                gameStateMachine.OnStateChanged += HandleStateChanged;
                gameStateMachine.OnFloorChanged += HandleFloorChanged;
                gameStateMachine.OnPatternCompleted += HandlePatternCompleted;
            }

            Hide();
        }

        private void OnDestroy()
        {
            if (gameStateMachine != null)
            {
                gameStateMachine.OnStateChanged -= HandleStateChanged;
                gameStateMachine.OnFloorChanged -= HandleFloorChanged;
                gameStateMachine.OnPatternCompleted -= HandlePatternCompleted;
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            switch (newState)
            {
                case GameState.PreRun:
                    // Show countdown or pre-run UI
                    Show();
                    break;

                case GameState.PlayingFloor:
                    Show();
                    break;

                case GameState.Idle:
                case GameState.Results:
                    Hide();
                    break;
            }
        }

        private void HandleFloorChanged(int floor)
        {
            UpdateFloorDisplay(floor);
            UpdateProgressBar(floor);
            UpdateSpeedIndicator();
        }

        private void HandlePatternCompleted(PatternResult result)
        {
            if (result.success)
            {
                currentCombo++;
                UpdateComboDisplay();
            }
            else
            {
                currentCombo = 0;
                HideComboDisplay();
            }
        }

        private void UpdateFloorDisplay(int floor)
        {
            if (floorText != null)
            {
                floorText.text = $"FLOOR {floor}";
            }
        }

        private void UpdateProgressBar(int floor)
        {
            if (progressBar != null)
            {
                // Progress through the current "tier" (every 10 floors)
                int tierProgress = floor % 10;
                progressBar.value = tierProgress / 10f;
            }
        }

        private void UpdateSpeedIndicator()
        {
            if (speedIndicator == null || gameStateMachine == null) return;

            float currentSpeed = gameStateMachine.currentPattern?.speed ?? 1.0f;

            // Color code based on speed
            if (currentSpeed < 1.5f)
            {
                speedIndicator.color = slowColor;
            }
            else if (currentSpeed < 2.5f)
            {
                speedIndicator.color = mediumColor;
            }
            else
            {
                speedIndicator.color = fastColor;
            }

            if (speedText != null)
            {
                speedText.text = $"x{currentSpeed:F1}";
            }
        }

        private void UpdateComboDisplay()
        {
            if (comboText != null && currentCombo > 1)
            {
                comboText.text = $"{currentCombo}x COMBO!";

                if (comboPanel != null)
                {
                    comboPanel.SetActive(true);
                }
            }
        }

        private void HideComboDisplay()
        {
            if (comboPanel != null)
            {
                comboPanel.SetActive(false);
            }
        }

        public void OnPauseClicked()
        {
            // TODO: Implement pause menu
            Debug.Log("[GameScreen] Pause clicked - not implemented yet");
        }

        public void Show()
        {
            gameObject.SetActive(true);
            currentCombo = 0;
            HideComboDisplay();
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }
    }
}
