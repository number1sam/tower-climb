using UnityEngine;
using TowerClimb.Core;
using TowerClimb.Gameplay;

namespace TowerClimb.Gameplay
{
    /// <summary>
    /// Manages visual effects: particles, screen shake, flash effects
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        public static VFXManager Instance { get; private set; }

        [Header("Particle Systems")]
        public ParticleSystem perfectParticles;
        public ParticleSystem goodParticles;
        public ParticleSystem missParticles;
        public ParticleSystem swipeTrailParticles;

        [Header("Screen Shake")]
        public Camera mainCamera;
        public float shakeIntensity = 0.1f;
        public float shakeDuration = 0.2f;

        [Header("Flash Effect")]
        public CanvasGroup flashOverlay;
        public Color perfectFlashColor = new Color(0, 1, 0.5f, 0.3f);
        public Color missFlashColor = new Color(1, 0, 0, 0.3f);

        private Vector3 originalCameraPosition;
        private float shakeTimer;
        private GameStateMachine gameStateMachine;

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

            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (mainCamera != null)
            {
                originalCameraPosition = mainCamera.transform.localPosition;
            }
        }

        private void Start()
        {
            gameStateMachine = GameStateMachine.Instance;

            if (gameStateMachine != null)
            {
                gameStateMachine.OnPatternCompleted += HandlePatternCompleted;
                gameStateMachine.OnStateChanged += HandleStateChanged;
            }
        }

        private void OnDestroy()
        {
            if (gameStateMachine != null)
            {
                gameStateMachine.OnPatternCompleted -= HandlePatternCompleted;
                gameStateMachine.OnStateChanged -= HandleStateChanged;
            }
        }

        private void Update()
        {
            // Update screen shake
            if (shakeTimer > 0)
            {
                shakeTimer -= Time.deltaTime;

                if (mainCamera != null)
                {
                    Vector3 shakeOffset = Random.insideUnitSphere * shakeIntensity;
                    mainCamera.transform.localPosition = originalCameraPosition + shakeOffset;
                }

                if (shakeTimer <= 0)
                {
                    StopScreenShake();
                }
            }
        }

        private void HandlePatternCompleted(PatternResult result)
        {
            if (result.success)
            {
                if (result.accuracy >= 0.95f)
                {
                    PlayPerfectEffect();
                }
                else if (result.accuracy >= 0.7f)
                {
                    PlayGoodEffect();
                }
            }
            else
            {
                PlayMissEffect();
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            if (newState == GameState.Failed)
            {
                PlayFailEffect();
            }
        }

        public void PlayPerfectEffect()
        {
            if (perfectParticles != null)
            {
                perfectParticles.Play();
            }

            FlashScreen(perfectFlashColor);
        }

        public void PlayGoodEffect()
        {
            if (goodParticles != null)
            {
                goodParticles.Play();
            }
        }

        public void PlayMissEffect()
        {
            if (missParticles != null)
            {
                missParticles.Play();
            }

            FlashScreen(missFlashColor);
            ScreenShake(0.1f, 0.15f);
        }

        public void PlayFailEffect()
        {
            FlashScreen(missFlashColor);
            ScreenShake(0.3f, 0.5f);
        }

        public void PlaySwipeTrail(Vector2 start, Vector2 end)
        {
            if (swipeTrailParticles != null)
            {
                swipeTrailParticles.transform.position = start;
                swipeTrailParticles.Play();
            }
        }

        public void ScreenShake(float intensity, float duration)
        {
            shakeIntensity = intensity;
            shakeDuration = duration;
            shakeTimer = duration;

            if (mainCamera != null)
            {
                originalCameraPosition = mainCamera.transform.localPosition;
            }
        }

        private void StopScreenShake()
        {
            if (mainCamera != null)
            {
                mainCamera.transform.localPosition = originalCameraPosition;
            }
        }

        public void FlashScreen(Color color)
        {
            if (flashOverlay != null)
            {
                StopAllCoroutines();
                StartCoroutine(FlashCoroutine(color));
            }
        }

        private System.Collections.IEnumerator FlashCoroutine(Color color)
        {
            // Quick fade in
            float elapsed = 0f;
            float fadeInDuration = 0.05f;

            while (elapsed < fadeInDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(0f, color.a, elapsed / fadeInDuration);
                flashOverlay.alpha = alpha;
                yield return null;
            }

            // Fade out
            elapsed = 0f;
            float fadeOutDuration = 0.2f;

            while (elapsed < fadeOutDuration)
            {
                elapsed += Time.deltaTime;
                float alpha = Mathf.Lerp(color.a, 0f, elapsed / fadeOutDuration);
                flashOverlay.alpha = alpha;
                yield return null;
            }

            flashOverlay.alpha = 0f;
        }
    }
}
