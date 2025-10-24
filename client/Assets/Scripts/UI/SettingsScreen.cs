using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TowerClimb.UI
{
    /// <summary>
    /// Settings screen for audio, vibration, and accessibility options
    /// </summary>
    public class SettingsScreen : MonoBehaviour
    {
        [Header("UI References")]
        public Button closeButton;

        [Header("Audio Settings")]
        public Slider musicVolumeSlider;
        public Slider sfxVolumeSlider;
        public TextMeshProUGUI musicVolumeText;
        public TextMeshProUGUI sfxVolumeText;

        [Header("Gameplay Settings")]
        public Toggle vibrationToggle;
        public Toggle colorBlindModeToggle;

        [Header("Account")]
        public Button logoutButton;
        public TextMeshProUGUI userHandleText;

        [Header("Info")]
        public TextMeshProUGUI versionText;
        public Button creditsButton;
        public Button privacyPolicyButton;

        private const string PREF_MUSIC_VOLUME = "Settings_MusicVolume";
        private const string PREF_SFX_VOLUME = "Settings_SFXVolume";
        private const string PREF_VIBRATION = "Settings_Vibration";
        private const string PREF_COLORBLIND = "Settings_ColorBlind";

        private void Start()
        {
            // Setup close button
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseClicked);
            }

            // Setup audio sliders
            if (musicVolumeSlider != null)
            {
                musicVolumeSlider.minValue = 0f;
                musicVolumeSlider.maxValue = 1f;
                musicVolumeSlider.value = LoadMusicVolume();
                musicVolumeSlider.onValueChanged.AddListener(OnMusicVolumeChanged);
            }

            if (sfxVolumeSlider != null)
            {
                sfxVolumeSlider.minValue = 0f;
                sfxVolumeSlider.maxValue = 1f;
                sfxVolumeSlider.value = LoadSFXVolume();
                sfxVolumeSlider.onValueChanged.AddListener(OnSFXVolumeChanged);
            }

            // Setup toggles
            if (vibrationToggle != null)
            {
                vibrationToggle.isOn = LoadVibrationEnabled();
                vibrationToggle.onValueChanged.AddListener(OnVibrationToggled);
            }

            if (colorBlindModeToggle != null)
            {
                colorBlindModeToggle.isOn = LoadColorBlindMode();
                colorBlindModeToggle.onValueChanged.AddListener(OnColorBlindModeToggled);
            }

            // Setup account buttons
            if (logoutButton != null)
            {
                logoutButton.onClick.AddListener(OnLogoutClicked);
            }

            // Setup info buttons
            if (creditsButton != null)
            {
                creditsButton.onClick.AddListener(OnCreditsClicked);
            }

            if (privacyPolicyButton != null)
            {
                privacyPolicyButton.onClick.AddListener(OnPrivacyPolicyClicked);
            }

            // Set version text
            if (versionText != null)
            {
                versionText.text = $"Version {Application.version}";
            }

            UpdateUI();
            Hide();
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

        private void UpdateUI()
        {
            // Update volume texts
            if (musicVolumeText != null && musicVolumeSlider != null)
            {
                musicVolumeText.text = $"{(int)(musicVolumeSlider.value * 100)}%";
            }

            if (sfxVolumeText != null && sfxVolumeSlider != null)
            {
                sfxVolumeText.text = $"{(int)(sfxVolumeSlider.value * 100)}%";
            }

            // Update user handle (if available)
            if (userHandleText != null)
            {
                string handle = PlayerPrefs.GetString("UserHandle", "Guest");
                userHandleText.text = handle;
            }
        }

        #region Audio Settings

        private void OnMusicVolumeChanged(float value)
        {
            SaveMusicVolume(value);
            ApplyMusicVolume(value);

            if (musicVolumeText != null)
            {
                musicVolumeText.text = $"{(int)(value * 100)}%";
            }
        }

        private void OnSFXVolumeChanged(float value)
        {
            SaveSFXVolume(value);
            ApplySFXVolume(value);

            if (sfxVolumeText != null)
            {
                sfxVolumeText.text = $"{(int)(value * 100)}%";
            }
        }

        private void ApplyMusicVolume(float volume)
        {
            // TODO: Wire up to AudioManager
            // AudioManager.Instance?.SetMusicVolume(volume);
            Debug.Log($"[SettingsScreen] Music volume set to {volume}");
        }

        private void ApplySFXVolume(float volume)
        {
            // TODO: Wire up to AudioManager
            // AudioManager.Instance?.SetSFXVolume(volume);
            Debug.Log($"[SettingsScreen] SFX volume set to {volume}");
        }

        private float LoadMusicVolume()
        {
            return PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, 0.7f);
        }

        private float LoadSFXVolume()
        {
            return PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 0.8f);
        }

        private void SaveMusicVolume(float volume)
        {
            PlayerPrefs.SetFloat(PREF_MUSIC_VOLUME, volume);
            PlayerPrefs.Save();
        }

        private void SaveSFXVolume(float volume)
        {
            PlayerPrefs.SetFloat(PREF_SFX_VOLUME, volume);
            PlayerPrefs.Save();
        }

        #endregion

        #region Gameplay Settings

        private void OnVibrationToggled(bool enabled)
        {
            SaveVibrationEnabled(enabled);
            Debug.Log($"[SettingsScreen] Vibration {(enabled ? "enabled" : "disabled")}");
        }

        private void OnColorBlindModeToggled(bool enabled)
        {
            SaveColorBlindMode(enabled);
            ApplyColorBlindMode(enabled);
            Debug.Log($"[SettingsScreen] Color-blind mode {(enabled ? "enabled" : "disabled")}");
        }

        private void ApplyColorBlindMode(bool enabled)
        {
            // TODO: Update color palette for pattern indicators
            // Could change green/red to blue/orange or add pattern shapes
            Debug.Log($"[SettingsScreen] Applying color-blind mode: {enabled}");
        }

        private bool LoadVibrationEnabled()
        {
            return PlayerPrefs.GetInt(PREF_VIBRATION, 1) == 1;
        }

        private bool LoadColorBlindMode()
        {
            return PlayerPrefs.GetInt(PREF_COLORBLIND, 0) == 1;
        }

        private void SaveVibrationEnabled(bool enabled)
        {
            PlayerPrefs.SetInt(PREF_VIBRATION, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        private void SaveColorBlindMode(bool enabled)
        {
            PlayerPrefs.SetInt(PREF_COLORBLIND, enabled ? 1 : 0);
            PlayerPrefs.Save();
        }

        #endregion

        #region Account

        private void OnLogoutClicked()
        {
            Debug.Log("[SettingsScreen] Logout clicked");

            // Clear saved auth tokens
            PlayerPrefs.DeleteKey("SupabaseAccessToken");
            PlayerPrefs.DeleteKey("SupabaseRefreshToken");
            PlayerPrefs.DeleteKey("UserHandle");
            PlayerPrefs.Save();

            // TODO: Return to login screen or prompt re-auth
            // SceneManager.LoadScene("LoginScene");
        }

        #endregion

        #region Info

        private void OnCreditsClicked()
        {
            Debug.Log("[SettingsScreen] Credits clicked");
            // TODO: Show credits popup or screen
        }

        private void OnPrivacyPolicyClicked()
        {
            Debug.Log("[SettingsScreen] Privacy policy clicked");
            // TODO: Open URL to privacy policy
            // Application.OpenURL("https://yourgame.com/privacy");
        }

        #endregion

        private void OnCloseClicked()
        {
            Hide();

            // Return to home screen
            if (HomeScreen.Instance != null)
            {
                HomeScreen.Instance.Show();
            }
        }

        /// <summary>
        /// Public getter methods for other systems to read settings
        /// </summary>
        public static float GetMusicVolume()
        {
            return PlayerPrefs.GetFloat(PREF_MUSIC_VOLUME, 0.7f);
        }

        public static float GetSFXVolume()
        {
            return PlayerPrefs.GetFloat(PREF_SFX_VOLUME, 0.8f);
        }

        public static bool IsVibrationEnabled()
        {
            return PlayerPrefs.GetInt(PREF_VIBRATION, 1) == 1;
        }

        public static bool IsColorBlindModeEnabled()
        {
            return PlayerPrefs.GetInt(PREF_COLORBLIND, 0) == 1;
        }
    }
}
