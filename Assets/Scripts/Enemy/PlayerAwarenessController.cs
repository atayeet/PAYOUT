using UnityEngine;

public class PlayerAwarenessController : MonoBehaviour
{
    public bool AwareOfPlayer { get; private set; }

    public Vector2 DirectionToPlayer { get; private set; }

    [SerializeField]
    private float _playerAwarenessDistance;

    private Transform _player;

    private float _sqrPlayerAwarenessDistance;

    private void Awake()
    {
        _player = Object.FindFirstObjectByType<PlayerController>().transform;
        _sqrPlayerAwarenessDistance = _playerAwarenessDistance * _playerAwarenessDistance;
    }

    // Update is called once per frame
    void Update()
    {
        Vector2 enemyToPlayerVector = _player.position - transform.position;

        if (enemyToPlayerVector.sqrMagnitude <= _sqrPlayerAwarenessDistance)
        {
            AwareOfPlayer = true;
            DirectionToPlayer = enemyToPlayerVector.normalized;
        }
        else
        {
            AwareOfPlayer = false;
        }
    }
}
