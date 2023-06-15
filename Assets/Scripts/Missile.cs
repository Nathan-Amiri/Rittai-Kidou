using FishNet;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile : MonoBehaviour
{
    //assigned in inspector
    public Rigidbody rb;
    public Animator anim;
    public SphereCollider sphereCollider;

    [NonSerialized] public readonly float missileSpeed = 400; //read by missilelauncher

    private Player immunePlayer;
    private bool isEnemy;

    //only called by missilelauncher, run on client and server
    public void Launch(bool newIsEnemy, Player launcher, Vector3 firePosition, Quaternion fireRotation)
    {
        immunePlayer = launcher; //if launched by turret, launcher is null
        isEnemy = newIsEnemy;

        transform.rotation = fireRotation;
        //do NOT use SetPositionAndRotation, rotation must change first
        transform.position = firePosition + (transform.forward * 10);

        gameObject.SetActive(true);
        rb.velocity = missileSpeed * transform.forward;

        if (isEnemy)
            anim.SetTrigger("EnemyMissile");
        else
            anim.SetTrigger("Missile");
    }

    private void OnTriggerEnter(Collider col)
    {
        //prioritize enemies over terrain by checking for them first
        if (col.CompareTag("Player"))
        {
            Player hitPlayer = col.GetComponent<Player>();
            if (hitPlayer == immunePlayer)
                return;

            if (InstanceFinder.IsServer)
            {
                hitPlayer.TakeDamage();
                if (immunePlayer != null)
                    immunePlayer.EarnPoints(100);
            }

            Explode();
        }
        else if (col.CompareTag("Turret"))
        {
            if (immunePlayer == null)
                return; //if a turret hit another turret

            if (InstanceFinder.IsServer)
            {
                col.GetComponent<Turret>().ServerDestroy();
                immunePlayer.EarnPoints(30);
            }

            Explode();
        }
        else if (col.CompareTag("Terrain"))
            Explode();

    }

    private void Explode()
    {
        rb.velocity = Vector3.zero;

        if (isEnemy)
            anim.SetTrigger("EnemyExplosion");
        else
            anim.SetTrigger("Explosion");

        StartCoroutine(EndExplosion());
    }
    private IEnumerator EndExplosion()
    {
        yield return new WaitForSeconds(1);
        gameObject.SetActive(false);
    }
}