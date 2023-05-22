using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
    //assigned in inspector
    public GameObject meshes;
    public SphereCollider sphereCollider;

    private ObjectPool objectPool;
    private Rigidbody playerRB;

    private Vector3 playerPosition;
    private Vector3 playerMoveDirection;
    private float playerSpeed;

    private readonly float fireRate = 3;

    private bool canFire = true;
    private readonly float respawnTime = 20;

    private void Awake()
    {
        playerRB = GameObject.FindWithTag("Player").GetComponent<Rigidbody>();
        objectPool = GameObject.Find("MiscScripts").GetComponent<ObjectPool>();
    }

    private void Start()
    {
        StartCoroutine(FireMissile());
    }

    private void Update()
    {
        playerPosition = playerRB.transform.position;
        playerMoveDirection = playerRB.velocity.normalized;
        playerSpeed = playerRB.velocity.magnitude;

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

    private IEnumerator FireMissile()
    {
        //initial delay, random so that turrets do not all fire at the same time
        yield return new WaitForSeconds(Random.Range(5, 8));

        while (canFire)
        {
            //if (GameManager.peacefulGameMode)
            //    break;

            //place pause check inside loop so that loop continues to run when paused
            if (!EscapeMenu.paused)
                objectPool.GetPooledInfo().missile.Launch(transform, "Player");

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