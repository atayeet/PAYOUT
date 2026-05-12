using System.Collections.Generic;
using UnityEngine;

public class EffectPool : MonoBehaviour
{
    public static EffectPool Instance { get; private set; }

    [System.Serializable]
    public class Pool
    {
        public string id;            // Kan, DuvarTozu vs gibi ayýrt edici bir isim
        public GameObject prefab;
        public int size;
    }

    public List<Pool> pools;
    public Dictionary<string, Queue<GameObject>> poolDictionary;

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

        poolDictionary = new Dictionary<string, Queue<GameObject>>();

        foreach (Pool pool in pools)
        {
            Queue<GameObject> objectPool = new Queue<GameObject>();
            for (int i = 0; i < pool.size; i++)
            {
                GameObject obj = Instantiate(pool.prefab, transform);
                obj.SetActive(false);
                objectPool.Enqueue(obj);
            }
            poolDictionary.Add(pool.id, objectPool);
        }
    }

    public GameObject SpawnEffect(string id, Vector2 position, Quaternion rotation)
    {
        if (!poolDictionary.ContainsKey(id) || poolDictionary[id].Count == 0)
        {
            return null;
        }

        GameObject obj = poolDictionary[id].Dequeue();
        
        obj.transform.position = position;
        obj.transform.rotation = rotation;
        obj.SetActive(true);

        // Efektin iþi bittiðinde tekrar havuza alýnmasý için sýraya ekle
        poolDictionary[id].Enqueue(obj);

        return obj;
    }

    public void ClearAllEffects()
    {
        // Pool objesinin altýndaki tüm açýk çocuk objeleri (Kan/parça vb.) kapatýr.
        // Scriptinizdeki mantýk bunlarý listeye halihazýrda eklediði için SetActive(false) güvenlidir.
        foreach (Transform child in transform)
        {
            child.gameObject.SetActive(false);
        }
    }
}
