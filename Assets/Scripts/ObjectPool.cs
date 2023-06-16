using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    private List<Missile> pooledMissiles;

    private readonly int amountToPool = 50;

    //assigned in scene:
    public GameObject objectToPool;
    public Transform poolParent;

    private void Start()
    {
        pooledMissiles = new List<Missile>();
        GameObject tmp;
        for (int i = 0; i < amountToPool; i++)
        {
            tmp = Instantiate(objectToPool, poolParent);
            tmp.SetActive(false);
            Missile missile = tmp.GetComponent<Missile>();
            pooledMissiles.Add(missile);
        }
    }

    public Missile GetPooledMissile()
    {
        for (int i = 0; i < amountToPool; i++)
        {
            if (!pooledMissiles[i].gameObject.activeSelf)
                return pooledMissiles[i];
        }
        Debug.LogError("No available objects in pool");
        return default;
    }
}