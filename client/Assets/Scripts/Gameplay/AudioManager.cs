using UnityEngine;
using TowerClimb.Core;
using TowerClimb.Gameplay;

namespace TowerClimb.Gameplay
{
    /// <summary>
    /// Manages game audio: SFX for pattern events and background music
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        public AudioSource sfxSource;
        public AudioSource musicSource;

        [Header("Pattern SFX")]
        public AudioClip tapSound;
        public AudioClip swipeSound;
        public AudioClip holdSound;
        public AudioClip rhythmSound;
        public AudioClip tiltSound;

        [Header("Feedback SFX")]
        public AudioClip perfectSound;
        public AudioClip goodSound;
        public AudioClip missSound;
        public AudioClip failSound;

        [Header("UI SFX")]
        public AudioClip buttonClickSound;
        public AudioClip unlockSound;

        [Header("Music")]
        public AudioClip menuMusic;
        public AudioClip gameplayMusic;

        [Header("Settings")]
        [Range(0f, 1f)] public float sfxVolume = 0.8f;
        [Range(0f, 1f)] public float musicVolume = 0.5f;

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
                return;
            }

            // Create audio sources if not assigned
            if (sfxSource == null)
            {
                GameObject sfxObj = new GameObject("SFX Source");
                sfxObj.transform.SetParent(transform);
                sfxSource = sfxObj.AddComponent<AudioSource>();
            }

            if (musicSource == null)
            {
                GameObject musicObj = new GameObject("Music Source");
                musicObj.transform.SetParent(transform);
                musicSource = musicObj.AddComponent<AudioSource>();
                musicSource.loop = true;
            }

            UpdateVolumes();
        }

        private void Start()
        {
            gameStateMachine = GameStateMachine.Instance;

            if (gameStateMachine != null)
            {
                gameStateMachine.OnNewPattern += HandleNewPattern;
                gameStateMachine.OnPatternCompleted += HandlePatternCompleted;
                gameStateMachine.OnStateChanged += HandleStateChanged;
            }

            // Start menu music
            PlayMusic(menuMusic);
        }

        private void OnDestroy()
        {
            if (gameStateMachine != null)
            {
                gameStateMachine.OnNewPattern -= HandleNewPattern;
                gameStateMachine.OnPatternCompleted -= HandlePatternCompleted;
                gameStateMachine.OnStateChanged -= HandleStateChanged;
            }
        }

        private void HandleNewPattern(Pattern pattern)
        {
            // Play pattern-specific sound
            AudioClip clip = GetPatternSound(pattern.type);
            if (clip != null)
            {
                PlaySFX(clip);
            }
        }

        private void HandlePatternCompleted(PatternResult result)
        {
            // Play feedback sound based on accuracy
            AudioClip feedbackClip = null;

            if (!result.success)
            {
                feedbackClip = missSound;
            }
            else if (result.accuracy >= 0.95f)
            {
                feedbackClip = perfectSound;
            }
            else if (result.accuracy >= 0.7f)
            {
                feedbackClip = goodSound;
            }

            if (feedbackClip != null)
            {
                PlaySFX(feedbackClip);
            }
        }

        private void HandleStateChanged(GameState oldState, GameState newState)
        {
            switch (newState)
            {
                case GameState.Idle:
                    PlayMusic(menuMusic);
                    break;

                case GameState.PlayingFloor:
                    if (musicSource.clip != gameplayMusic)
                    {
                        PlayMusic(gameplayMusic);
                    }
                    break;

                case GameState.Failed:
                    PlaySFX(failSound);
                    break;
            }
        }

        private AudioClip GetPatternSound(PatternType type)
        {
            switch (type)
            {
                case PatternType.Tap:
                case PatternType.DoubleTap:
                    return tapSound;

                case PatternType.Swipe:
                    return swipeSound;

                case PatternType.Hold:
                    return holdSound;

                case PatternType.Rhythm:
                    return rhythmSound;

                case PatternType.Tilt:
                    return tiltSound;

                default:
                    return null;
            }
        }

        public void PlaySFX(AudioClip clip, float volumeMultiplier = 1f)
        {
            if (clip != null && sfxSource != null)
            {
                sfxSource.PlayOneShot(clip, sfxVolume * volumeMultiplier);
            }
        }

        public void PlayMusic(AudioClip clip)
        {
            if (clip != null && musicSource != null)
            {
                if (musicSource.clip != clip)
                {
                    musicSource.clip = clip;
                    musicSource.Play();
                }
            }
        }

        public void PlayButtonClick()
        {
            PlaySFX(buttonClickSound);
        }

        public void PlayUnlock()
        {
            PlaySFX(unlockSound);
        }

        public void SetSFXVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            musicVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
        }

        private void UpdateVolumes()
        {
            if (sfxSource != null)
            {
                sfxSource.volume = sfxVolume;
            }

            if (musicSource != null)
            {
                musicSource.volume = musicVolume;
            }
        }
    }
}
