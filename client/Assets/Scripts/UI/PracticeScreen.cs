using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerClimb.Core;
using TowerClimb.Gameplay;

namespace TowerClimb.UI
{
    /// <summary>
    /// Practice Mode UI - Pattern and speed selection
    /// </summary>
    public class PracticeScreen : MonoBehaviour
    {
        [Header("Pattern Selection")]
        public Button tapButton;
        public Button swipeButton;
        public Button holdButton;
        public Button rhythmButton;
        public Button tiltButton;
        public Button doubleTapButton;

        [Header("Speed Selection")]
        public Slider speedSlider;
        public TextMeshProUGUI speedValueText;

        [Header("Mode Selection")]
        public Toggle endlessModeToggle;

        [Header("Action Buttons")]
        public Button startButton;
        public Button stopButton;
        public Button closeButton;

        [Header("Stats Display")]
        public TextMeshProUGUI attemptsText;
        public TextMeshProUGUI successRateText;
        public TextMeshProUGUI perfectRateText;
        public TextMeshProUGUI avgReactionText;
        public GameObject statsPanel;

        [Header("Settings")]
        public Color selectedPatternColor = Color.green;
        public Color normalPatternColor = Color.white;

        private PracticeMode practiceMode;
        private PatternType selectedPattern = PatternType.Tap;

        private void Start()
        {
            practiceMode = PracticeMode.Instance;

            // Setup pattern buttons
            if (tapButton != null) tapButton.onClick.AddListener(() => SelectPattern(PatternType.Tap));
            if (swipeButton != null) swipeButton.onClick.AddListener(() => SelectPattern(PatternType.Swipe));
            if (holdButton != null) holdButton.onClick.AddListener(() => SelectPattern(PatternType.Hold));
            if (rhythmButton != null) rhythmButton.onClick.AddListener(() => SelectPattern(PatternType.Rhythm));
            if (tiltButton != null) tiltButton.onClick.AddListener(() => SelectPattern(PatternType.Tilt));
            if (doubleTapButton != null) doubleTapButton.onClick.AddListener(() => SelectPattern(PatternType.DoubleTap));

            // Setup speed slider
            if (speedSlider != null)
            {
                speedSlider.minValue = 0.5f;
                speedSlider.maxValue = 3.0f;
                speedSlider.value = 1.0f;
                speedSlider.onValueChanged.AddListener(OnSpeedChanged);
            }

            // Setup action buttons
            if (startButton != null) startButton.onClick.AddListener(OnStartClicked);
            if (stopButton != null) stopButton.onClick.AddListener(OnStopClicked);
            if (closeButton != null) closeButton.onClick.AddListener(OnCloseClicked);

            // Initial state
            SelectPattern(PatternType.Tap);
            UpdateUI();
            Hide();
        }

        private void Update()
        {
            if (practiceMode != null && practiceMode.IsPracticing())
            {
                UpdateStats();
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

        private void SelectPattern(PatternType pattern)
        {
            selectedPattern = pattern;
            UpdatePatternButtons();
        }

        private void UpdatePatternButtons()
        {
            SetButtonColor(tapButton, selectedPattern == PatternType.Tap);
            SetButtonColor(swipeButton, selectedPattern == PatternType.Swipe);
            SetButtonColor(holdButton, selectedPattern == PatternType.Hold);
            SetButtonColor(rhythmButton, selectedPattern == PatternType.Rhythm);
            SetButtonColor(tiltButton, selectedPattern == PatternType.Tilt);
            SetButtonColor(doubleTapButton, selectedPattern == PatternType.DoubleTap);
        }

        private void SetButtonColor(Button button, bool selected)
        {
            if (button == null) return;

            var colors = button.colors;
            colors.normalColor = selected ? selectedPatternColor : normalPatternColor;
            button.colors = colors;
        }

        private void OnSpeedChanged(float value)
        {
            if (speedValueText != null)
            {
                speedValueText.text = $"{value:F1}x";
            }
        }

        private void OnStartClicked()
        {
            if (practiceMode == null) return;

            float speed = speedSlider != null ? speedSlider.value : 1.0f;
            bool endless = endlessModeToggle != null && endlessModeToggle.isOn;

            practiceMode.endlessMode = endless;
            practiceMode.StartPractice(selectedPattern, speed);

            UpdateUI();
            Hide(); // Hide practice menu and show game screen
        }

        private void OnStopClicked()
        {
            if (practiceMode == null) return;

            practiceMode.EndPractice();
            UpdateUI();
            Show(); // Return to practice menu
        }

        private void OnCloseClicked()
        {
            if (practiceMode != null && practiceMode.IsPracticing())
            {
                practiceMode.EndPractice();
            }

            Hide();

            // Return to home screen
            if (HomeScreen.Instance != null)
            {
                HomeScreen.Instance.Show();
            }
        }

        private void UpdateUI()
        {
            bool isPracticing = practiceMode != null && practiceMode.IsPracticing();

            if (startButton != null)
            {
                startButton.gameObject.SetActive(!isPracticing);
            }

            if (stopButton != null)
            {
                stopButton.gameObject.SetActive(isPracticing);
            }

            if (statsPanel != null)
            {
                statsPanel.SetActive(isPracticing);
            }

            if (isPracticing)
            {
                UpdateStats();
            }
        }

        private void UpdateStats()
        {
            if (practiceMode == null) return;

            if (attemptsText != null)
            {
                attemptsText.text = $"Attempts: {practiceMode.totalAttempts}";
            }

            if (successRateText != null)
            {
                successRateText.text = $"Success: {practiceMode.GetSuccessRate() * 100:F1}%";
            }

            if (perfectRateText != null)
            {
                perfectRateText.text = $"Perfect: {practiceMode.GetPerfectRate() * 100:F1}%";
            }

            if (avgReactionText != null)
            {
                avgReactionText.text = $"Avg Reaction: {practiceMode.GetAverageReaction()}ms";
            }
        }
    }
}
