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

    private Camera _mainCamera;
    private PlayerController _player;
    
    // İmlecin Player'a göre DÜNYA üzerindeki mesafesi
    private Vector3 _aimOffset;

    private void Awake()
    {
        _mainCamera = Camera.main;
        _player = Object.FindFirstObjectByType<PlayerController>();

        LockSystemCursor();
        
        // Oyun başında imleç hafif sağ tarafta bir ofsetle başlasın
        _aimOffset = new Vector3(2f, 0f, 0f);
    }

    private void Update()
    {
        // Oyuncu öldüyse veya oyun durduysa imleci güncellemeyi kes
        if (PauseMenuManager.GameIsPaused || _player == null) return;

        UpdateCursorPosition();
        UpdateDynamicCamera();
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
        _aimOffset = Vector3.ClampMagnitude(_aimOffset, _maxAimDistance);

        // 3. FINAL POZİSYON:
        // İmleç her daim = (Oyuncunun Pozisyonu) + (Nişan Uzaklığı)
        // Bu kod, Player WASD ile haritayı gezerken, imlecin Player ile %100 aynı hızda 
        // senkronize bir şekilde dünyada hareket etmesini sağlar.
        transform.position = _player.transform.position + _aimOffset;
    }

    private void UpdateDynamicCamera()
    {
        if (_enableDynamicCamera && _player != null)
        {
            // Oyuncu ile Cursor arasındaki orta noktayı bul
            Vector3 midPoint = (_player.transform.position + transform.position) / 2f;
            
            // Kameranın oyuncudan çok fazla uzaklaşmaması için kilit noktası
            Vector3 offset = midPoint - _player.transform.position;
            offset = Vector3.ClampMagnitude(offset, _cameraMaxDistance);
            Vector3 targetCamPos = _player.transform.position + offset;
            
            targetCamPos.z = _mainCamera.transform.position.z; // Z ekseninde kamerayı bozmamak için

            // Kamerayı yumuşakça (ve hafif gecikmeli, estetik his için) oraya taşı
            _mainCamera.transform.position = Vector3.Lerp(_mainCamera.transform.position, targetCamPos, Time.deltaTime * _cameraFollowSpeed);
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