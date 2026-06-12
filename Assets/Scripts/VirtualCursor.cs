using UnityEngine;
using UnityEngine.InputSystem;

public class VirtualCursor : MonoBehaviour
{
    [Header("Cursor Settings")]
    [SerializeField] private float _mouseSensitivity = 1.0f; // Farenin dünya hareket hassasiyeti
    [SerializeField] private float _maxAimDistance = 7f;     // İmlecin oyuncudan ne kadar uzaklaşabileceği

    [Header("Camera Settings")]
    [SerializeField] private bool _enableDynamicCamera = true;
    [SerializeField] private float _cameraFollowSpeed = 5f;
    [SerializeField] private float _cameraMaxDistance = 4f; 

    [Header("Look Ahead Settings (SHIFT)")]
    [SerializeField] private float _lookAheadMaxAimDistance = 12f;     // SHIFT basılıyken imlecin oyuncudan ne kadar uzaklaşabileceği
    [SerializeField] private float _lookAheadCameraMaxDistance = 8f;   // SHIFT basılıyken kameranın oyuncudan ne kadar uzaklaşabileceği
    [SerializeField] private float _lookAheadTransitionSpeed = 5f;     // Görüş açısı geçiş hızı

    private Camera _mainCamera;
    private PlayerController _player;
    
    // İmlecin Player'a göre DÜNYA üzerindeki mesafesi
    private Vector3 _aimOffset;

    private float _currentMaxAimDistance;
    private float _currentCameraMaxDistance;

    private Transform _cameraAnchor;

    public bool IsLookingAhead => Keyboard.current != null && (Keyboard.current.leftShiftKey.isPressed || Keyboard.current.rightShiftKey.isPressed);

    private void Awake()
    {
        _mainCamera = Camera.main;
        _player = Object.FindAnyObjectByType<PlayerController>();

        LockSystemCursor();
        
        // Oyun başında imleç hafif sağ tarafta bir ofsetle başlasın
        _aimOffset = new Vector3(2f, 0f, 0f);

        _currentMaxAimDistance = _maxAimDistance;
        _currentCameraMaxDistance = _cameraMaxDistance;

        // Kameranın takip edeceği bir Anchor (Çapa) objesi oluşturuyoruz
        GameObject anchorObj = new GameObject("CameraAnchor");
        _cameraAnchor = anchorObj.transform;
        if (_player != null)
        {
            _cameraAnchor.position = _player.transform.position;
        }
    }

    private void Start()
    {
        // Cinemachine kamerasını bulup takip hedefini bu Anchor yapıyoruz
        var vcam = Object.FindAnyObjectByType<Unity.Cinemachine.CinemachineCamera>();
        if (vcam != null)
        {
            vcam.Follow = _cameraAnchor;
            vcam.LookAt = null; // 2D'de rotasyon bozulmalarını önlemek için LookAt'i null yapıyoruz
        }
    }

    private void Update()
    {
        // Oyuncu öldüyse veya oyun durduysa imleci güncellemeyi kes
        if (PauseMenuManager.GameIsPaused || _player == null) return;

        UpdateLookAheadValues();
        UpdateCursorPosition();
        UpdateDynamicCamera();
    }

    private void UpdateLookAheadValues()
    {
        float targetMaxAim = IsLookingAhead ? _lookAheadMaxAimDistance : _maxAimDistance;
        float targetCamMax = IsLookingAhead ? _lookAheadCameraMaxDistance : _cameraMaxDistance;

        _currentMaxAimDistance = Mathf.Lerp(_currentMaxAimDistance, targetMaxAim, Time.deltaTime * _lookAheadTransitionSpeed);
        _currentCameraMaxDistance = Mathf.Lerp(_currentCameraMaxDistance, targetCamMax, Time.deltaTime * _lookAheadTransitionSpeed);
    }

    private void UpdateCursorPosition()
    {
        // 1. Ekrandaki farenin saf hareket(delta) miktarını alıyoruz
        if (Mouse.current != null)
        {
            Vector2 mouseDelta = Mouse.current.delta.ReadValue();
            
            // Farenin pixel hareketini Dünya(World) birimlerine uygun çarpanla oranlıyoruz
            float pixelsToWorld = (_mainCamera.orthographicSize * 2f) / Screen.height;
            
            Vector3 worldDelta = new Vector3(mouseDelta.x, mouseDelta.y, 0f) * pixelsToWorld;
            
            // Farenin bu hareketini oyuncu etrafındaki Aim mesafesine ekle
            _aimOffset += worldDelta * _mouseSensitivity;
        }

        // 2. İmlecin (Nişangahın) oyuncudan en fazla ne kadar uzağa gidebileceğini kestiğimiz alan
        _aimOffset = Vector3.ClampMagnitude(_aimOffset, _currentMaxAimDistance);

        // 3. FINAL POZİSYON:
        // İmleç her daim = (Oyuncunun Pozisyonu) + (Nişan Uzaklığı)
        // Bu kod, Player WASD ile haritayı gezerken, imlecin Player ile %100 aynı hızda 
        // senkronize bir şekilde dünyada hareket etmesini sağlar.
        transform.position = _player.transform.position + _aimOffset;
    }

    private void UpdateDynamicCamera()
    {
        if (_player != null && _cameraAnchor != null)
        {
            Vector3 targetCamPos;

            if (_enableDynamicCamera && IsLookingAhead)
            {
                // SHIFT basılıyken: Oyuncu ile Cursor arasındaki orta noktayı bul
                Vector3 midPoint = (_player.transform.position + transform.position) / 2f;
                Vector3 offset = midPoint - _player.transform.position;
                offset = Vector3.ClampMagnitude(offset, _currentCameraMaxDistance);
                targetCamPos = _player.transform.position + offset;
            }
            else
            {
                // Normal durumda: Kamera sadece oyuncunun üzerinde kilitli kalır
                targetCamPos = _player.transform.position;
            }
            
            // 2D'de Z derinliğini korumak için 0f yapıyoruz, Cinemachine Follow Offset (-10 vb.) derinliği yönetir
            targetCamPos.z = 0f;
 
            // Çapayı (Anchor) yumuşakça (ve hafif gecikmeli, estetik his için) hedef pozisyona taşıyoruz
            _cameraAnchor.position = Vector3.Lerp(_cameraAnchor.position, targetCamPos, Time.deltaTime * _cameraFollowSpeed);
        }
    }

    private void LockSystemCursor()
    {
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Confined;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && !PauseMenuManager.GameIsPaused)
        {
            LockSystemCursor();
        }
    }
}