using UnityEngine;
using System.Collections.Generic;
using TowerClimb.Core;

namespace TowerClimb.Gameplay
{
    /// <summary>
    /// Handles all player input types: tap, swipe, hold, rhythm, tilt, double-tap
    /// Works with both touch (mobile) and mouse (testing)
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        [Header("Swipe Detection")]
        [SerializeField] private float minSwipeDistance = 50f;
        [SerializeField] private float maxSwipeTime = 0.5f;

        [Header("Hold Detection")]
        [SerializeField] private float minHoldTime = 0.3f;

        [Header("Double Tap Detection")]
        [SerializeField] private float doubleTapWindow = 0.3f;

        [Header("Rhythm Detection")]
        [SerializeField] private float rhythmTapWindow = 0.5f;
        [SerializeField] private float rhythmSequenceWindow = 2f;

        [Header("Tilt Detection")]
        [SerializeField] private float tiltThreshold = 0.3f;

        // Touch/Mouse state
        private Vector2 touchStartPos;
        private float touchStartTime;
        private bool isTouching;

        // Tap detection
        private bool tapDetected;
        private float lastTapTime;

        // Swipe detection
        private Direction lastSwipeDirection;
        private bool swipeDetected;

        // Hold detection
        private bool holdStartDetected;
        private float holdStartTime;

        // Rhythm detection
        private List<float> rhythmTaps = new List<float>();
        private bool rhythmDetected;

        // Tilt detection (using accelerometer)
        private Vector3 lastAcceleration;

        private void Start()
        {
            // Initialize accelerometer
            if (SystemInfo.supportsAccelerometer)
            {
                Input.acceleration = Vector3.zero;
            }

            lastAcceleration = Input.acceleration;
        }

        private void Update()
        {
            ProcessInput();
        }

        private void LateUpdate()
        {
            // Reset per-frame detection flags
            tapDetected = false;
            swipeDetected = false;
            holdStartDetected = false;
            rhythmDetected = false;
        }

        /// <summary>
        /// Process touch/mouse input
        /// </summary>
        private void ProcessInput()
        {
            // Handle touch input (mobile)
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);

                switch (touch.phase)
                {
                    case TouchPhase.Began:
                        OnTouchStart(touch.position);
                        break;

                    case TouchPhase.Ended:
                        OnTouchEnd(touch.position);
                        break;
                }
            }
            // Handle mouse input (testing on PC)
            else if (Input.GetMouseButtonDown(0))
            {
                OnTouchStart(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0))
            {
                OnTouchEnd(Input.mousePosition);
            }

            // Update hold state
            if (isTouching)
            {
                float holdDuration = Time.time - touchStartTime;
                if (holdDuration >= minHoldTime && !holdStartDetected)
                {
                    holdStartDetected = true;
                    holdStartTime = Time.time;
                }
            }

            // Clean up old rhythm taps
            rhythmTaps.RemoveAll(t => Time.time - t > rhythmSequenceWindow);
        }

        private void OnTouchStart(Vector2 position)
        {
            touchStartPos = position;
            touchStartTime = Time.time;
            isTouching = true;
        }

        private void OnTouchEnd(Vector2 position)
        {
            if (!isTouching) return;

            float touchDuration = Time.time - touchStartTime;
            Vector2 delta = position - touchStartPos;
            float distance = delta.magnitude;

            // Detect swipe
            if (distance >= minSwipeDistance && touchDuration <= maxSwipeTime)
            {
                lastSwipeDirection = GetSwipeDirection(delta);
                swipeDetected = true;
            }
            // Detect tap (short touch, minimal movement)
            else if (touchDuration < 0.3f && distance < 30f)
            {
                OnTap();
            }

            isTouching = false;
        }

        private void OnTap()
        {
            tapDetected = true;
            rhythmTaps.Add(Time.time);

            // Check for double tap
            if (Time.time - lastTapTime <= doubleTapWindow)
            {
                // Double tap detected (handled by IsDoubleTap check)
            }

            lastTapTime = Time.time;
        }

        /// <summary>
        /// Determine swipe direction from delta
        /// </summary>
        private Direction GetSwipeDirection(Vector2 delta)
        {
            float absX = Mathf.Abs(delta.x);
            float absY = Mathf.Abs(delta.y);

            if (absX > absY)
            {
                return delta.x > 0 ? Direction.Right : Direction.Left;
            }
            else
            {
                return delta.y > 0 ? Direction.Up : Direction.Down;
            }
        }

        // PUBLIC API for PatternExecutor

        /// <summary>
        /// Check if a tap occurred this frame
        /// </summary>
        public bool IsTap()
        {
            return tapDetected;
        }

        /// <summary>
        /// Check if a swipe in the specified direction occurred
        /// </summary>
        public bool IsSwipe(Direction direction)
        {
            return swipeDetected && lastSwipeDirection == direction;
        }

        /// <summary>
        /// Check if hold started (for Hold pattern tracking)
        /// </summary>
        public bool IsHoldStart()
        {
            return holdStartDetected;
        }

        /// <summary>
        /// Check if rhythm pattern completed (N taps within time window)
        /// </summary>
        public bool IsRhythm(int requiredTaps)
        {
            if (rhythmDetected) return false; // Already detected this sequence

            // Check if we have enough taps within the sequence window
            int recentTaps = 0;
            float now = Time.time;

            foreach (float tapTime in rhythmTaps)
            {
                if (now - tapTime <= rhythmTapWindow * requiredTaps)
                {
                    recentTaps++;
                }
            }

            if (recentTaps >= requiredTaps)
            {
                rhythmDetected = true;
                rhythmTaps.Clear();
                return true;
            }

            return false;
        }

        /// <summary>
        /// Check if device is tilted in the specified direction
        /// </summary>
        public bool IsTilt(Direction direction)
        {
            if (!SystemInfo.supportsAccelerometer)
            {
                // Fallback for testing: use arrow keys
                switch (direction)
                {
                    case Direction.Left: return Input.GetKeyDown(KeyCode.LeftArrow);
                    case Direction.Right: return Input.GetKeyDown(KeyCode.RightArrow);
                    case Direction.Up: return Input.GetKeyDown(KeyCode.UpArrow);
                    case Direction.Down: return Input.GetKeyDown(KeyCode.DownArrow);
                }
                return false;
            }

            Vector3 accel = Input.acceleration;
            Vector3 delta = accel - lastAcceleration;

            bool tilted = false;

            switch (direction)
            {
                case Direction.Left:
                    tilted = delta.x < -tiltThreshold;
                    break;

                case Direction.Right:
                    tilted = delta.x > tiltThreshold;
                    break;

                case Direction.Up:
                    tilted = delta.y > tiltThreshold;
                    break;

                case Direction.Down:
                    tilted = delta.y < -tiltThreshold;
                    break;
            }

            if (tilted)
            {
                lastAcceleration = accel;
            }

            return tilted;
        }

        /// <summary>
        /// Check if double tap occurred
        /// </summary>
        public bool IsDoubleTap()
        {
            if (!tapDetected) return false;

            // Check if this tap is within double-tap window of previous tap
            return Time.time - lastTapTime <= doubleTapWindow;
        }

        // DEBUG VISUALIZATION
        private void OnGUI()
        {
            if (!Debug.isDebugBuild) return;

            GUILayout.BeginArea(new Rect(10, 10, 300, 200));
            GUILayout.Label($"Tap: {tapDetected}");
            GUILayout.Label($"Swipe: {(swipeDetected ? lastSwipeDirection.ToString() : "None")}");
            GUILayout.Label($"Hold: {(isTouching ? (Time.time - touchStartTime).ToString("F2") + "s" : "None")}");
            GUILayout.Label($"Rhythm Taps: {rhythmTaps.Count}");
            GUILayout.Label($"Accel: {Input.acceleration}");
            GUILayout.EndArea();
        }
    }
}
