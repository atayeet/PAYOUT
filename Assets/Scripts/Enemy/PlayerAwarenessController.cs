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
    [Tooltip("Düþmanýn görüþünü engelleyecek katmanlar (Örn: Obstacle, Door)")]
    //[SerializeField] private LayerMask _obstacleLayerMask; // Yeni eklenen LayerMask

    // Diðer deðiþkenleriniz ayný kalýyor...
    [Header("Movement & Rotation")]
    [SerializeField] private float _chaseSpeed = 3.5f;
    [SerializeField] private float _patrolSpeed = 1.5f;
    [SerializeField] private float _rotationSpeed = 500f;
    [SerializeField] private float _rotationOffset = 0f;

    [Header("Patrol Settings")]
    [SerializeField] private float _patrolPointThreshold = 0.5f;
    [SerializeField] private float _roomSearchRadius = 20f;

    private Transform _playerTransform;
    private float _sqrPlayerAwarenessDistance;

    private NavMeshAgent _agent;
    private Rigidbody2D _rigidbody;
    private PlayerController _playerController;

    private enum EnemyState { Patrol, Chase }
    private EnemyState _currentState = EnemyState.Patrol;
    private List<Transform> _currentPatrolPoints = new List<Transform>();
    private int _currentPatrolIndex;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();
        _rigidbody = GetComponent<Rigidbody2D>();

        // NavMeshAgent ve 2D Ayarlarý
        _agent.updateRotation = false;
        _agent.updateUpAxis = false;
        _agent.speed = _patrolSpeed;
        _agent.obstacleAvoidanceType = ObstacleAvoidanceType.NoObstacleAvoidance;

        PlayerController pc = Object.FindFirstObjectByType<PlayerController>();
        if (pc != null) 
        {
            _playerTransform = pc.transform;
            _playerController = pc; // Oyuncuya daha pratik ulaþým için
        }

        _sqrPlayerAwarenessDistance = _playerAwarenessDistance * _playerAwarenessDistance;

        FindPatrolPointsInCurrentRoom();
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
        // Oyuncu öldüyse veya referansý uçtuysa kovalamayý sonlandýr - SÝZÝ YAKALAMASIN
        if (_playerTransform == null || (_playerController != null && _playerController.IsDead)) 
        {
            AwareOfPlayer = false;
            return;
        }

        Vector2 enemyToPlayerVector = _playerTransform.position - transform.position;

        if (enemyToPlayerVector.sqrMagnitude <= _sqrPlayerAwarenessDistance)
        {
            // Görüþ Açýsý Kontrolü (Line of Sight)
            // Düþmandan oyuncuya bir çizgi (Linecast) çeker, engele çarpýp çarpmadýðýna bakar
            RaycastHit2D hit = Physics2D.Linecast(transform.position, _playerTransform.position, LayerMask.GetMask("Obstacle", "Door"));

            // Eðer aradaki çizgi belirtilen LayerMask'teki bir þeye çarpmamýþsa (hit.collider yoksa) oyuncuyu görür
            if (hit.collider == null)
            {
                AwareOfPlayer = true;
                DirectionToPlayer = enemyToPlayerVector.normalized;
            }
            else
            {
                // Arada duvar vb. bir engel var ise oyuncuyu duymaz/görmez
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
        // Player'ý Görürse -> Kovalamaya Geç
        if (AwareOfPlayer && _currentState != EnemyState.Chase)
        {
            _currentState = EnemyState.Chase;
        }
        // Player'ý Kaybederse -> Hemen Devriyeye Dön
        else if (!AwareOfPlayer && _currentState == EnemyState.Chase)
        {
            _currentState = EnemyState.Patrol;
            FindPatrolPointsInCurrentRoom(); // Player'ý kaybettiði yerdeki ("yeni" odadaki) noktalarý tarar.
        }
    }
    
    private void ExecuteCurrentState()
    {
        if (_currentState == EnemyState.Chase)
        {
            _agent.speed = _chaseSpeed; // Kovalama hýzýna geç
            _agent.isStopped = false;
            if (_playerTransform != null) _agent.SetDestination(_playerTransform.position);
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
        Vector2 targetDir = _agent.desiredVelocity;

        if (targetDir.sqrMagnitude > 0.01f)
        {
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
