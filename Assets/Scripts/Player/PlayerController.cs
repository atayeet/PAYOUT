using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [SerializeField]
    private float _speed;

    [SerializeField]
    private float _smoothTime = 0.1f;

    [SerializeField]
    private float _rotationOffset = 0f; // Eðer sprite'ýnýz ters ise bunu 90 veya -90 yapýn

    private Rigidbody2D _rigidbody;
    private Camera _mainCamera;
    private Animator _animator;

    public Weapon _weapon; // Silah referansý

    private Vector2 _movementInput;
    private Vector2 _currentVelocity;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody2D>();
        _mainCamera = Camera.main; // Fare pozisyonunu çevirmek için ana kamera
        _animator = GetComponent<Animator>();
        _weapon = GetComponentInChildren<Weapon>(); // Silah bileþenini çocuklardan bul
    }

    private void Update()
    {
        // Farenin ekrandaki pozisyonunu al (Yeni Input System gerektirir)
        if (Mouse.current != null)
        {
            Vector2 mouseScreenPosition = Mouse.current.position.ReadValue();
            // Ekran koordinatýný dünya koordinatýna çevir
            Vector3 mouseWorldPosition = _mainCamera.ScreenToWorldPoint(mouseScreenPosition);
            
            // Karakterden fareye doðru olan yön vektörünü hesapla
            Vector2 lookDirection = mouseWorldPosition - transform.position;

            // Bakýþ yönünün açýsýný (Atan2 ile) hesapla ve dereceye çevir
            float angle = Mathf.Atan2(lookDirection.y, lookDirection.x) * Mathf.Rad2Deg;

            // Rigidbody'nin dönüþ açýsýný güncelle
            _rigidbody.rotation = angle + _rotationOffset;
        }

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            _weapon.Fire(); // Ateþ et
        }
    }

    private void FixedUpdate()
    {
        SetAnimation();

        // Hedeflenen hýz
        Vector2 targetVelocity = _movementInput * _speed;

        // Mevcut hýzdan hedeflenen hýza yumuþak bir geçiþ
        _rigidbody.linearVelocity = Vector2.SmoothDamp(
            _rigidbody.linearVelocity,
            targetVelocity,
            ref _currentVelocity,
            _smoothTime);
    }

    private void SetAnimation()
    {
        bool isMoving = _movementInput != Vector2.zero;

        _animator.SetBool("isMoving", isMoving);
    }

    private void OnMove(InputValue inputValue)
    {
        _movementInput = inputValue.Get<Vector2>();
    }
}
