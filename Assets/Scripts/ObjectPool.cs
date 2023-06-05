using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour
{
    public static ObjectPool sharedInstance;
    private List<Missile> pooledMissiles;

    private int amountToPool;

    //assigned in scene:
    public GameObject objectToPool;
    public Transform poolParent;

    private void Awake()
    {
        sharedInstance = this;

        amountToPool = 50;
    }

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
        return default;
    }
}