using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAwarenessController : MonoBehaviour
{
    public bool AwareOfPlayer { get; private set; }
    public Vector2 DirectionToPlayer { get; private set; }
    public bool IsStunned { get; set; } // EnemyController tarafýndan güncellenecek

    [Header("Awareness")]
    [SerializeField] private float _playerAwarenessDistance = 10f;
    [SerializeField] private float _shootingRange = 7f; // Ateþ menzili eklendi!

    [Header("Movement & Rotation")]
    [SerializeField] private float _chaseSpeed = 3.5f;
    [SerializeField] private float _patrolSpeed = 1.5f;
    [SerializeField] private float _fleeSpeed = 4f; // Kaçma Hýzý eklendi!
    [SerializeField] private float _rotationSpeed = 500f;
    [SerializeField] private float _rotationOffset = 0f;

    [Header("Patrol Settings")]
    [SerializeField] private float _patrolPointThreshold = 0.5f;
    [SerializeField] private float _roomSearchRadius = 20f;
    
    [Header("Behavior Settings")]
    [SerializeField] private bool _isStationary = false; 

    private Transform _playerTransform;
    private float _sqrPlayerAwarenessDistance;

    private NavMeshAgent _agent;
    private Rigidbody2D _rigidbody;
    private PlayerController _playerController;
    private EnemyController _enemyController;

    private enum EnemyState { Idle, Patrol, Chase, SearchWeapon, Flee } 
    private EnemyState _currentState; 
    
    private List<Transform> _currentPatrolPoints = new List<Transform>();
    private int _currentPatrolIndex;
    
    private GameObject _targetDroppedWeapon; // Aranýlan yerdeki silah

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _enemyController = GetComponent<EnemyController>(); // EKLENDÝ

        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        _agent.speed = _patrolSpeed;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) 
        {
            _playerTransform = pc.transform;
            _playerController = pc; 
        }

        _sqrPlayerAwarenessDistance = _playerAwarenessDistance * _playerAwarenessDistance;

        FindPatrolPointsInCurrentRoom();

        if (_isStationary)
        {
            _currentState = EnemyState.Idle; 
            _agent.isStopped = true; 
        }
        else
        {
            _currentState = EnemyState.Patrol; 
        }
    }

    private void Update()
    {
        CheckPlayerAwareness();

        if (IsStunned || !_agent.enabled || !_agent.isOnNavMesh) return;

        HandleStateTransitions();
        ExecuteCurrentState();
        RotateTowardsMovement();
    }

    private void CheckPlayerAwareness()
    {
        if (_playerTransform == null || (_playerController != null && _playerController.IsDead)) 
        {
            AwareOfPlayer = false;
            return;
        }

        Vector2 enemyToPlayerVector = _playerTransform.position - transform.position;

        if (enemyToPlayerVector.sqrMagnitude <= _sqrPlayerAwarenessDistance)
        {
            RaycastHit2D hit = Physics2D.Linecast(transform.position, _playerTransform.position, LayerMask.GetMask("Obstacle", "Door"));

            if (hit.collider == null)
            {
                AwareOfPlayer = true;
                DirectionToPlayer = enemyToPlayerVector.normalized;
            }
            else
            {
                AwareOfPlayer = false;
            }
        }
        else
        {
            AwareOfPlayer = false;
        }
    }

    private void HandleStateTransitions()
    {
        // 1. Silahsýzsa Silahý Arama veya Kaçma Davranýþý (FLEE / SEARCH WEAPON)
        if (!_enemyController.HasWeapon)
        {
            TryFindNearestDroppedWeapon();
            
            if (_targetDroppedWeapon != null)
            {
                _currentState = EnemyState.SearchWeapon; // Silah bulunduysa ona koþ
            }
            else if (AwareOfPlayer)
            {
                _currentState = EnemyState.Flee; // Silah yoksa ve Player yakýnsa kaç
            }
            else
            {
                _currentState = EnemyState.Idle; // Oyuncu yok silah da yok öylece bekle
            }
        }
        // 2. Silahý Varsa ve Player Görülüyorsa -> CHASE
        else if (AwareOfPlayer && _currentState != EnemyState.Chase)
        {
            _currentState = EnemyState.Chase;
        }
        // 3. Player'ý Kaybettiyse Normal Devriyeye Dön
        else if (!AwareOfPlayer && _currentState == EnemyState.Chase)
        {
            _currentState = _isStationary ? EnemyState.Idle : EnemyState.Patrol;
            if (!_isStationary) FindPatrolPointsInCurrentRoom(); 
        }
    }
    
    private void TryFindNearestDroppedWeapon()
    {
        Collider2D[] foundItems = Physics2D.OverlapCircleAll(transform.position, _roomSearchRadius);
        float closestDistance = Mathf.Infinity;
        GameObject closestWeapon = null;

        foreach (Collider2D item in foundItems)
        {
            if (item.GetComponent<DropPistol>() != null) // Yerdeki silah mý?
            {
                // Silaha doðru arada baþka bloklayýcý duvar var mý kontrol et
                RaycastHit2D hit = Physics2D.Linecast(transform.position, item.transform.position, LayerMask.GetMask("Obstacle", "Door"));
                if (hit.collider == null)
                {
                    float distance = Vector2.Distance(transform.position, item.transform.position);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestWeapon = item.gameObject;
                    }
                }
            }
        }
        _targetDroppedWeapon = closestWeapon;
    }

    private void ExecuteCurrentState()
    {
        if (_currentState == EnemyState.Idle)
        {
            _agent.isStopped = true; 
        }
        else if (_currentState == EnemyState.Chase)
        {
            _agent.speed = _chaseSpeed; 
            _agent.isStopped = false;
            
            if (_playerTransform != null) 
            {
                float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);
                
                // Oyuncu menzildeyse durup ateþ et
                if (distanceToPlayer <= _shootingRange)
                {
                    _agent.isStopped = true; 
                    _enemyController.FireWeapon(); // ATEÞ ET
                }
                else
                {
                    _agent.isStopped = false;
                    _agent.SetDestination(_playerTransform.position);
                }
            }
        }
        else if (_currentState == EnemyState.SearchWeapon)
        {
            if (_targetDroppedWeapon == null) return; // Baþkasý almýþ veya kaybolmuþ olabilir

            _agent.speed = _chaseSpeed; 
            _agent.isStopped = false;
            _agent.SetDestination(_targetDroppedWeapon.transform.position);

            // Silaha yeterince yakýnsa al
            if (Vector2.Distance(transform.position, _targetDroppedWeapon.transform.position) <= 1.5f)
            {
                _enemyController.EquipWeapon(_targetDroppedWeapon);
                _targetDroppedWeapon = null; 
            }
        }
        else if (_currentState == EnemyState.Flee)
        {
            _agent.speed = _fleeSpeed; 
            _agent.isStopped = false;

            if (_playerTransform != null)
            {
                // Oyuncunun tersi yönünü hesapla ve oraya koþ
                Vector3 fleeDirection = (transform.position - _playerTransform.position).normalized;
                Vector3 fleeTarget = transform.position + fleeDirection * 5f;
                _agent.SetDestination(fleeTarget);
            }
        }
        else if (_currentState == EnemyState.Patrol)
        {
            _agent.speed = _patrolSpeed; // Devriye hýzýna geç
            
            if (_currentPatrolPoints.Count == 0) return;

            _agent.isStopped = false;
            Transform targetPoint = _currentPatrolPoints[_currentPatrolIndex];
            _agent.SetDestination(targetPoint.position);

            // Noktaya vardýysa sýradakine geç
            if (Vector2.Distance(transform.position, targetPoint.position) < _patrolPointThreshold)
            {
                _currentPatrolIndex = (_currentPatrolIndex + 1) % _currentPatrolPoints.Count;
            }
        }
    }

    private void FindPatrolPointsInCurrentRoom()
    {
        _currentPatrolPoints.Clear();
        GameObject[] allPoints = GameObject.FindGameObjectsWithTag("PatrolPoint");

        foreach (GameObject point in allPoints)
        {
            float distance = Vector2.Distance(transform.position, point.transform.position);

            if (distance <= _roomSearchRadius) // Sadece belli bir yarýçaptakileri kontrol et
            {
                // Arada engel (Obstacle) yoksa listeye dahil et
                // Burada da Inspector üzerinden belirlediðimiz LayerMask'i kullanmak daha tutarlý olur
                RaycastHit2D hit = Physics2D.Linecast(transform.position, point.transform.position, LayerMask.GetMask("Obstacle", "Door"));

                if (hit.collider == null)
                {
                    _currentPatrolPoints.Add(point.transform);
                }
            }
        }
        
        // Yeni bir devriye listesi oluþtuysa rastgele birinden baþla
        if (_currentPatrolPoints.Count > 0)
        {
             _currentPatrolIndex = Random.Range(0, _currentPatrolPoints.Count);
        }
    }

    private void RotateTowardsMovement()
    {
        // 1. DURUM: Kovalama modunda ve ateþ etmek için durmuþsak direkt oyuncuya dön
        if (_currentState == EnemyState.Chase && _agent.isStopped && _playerTransform != null)
        {
            Vector2 targetDir = (_playerTransform.position - transform.position).normalized;
            float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
            float newAngle = Mathf.MoveTowardsAngle(_rigidbody.rotation, angle + _rotationOffset, _rotationSpeed * Time.deltaTime);
            _rigidbody.SetRotation(newAngle);
        }
        // 2. DURUM: Normal hareket ediyorsa NavMesh'in hareket yönüne dön
        else if (_agent.desiredVelocity.sqrMagnitude > 0.01f)
        {
            Vector2 targetDir = _agent.desiredVelocity;
            float angle = Mathf.Atan2(targetDir.y, targetDir.x) * Mathf.Rad2Deg;
            float newAngle = Mathf.MoveTowardsAngle(_rigidbody.rotation, angle + _rotationOffset, _rotationSpeed * Time.deltaTime);
            _rigidbody.SetRotation(newAngle);
        }
    }

    public void SetAgentEnabled(bool isEnabled)
    {
        if (_agent != null) _agent.enabled = isEnabled;
    }
    
    public Vector2 GetAgentVelocity()
    {
        return _agent != null ? (Vector2)_agent.velocity : Vector2.zero;
    }

    private void OnDrawGizmosSelected()
    {
        // 1. Oyuncuyu Fark Etme (Awareness) Alaný - Kýrmýzý Çember
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _playerAwarenessDistance);

        // 2. Devriye Noktalarýný Tarama Alaný (Room Search) - Mavi Çember
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _roomSearchRadius);

        // Eðer oyun çalýþýyorsa hedeflere doðru çizgiler çiz
        if (Application.isPlaying)
        {
            // Kovalama Modu: Oyuncuya sarý bir çizgi çeker
            if (_currentState == EnemyState.Chase && _playerTransform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _playerTransform.position);
            }
            // Devriye Modu: Gittiði hedefe yeþil bir çizgi çeker
            else if (_currentState == EnemyState.Patrol && _currentPatrolPoints.Count > 0)
            {
                Transform targetPoint = _currentPatrolPoints[_currentPatrolIndex];
                if (targetPoint != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, targetPoint.position);
                    Gizmos.DrawWireSphere(targetPoint.position, 0.3f); // Hedef noktayý da küçük bir topla belirginleþtir
                }
            }
        }
    }
}
