using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : NetworkBehaviour
{
    //assigned in prefab
    public GameObject meshes;
    public SphereCollider sphereCollider;

    //assigned dynamically
    private ObjectPool objectPool;

    private Vector3 playerPosition;
    private Vector3 playerMoveDirection;
    private float playerSpeed;

    private readonly float fireRate = 3;

    private bool canFire = true;
    private readonly float respawnTime = 20;

    private GameManager gameManager;

    private void OnEnable()
    {
        GameManager.OnClientConnectOrLoad += OnSpawn;
    }
    private void OnDisable()
    {
        GameManager.OnClientConnectOrLoad -= OnSpawn;
    }

    private void Awake()
    {
        //playerRB = GameObject.FindWithTag("Player").GetComponent<Rigidbody>();
        objectPool = GameObject.Find("MiscScripts").GetComponent<ObjectPool>();
    }

    private void OnSpawn(GameManager gm)
    {
        if (!IsServer)
            return;

        if (gm.peacefulGameMode)
        {
            Despawn(gameObject);
            return;
        }
        
        gameManager = gm;
        StartCoroutine(FireMissile());
    }

    private void Update()
    {
        if (gameManager == null) return; //must be battlemode and server

        Rigidbody playerRb = null;
        float shortestDistance = 0;
        foreach (Rigidbody rb in gameManager.playerRbs)
        {
            if (rb == null) continue;

            float newDistance = Vector3.Distance(transform.position, rb.transform.position);
            if (shortestDistance == 0 || newDistance < shortestDistance)
            {
                shortestDistance = newDistance;
                playerRb = rb;
            }
        }
        if (playerRb == null)
            return; //playerRb not loaded by Setup yet

        playerPosition = playerRb.transform.position;
        playerMoveDirection = playerRb.velocity.normalized;
        playerSpeed = playerRb.velocity.magnitude;

        //how far in the future the player will be predicted
        float futureTime = Random.Range(.5f, 1);
        //how far the player will travel in futureTime
        float playerTravelDistance = futureTime * playerSpeed;
        //where the player will be in futureTime
        Vector3 playerFuturePosition = playerPosition + (playerTravelDistance * playerMoveDirection);
        //the direction the turret wants to fire in
        Vector3 targetFireDirection = (playerFuturePosition - transform.position).normalized;

        transform.rotation = Quaternion.LookRotation(targetFireDirection);
        //Quaternion targetRotation = Quaternion.LookRotation(targetFireDirection);
        //transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * turretRotateSpeed);
    }

    private IEnumerator FireMissile() //only run on server
    {
        //initial delay of at least 5. Random so that turrets do not all fire at the same time
        yield return new WaitForSeconds(Random.Range(5, 5 + fireRate));

        while (canFire)
        {
            objectPool.GetPooledMissile().Launch(true, null, transform.position, transform.rotation);

            yield return new WaitForSeconds(fireRate);
        }
    }

    public void Destroy()
    {
        //ScoreTracker.currentScore += 100;

        canFire = false;
        meshes.SetActive(false);
        sphereCollider.enabled = false;

        StartCoroutine(Respawn());
    }
    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnTime);

        canFire = true;
        meshes.SetActive(true);
        sphereCollider.enabled = true;

        StartCoroutine(FireMissile());
    }
}