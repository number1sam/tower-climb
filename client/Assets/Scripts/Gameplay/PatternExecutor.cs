using UnityEngine;
using UnityEngine.UI;
using TMPro;
using TowerClimb.Core;

namespace TowerClimb.Gameplay
{
    /// <summary>
    /// Executes a single pattern challenge and tracks player response timing
    /// </summary>
    public class PatternExecutor : MonoBehaviour
    {
        [Header("UI References")]
        public Image patternIcon;
        public TextMeshProUGUI patternText;
        public Image timerBar;
        public TextMeshProUGUI floorText;

        [Header("Pattern Icons")]
        public Sprite tapIcon;
        public Sprite swipeLeftIcon;
        public Sprite swipeRightIcon;
        public Sprite swipeUpIcon;
        public Sprite swipeDownIcon;
        public Sprite holdIcon;
        public Sprite rhythmIcon;
        public Sprite tiltIcon;
        public Sprite doubleTapIcon;

        [Header("Feedback")]
        public GameObject perfectFeedback;
        public GameObject goodFeedback;
        public GameObject missFeedback;

        private Pattern currentPattern;
        private float patternStartTime;
        private float timeWindow;
        private bool patternCompleted;
        private bool holdingPattern;
        private float holdStartTime;

        private InputHandler inputHandler;
        private GameStateMachine gameStateMachine;

        private void Awake()
        {
            inputHandler = FindObjectOfType<InputHandler>();
            if (inputHandler == null)
            {
                Debug.LogError("[PatternExecutor] InputHandler not found in scene!");
            }
        }

        private void Start()
        {
            gameStateMachine = GameStateMachine.Instance;
            if (gameStateMachine != null)
            {
                gameStateMachine.OnNewPattern += StartPattern;
            }

            HideFeedback();
        }

        private void OnDestroy()
        {
            if (gameStateMachine != null)
            {
                gameStateMachine.OnNewPattern -= StartPattern;
            }
        }

        /// <summary>
        /// Start executing a new pattern
        /// </summary>
        public void StartPattern(Pattern pattern)
        {
            currentPattern = pattern;
            timeWindow = pattern.timeWindow;
            patternStartTime = Time.time;
            patternCompleted = false;
            holdingPattern = false;

            UpdateUI();
            HideFeedback();

            Debug.Log($"[PatternExecutor] Started pattern: {pattern}");
        }

        private void Update()
        {
            if (gameStateMachine.currentState != GameState.PlayingFloor || patternCompleted)
                return;

            float elapsed = Time.time - patternStartTime;

            // Update timer bar
            if (timerBar != null)
            {
                timerBar.fillAmount = 1f - (elapsed / timeWindow);
            }

            // Check for timeout
            if (elapsed >= timeWindow)
            {
                if (currentPattern.type == PatternType.Hold && holdingPattern)
                {
                    // Check if hold duration was met
                    float holdDuration = Time.time - holdStartTime;
                    if (holdDuration >= currentPattern.duration)
                    {
                        CompletePattern(true, elapsed);
                    }
                    else
                    {
                        CompletePattern(false, elapsed);
                    }
                }
                else
                {
                    // Timeout - pattern failed
                    CompletePattern(false, elapsed);
                }
                return;
            }

            // Check for pattern match
            CheckPatternMatch();
        }

        /// <summary>
        /// Check if player input matches current pattern
        /// </summary>
        private void CheckPatternMatch()
        {
            if (inputHandler == null) return;

            bool matched = false;

            switch (currentPattern.type)
            {
                case PatternType.Tap:
                    matched = inputHandler.IsTap();
                    break;

                case PatternType.Swipe:
                    matched = inputHandler.IsSwipe(currentPattern.direction);
                    break;

                case PatternType.Hold:
                    if (!holdingPattern && inputHandler.IsHoldStart())
                    {
                        holdingPattern = true;
                        holdStartTime = Time.time;
                    }
                    else if (holdingPattern)
                    {
                        float holdDuration = Time.time - holdStartTime;
                        if (holdDuration >= currentPattern.duration)
                        {
                            matched = true;
                        }
                    }
                    break;

                case PatternType.Rhythm:
                    matched = inputHandler.IsRhythm(currentPattern.complexity);
                    break;

                case PatternType.Tilt:
                    matched = inputHandler.IsTilt(currentPattern.direction);
                    break;

                case PatternType.DoubleTap:
                    matched = inputHandler.IsDoubleTap();
                    break;
            }

            if (matched)
            {
                float elapsed = Time.time - patternStartTime;
                CompletePattern(true, elapsed);
            }
        }

        /// <summary>
        /// Complete the current pattern (success or failure)
        /// </summary>
        private void CompletePattern(bool success, float elapsed)
        {
            if (patternCompleted) return;

            patternCompleted = true;

            int reactionMs = Mathf.RoundToInt(elapsed * 1000f);
            float accuracy = CalculateAccuracy(elapsed, timeWindow);

            ShowFeedback(accuracy);

            Debug.Log($"[PatternExecutor] Pattern {(success ? "SUCCESS" : "FAILED")}: {reactionMs}ms, accuracy={accuracy:F2}");

            // Notify game state machine
            if (success)
            {
                gameStateMachine.PatternSuccess(reactionMs, accuracy);
            }
            else
            {
                gameStateMachine.PatternFailed(reactionMs, accuracy);
            }
        }

        /// <summary>
        /// Calculate accuracy based on reaction time vs time window
        /// </summary>
        private float CalculateAccuracy(float reactionTime, float timeWindow)
        {
            // Perfect = used less than 40% of time window
            // Good = used less than 70% of time window
            // OK = used less than 100% of time window

            float ratio = reactionTime / timeWindow;

            if (ratio < 0.4f)
                return 1.0f; // Perfect
            else if (ratio < 0.7f)
                return 0.85f; // Good
            else
                return 0.6f; // OK
        }

        /// <summary>
        /// Update UI to show current pattern
        /// </summary>
        private void UpdateUI()
        {
            // Update floor counter
            if (floorText != null)
            {
                floorText.text = $"Floor {gameStateMachine.currentFloor}";
            }

            // Update pattern icon
            if (patternIcon != null)
            {
                patternIcon.sprite = GetPatternIcon(currentPattern);
            }

            // Update pattern text
            if (patternText != null)
            {
                patternText.text = GetPatternText(currentPattern);
            }

            // Reset timer bar
            if (timerBar != null)
            {
                timerBar.fillAmount = 1f;
            }
        }

        /// <summary>
        /// Get appropriate icon for pattern type
        /// </summary>
        private Sprite GetPatternIcon(Pattern pattern)
        {
            switch (pattern.type)
            {
                case PatternType.Tap:
                    return tapIcon;

                case PatternType.Swipe:
                    switch (pattern.direction)
                    {
                        case Direction.Left: return swipeLeftIcon;
                        case Direction.Right: return swipeRightIcon;
                        case Direction.Up: return swipeUpIcon;
                        case Direction.Down: return swipeDownIcon;
                    }
                    break;

                case PatternType.Hold:
                    return holdIcon;

                case PatternType.Rhythm:
                    return rhythmIcon;

                case PatternType.Tilt:
                    return tiltIcon;

                case PatternType.DoubleTap:
                    return doubleTapIcon;
            }

            return tapIcon; // fallback
        }

        /// <summary>
        /// Get descriptive text for pattern
        /// </summary>
        private string GetPatternText(Pattern pattern)
        {
            switch (pattern.type)
            {
                case PatternType.Tap:
                    return "TAP";

                case PatternType.Swipe:
                    return $"SWIPE {pattern.direction.ToString().ToUpper()}";

                case PatternType.Hold:
                    return $"HOLD {pattern.duration:F1}s";

                case PatternType.Rhythm:
                    return $"TAP {pattern.complexity}X";

                case PatternType.Tilt:
                    return $"TILT {pattern.direction.ToString().ToUpper()}";

                case PatternType.DoubleTap:
                    return "DOUBLE TAP";

                default:
                    return "READY";
            }
        }

        /// <summary>
        /// Show visual feedback based on accuracy
        /// </summary>
        private void ShowFeedback(float accuracy)
        {
            HideFeedback();

            if (accuracy >= 0.95f && perfectFeedback != null)
            {
                perfectFeedback.SetActive(true);
                Invoke(nameof(HideFeedback), 0.5f);
            }
            else if (accuracy >= 0.7f && goodFeedback != null)
            {
                goodFeedback.SetActive(true);
                Invoke(nameof(HideFeedback), 0.5f);
            }
            else if (missFeedback != null)
            {
                missFeedback.SetActive(true);
                Invoke(nameof(HideFeedback), 0.5f);
            }
        }

        private void HideFeedback()
        {
            if (perfectFeedback != null) perfectFeedback.SetActive(false);
            if (goodFeedback != null) goodFeedback.SetActive(false);
            if (missFeedback != null) missFeedback.SetActive(false);
        }
    }
}
