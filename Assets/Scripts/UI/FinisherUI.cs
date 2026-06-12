using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FinisherUI : MonoBehaviour
{
    [SerializeField] private UnityEngine.UI.Slider _staminaSlider;
    [SerializeField] private UnityEngine.UI.Image _fillImage;
    [SerializeField] private TMPro.TextMeshProUGUI _readyText;

    [Header("Visual Colors")]
    [SerializeField] private Color _chargingColor = new Color(0.2f, 0.8f, 1f); // Cyan/Light Blue
    [SerializeField] private Color _readyColor = new Color(1f, 0.8f, 0f);     // Gold/Yellow

    private PlayerController _player;

    private void Start()
    {
        _player = Object.FindAnyObjectByType<PlayerController>();
        if (_staminaSlider != null)
        {
            _staminaSlider.minValue = 0f;
            _staminaSlider.maxValue = 1f;
        }
    }

    private void Update()
    {
        if (_player == null)
        {
            _player = Object.FindAnyObjectByType<PlayerController>();
            return;
        }

        float ratio = _player.FinisherStaminaRatio;

        if (_staminaSlider != null)
        {
            _staminaSlider.value = ratio;
        }

        bool isReady = ratio >= 1f;

        if (_fillImage != null)
        {
            _fillImage.color = isReady ? _readyColor : _chargingColor;
        }

        if (_readyText != null)
        {
            _readyText.gameObject.SetActive(isReady);
        }
    }
}
