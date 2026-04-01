using System.Collections.Generic;
using UnityEngine;

public class BulletPool : MonoBehaviour
{
    public static BulletPool Instance { get; private set; }

    [SerializeField] private int _poolSize = 10;

    [SerializeField] private Bullet _prefab;

    Queue<Bullet> _pool = new Queue<Bullet>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Debug.LogWarning("Multiple instances of BulletPool detected. Destroying duplicate.");
            Destroy(gameObject);
            return;
        }

        Initialize();
    }
    private void Initialize()
    {
        GameObject parent = new GameObject("BulletPool");

        if (_poolSize > 0)
        {
            for(int i = 0; i < _poolSize; i++)
            {   
                Bullet bullet = Instantiate(_prefab , parent.transform);
                bullet.gameObject.SetActive(false);
                _pool.Enqueue(bullet);
            }
        }
        else
        {
            Debug.LogError("Pool size must be greater than 0.");
        }
    }

    public Bullet GetBullet(Vector2 pos)
    {
        if(_pool.Count > 0)
        {
            Bullet bullet = _pool.Dequeue();
            bullet.transform.position = pos;
            bullet.gameObject.SetActive(true);

            return bullet;
        }
        else
        {
            Debug.LogWarning("Bullet pool exhausted! Consider increasing pool size.");
        }

        return null;
    }

    public void ReturnBullet(Bullet activeBullet)
    {
        if(_pool.Count < _poolSize)
        {
            if (activeBullet != null)
            {
                activeBullet.gameObject.SetActive(false);
                activeBullet.transform.position = Vector2.zero; 
                _pool.Enqueue(activeBullet);
            }
        }
        else
        {
            Debug.LogWarning("Bullet pool is already full! Cannot return bullet.");
        }
    }
}
