using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Audio;

public class SettingsMenuController : MonoBehaviour
{
    [SerializeField] private AudioMixer _audioMixer;
    [SerializeField] private Slider _musicSlider;
    [SerializeField] private Slider _sfxSlider;

    [Header("UI Panel Controls")]
    [SerializeField] private GameObject _settingsPanel;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private Button _backButton;

    private const string MusicParam = "MusicVolume";
    private const string SFXParam = "SFXVolume";

    private void Start()
    {
        // Load values from PlayerPrefs, default to 0.75f if not set
        float savedMusic = PlayerPrefs.GetFloat(MusicParam, 0.75f);
        float savedSFX = PlayerPrefs.GetFloat(SFXParam, 0.75f);

        if (_musicSlider != null)
        {
            _musicSlider.minValue = 0.0001f;
            _musicSlider.maxValue = 1f;
            _musicSlider.value = savedMusic;
            _musicSlider.onValueChanged.AddListener(SetMusicVolume);
        }

        if (_sfxSlider != null)
        {
            _sfxSlider.minValue = 0.0001f;
            _sfxSlider.maxValue = 1f;
            _sfxSlider.value = savedSFX;
            _sfxSlider.onValueChanged.AddListener(SetSFXVolume);
        }

        // Apply volumes on start
        SetVolume(MusicParam, savedMusic);
        SetVolume(SFXParam, savedSFX);

        // Setup panel toggles
        if (_settingsButton != null && _settingsPanel != null)
        {
            _settingsButton.onClick.AddListener(() => OpenPanel());
        }

        if (_backButton != null && _settingsPanel != null)
        {
            _backButton.onClick.AddListener(() => ClosePanel());
        }

        // Ensure panel is closed at start
        if (_settingsPanel != null)
        {
            _settingsPanel.SetActive(false);
        }
    }

    private void Update()
    {
        if (_settingsPanel != null && _settingsPanel.activeSelf)
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                // In game, PauseMenuManager handles the escape key to avoid double-processing or resumption
                if (Object.FindAnyObjectByType<PauseMenuManager>() == null)
                {
                    ClosePanel();
                }
            }
        }
    }

    private void OpenPanel()
    {
        if (_settingsPanel != null) _settingsPanel.SetActive(true);
    }

    private void ClosePanel()
    {
        if (_settingsPanel != null) _settingsPanel.SetActive(false);
    }

    public void SetMusicVolume(float value)
    {
        PlayerPrefs.SetFloat(MusicParam, value);
        SetVolume(MusicParam, value);
    }

    public void SetSFXVolume(float value)
    {
        PlayerPrefs.SetFloat(SFXParam, value);
        SetVolume(SFXParam, value);
    }

    private void SetVolume(string parameterName, float value)
    {
        if (_audioMixer == null) return;

        // Convert linear 0-1 value to logarithmic decibels (-80dB to 0dB)
        float db = Mathf.Log10(value) * 20f;
        _audioMixer.SetFloat(parameterName, db);
    }
}