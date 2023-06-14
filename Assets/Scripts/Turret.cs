using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : NetworkBehaviour
{
    //assigned in prefab
    public GameObject meshes;
    public SphereCollider sphereCollider;

    //assigned in scene
    public MissileLauncher missileLauncher;

    private Vector3 playerPosition;
    private Vector3 playerMoveDirection;
    private float playerSpeed;

    private readonly float turretRotateSpeed = 3;
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

        Quaternion targetRotation = Quaternion.LookRotation(targetFireDirection);
        transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, Time.deltaTime * turretRotateSpeed);
    }

    private IEnumerator FireMissile() //only run on server
    {
        //initial delay of at least 5. Random so that turrets do not all fire at the same time
        yield return new WaitForSeconds(Random.Range(5, 5 + fireRate));

        while (canFire)
        {
            MissileInfo info = new()
            {
                firePosition = transform.position,
                fireRotation = transform.rotation,
                launcher = null
            };
            missileLauncher.Fire(info);

            yield return new WaitForSeconds(fireRate);
        }
    }

    [Server]
    public void ServerDestroy()
    {
        DestroyRespawn(false);
        RpcClientDestroyRespawn(false);

        StartCoroutine(ServerRespawn());
    }
    [Server]
    private IEnumerator ServerRespawn()
    {
        yield return new WaitForSeconds(respawnTime);

        DestroyRespawn(true);
        RpcClientDestroyRespawn(true);

        StartCoroutine(FireMissile());
    }
    [ObserversRpc]
    private void RpcClientDestroyRespawn(bool respawn)
    {
        if (!IsServer)
            DestroyRespawn(respawn);
    }
    private void DestroyRespawn(bool respawn) //true = respawn, false = destroy
    {
        canFire = respawn;
        meshes.SetActive(respawn);
        sphereCollider.enabled = respawn;
    }
}