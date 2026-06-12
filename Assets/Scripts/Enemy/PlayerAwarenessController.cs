using UnityEngine;
using UnityEngine.AI;
using System.Collections.Generic;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerAwarenessController : MonoBehaviour
{
    public bool AwareOfPlayer { get; private set; }
    public Vector2 DirectionToPlayer { get; private set; }
    public bool IsStunned { get; set; } // EnemyController taraf ndan g ncellenecek

    [Header("Awareness")]
    [SerializeField] private float _playerAwarenessDistance = 10f;
    [SerializeField] private float _shootingRange = 7f; // Ate  menzili eklendi!

    [Header("Movement & Rotation")]
    [SerializeField] private float _chaseSpeed = 3.5f;
    [SerializeField] private float _patrolSpeed = 1.5f;
    [SerializeField] private float _fleeSpeed = 4f; // Ka ma H z  eklendi!
    [SerializeField] private float _rotationSpeed = 500f;
    [SerializeField] private float _rotationOffset = 0f;

    [Header("Patrol Settings")]
    [SerializeField] private float _patrolPointThreshold = 0.5f;
    [SerializeField] private float _roomSearchRadius = 20f;
    
    [Header("Behavior Settings")]
    [SerializeField] private bool _isStationary = false; 

    private Transform _playerTransform;
    private float _sqrPlayerAwarenessDistance;

    private Vector2 _lastSeenPosition;
    private bool _hasLastSeenPosition;

    private NavMeshAgent _agent;
    private Rigidbody2D _rigidbody;
    private PlayerController _playerController;
    private EnemyController _enemyController;

    private enum EnemyState { Idle, Patrol, Alert, Chase, SearchWeapon, Flee, InvestigateLastSeen } 
    private EnemyState _currentState; 
    private float _alertTimer;
    
    private List<Transform> _currentPatrolPoints = new List<Transform>();
    private int _currentPatrolIndex;
    
    private GameObject _targetDroppedWeapon; // Aran lan yerdeki silah

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rigidbody = GetComponent<Rigidbody2D>();
        _enemyController = GetComponent<EnemyController>(); // EKLEND 

        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        _agent.speed = _patrolSpeed;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        PlayerController pc = Object.FindAnyObjectByType<PlayerController>();
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
                _lastSeenPosition = _playerTransform.position;
                _hasLastSeenPosition = true;
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
        // 1. Silahsızsa Silahı Arama veya Kaçma Davranışı (FLEE / SEARCH WEAPON)
        if (!_enemyController.HasWeapon)
        {
            TryFindNearestDroppedWeapon();
            
            if (_targetDroppedWeapon != null)
            {
                _currentState = EnemyState.SearchWeapon; // Silah bulunduysa ona koş
            }
            else if (AwareOfPlayer)
            {
                _currentState = EnemyState.Flee; // Silah yoksa ve Player yakınsa kaç
            }
            else
            {
                _currentState = EnemyState.Idle; // Oyuncu yok silah da yok öylece bekle
            }
        }
        // 2. Silahı Varsa ve Player Görülüyorsa
        else if (AwareOfPlayer)
        {
            if (_currentState != EnemyState.Chase && _currentState != EnemyState.Alert)
            {
                if (_currentState == EnemyState.InvestigateLastSeen)
                {
                    _currentState = EnemyState.Chase;
                }
                else
                {
                    _currentState = EnemyState.Alert;
                    float duration = 0.5f;
                    if (DifficultyManager.Instance != null)
                    {
                        duration = DifficultyManager.Instance.GetAlertDuration();
                    }
                    _alertTimer = duration;
                }
            }
        }
        // 3. Player'ı Kaybettiyse En Son Görüldüğü Yere Git veya Normal Devriyeye Dön
        else if (!AwareOfPlayer && (_currentState == EnemyState.Chase || _currentState == EnemyState.Alert))
        {
            if (_hasLastSeenPosition)
            {
                _currentState = EnemyState.InvestigateLastSeen;
            }
            else
            {
                _currentState = _isStationary ? EnemyState.Idle : EnemyState.Patrol;
                if (!_isStationary) FindPatrolPointsInCurrentRoom(); 
            }
        }
    }
    
    private void TryFindNearestDroppedWeapon()
    {
        Collider2D[] foundItems = Physics2D.OverlapCircleAll(transform.position, _roomSearchRadius);
        float closestDistance = Mathf.Infinity;
        GameObject closestWeapon = null;

        foreach (Collider2D item in foundItems)
        {
            if (item.GetComponent<DropPistol>() != null) // Yerdeki silah m ?
            {
                // Silaha do ru arada ba ka bloklay c  duvar var m  kontrol et
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
        else if (_currentState == EnemyState.Alert)
        {
            _agent.isStopped = true;
            _alertTimer -= Time.deltaTime;
            if (_alertTimer <= 0f)
            {
                _currentState = EnemyState.Chase;
            }
        }
        else if (_currentState == EnemyState.Chase)
        {
            _agent.speed = _chaseSpeed; 
            _agent.isStopped = false;
            
            if (_playerTransform != null) 
            {
                _agent.SetDestination(_playerTransform.position);
                
                float distanceToPlayer = Vector2.Distance(transform.position, _playerTransform.position);
                
                // Oyuncu menzildeyse ateş et ama duraksama, kovalayarak devam et
                if (distanceToPlayer <= _shootingRange)
                {
                    _enemyController.FireWeapon(); // ATEŞ ET
                }
            }
        }
        else if (_currentState == EnemyState.SearchWeapon)
        {
            if (_targetDroppedWeapon == null) return; // Ba kas  alm   veya kaybolmu  olabilir

            _agent.speed = _chaseSpeed; 
            _agent.isStopped = false;
            _agent.SetDestination(_targetDroppedWeapon.transform.position);

            // Silaha yeterince yak nsa al
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
                // Oyuncunun tersi y n n  hesapla ve oraya ko 
                Vector3 fleeDirection = (transform.position - _playerTransform.position).normalized;
                Vector3 fleeTarget = transform.position + fleeDirection * 5f;
                _agent.SetDestination(fleeTarget);
            }
        }
        else if (_currentState == EnemyState.Patrol)
        {
            _agent.speed = _patrolSpeed; // Devriye h z na ge 
            
            if (_currentPatrolPoints.Count == 0) return;

            _agent.isStopped = false;
            Transform targetPoint = _currentPatrolPoints[_currentPatrolIndex];
            _agent.SetDestination(targetPoint.position);

            // Noktaya vard ysa s radakine ge 
            if (Vector2.Distance(transform.position, targetPoint.position) < _patrolPointThreshold)
            {
                _currentPatrolIndex = (_currentPatrolIndex + 1) % _currentPatrolPoints.Count;
            }
        }
        else if (_currentState == EnemyState.InvestigateLastSeen)
        {
            _agent.speed = _chaseSpeed; 
            _agent.isStopped = false;

            if (_hasLastSeenPosition)
            {
                _agent.SetDestination(_lastSeenPosition);

                float distanceToLastSeen = Vector2.Distance(transform.position, _lastSeenPosition);
                if (distanceToLastSeen < _patrolPointThreshold)
                {
                    _hasLastSeenPosition = false;
                    _currentState = _isStationary ? EnemyState.Idle : EnemyState.Patrol;
                    if (!_isStationary) FindPatrolPointsInCurrentRoom();
                }
            }
            else
            {
                _currentState = _isStationary ? EnemyState.Idle : EnemyState.Patrol;
                if (!_isStationary) FindPatrolPointsInCurrentRoom();
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

            if (distance <= _roomSearchRadius) // Sadece belli bir yar  aptakileri kontrol et
            {
                // Arada engel (Obstacle) yoksa listeye dahil et
                // Burada da Inspector  zerinden belirledi imiz LayerMask'i kullanmak daha tutarl  olur
                RaycastHit2D hit = Physics2D.Linecast(transform.position, point.transform.position, LayerMask.GetMask("Obstacle", "Door"));

                if (hit.collider == null)
                {
                    _currentPatrolPoints.Add(point.transform);
                }
            }
        }
        
        // Yeni bir devriye listesi olu tuysa rastgele birinden ba la
        if (_currentPatrolPoints.Count > 0)
        {
             _currentPatrolIndex = Random.Range(0, _currentPatrolPoints.Count);
        }
    }

    private void RotateTowardsMovement()
    {
        // 1. DURUM: Kovalama veya Alert durumundaysak doğrudan oyuncuya dön
        if ((_currentState == EnemyState.Chase || _currentState == EnemyState.Alert) && _playerTransform != null)
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
        // 1. Oyuncuyu Fark Etme (Awareness) Alan  - K rm z   ember
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _playerAwarenessDistance);

        // 2. Devriye Noktalar n  Tarama Alan  (Room Search) - Mavi  ember
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, _roomSearchRadius);

        // E er oyun  al   yorsa hedeflere do ru  izgiler  iz
        if (Application.isPlaying)
        {
            // Kovalama Modu: Oyuncuya sar  bir  izgi  eker
            if (_currentState == EnemyState.Chase && _playerTransform != null)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(transform.position, _playerTransform.position);
            }
            // Devriye Modu: Gitti i hedefe ye il bir  izgi  eker
            else if (_currentState == EnemyState.Patrol && _currentPatrolPoints.Count > 0)
            {
                Transform targetPoint = _currentPatrolPoints[_currentPatrolIndex];
                if (targetPoint != null)
                {
                    Gizmos.color = Color.green;
                    Gizmos.DrawLine(transform.position, targetPoint.position);
                    Gizmos.DrawWireSphere(targetPoint.position, 0.3f); // Hedef noktay  da k   k bir topla belirginle tir
                }
            }
            else if (_currentState == EnemyState.InvestigateLastSeen && _hasLastSeenPosition)
            {
                Gizmos.color = Color.magenta;
                Gizmos.DrawLine(transform.position, _lastSeenPosition);
                Gizmos.DrawWireSphere(_lastSeenPosition, 0.5f);
            }
        }
    }
}
