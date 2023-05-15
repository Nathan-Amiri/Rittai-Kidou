using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool sharedInstance;
    private List<MissileInfo> pooledObjects;
    public GameObject objectToPool; //assigned in inspector
    private int amountToPool;

    private void Awake()
    {
        sharedInstance = this;

        amountToPool = 40;
    }

    private void Start()
    {
        pooledObjects = new List<MissileInfo>();
        GameObject tmp;
        for (int i = 0; i < amountToPool; i++)
        {
            tmp = Instantiate(objectToPool, transform);
            tmp.SetActive(false);
            MissileInfo newInfo = new()
            {
                obj = tmp,
                missile = tmp.GetComponent<Missile>()
            };
            pooledObjects.Add(newInfo);
        }
    }

    public MissileInfo GetPooledInfo()
    {
        for (int i = 0; i < amountToPool; i++)
        {
            if (!pooledObjects[i].obj.activeSelf)
                return pooledObjects[i];
        }
        return default;
    }
}
public struct MissileInfo
{
    public GameObject obj;
    public Missile missile;
}