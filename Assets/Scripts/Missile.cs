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

    public void Launch(bool isEnemy, Player launcher, Vector3 firePosition, Quaternion fireRotation) //only called by missilelauncher
    {
        immunePlayer = launcher; //if launched by turret, launcher is null

        sphereCollider.enabled = false;

        transform.rotation = fireRotation; //do NOT use SetPositionAndRotation
        transform.position = firePosition + (transform.forward * 10);

        gameObject.SetActive(true);
        rb.velocity = missileSpeed * transform.forward;
        StartCoroutine(EnableTrigger());

        if (isEnemy)
            anim.SetTrigger("EnemyMissile");
        else
            anim.SetTrigger("Missile");
    }
    private IEnumerator EnableTrigger()
    {
        yield return new WaitForSeconds(.25f);
        sphereCollider.enabled = true;
    }

    private void OnTriggerEnter(Collider col)
    {
        //prioritize enemies over terrain by checking for them first
        string enemyTag = "Turret";
        if (col.CompareTag(enemyTag))
        {
            if (enemyTag == "Turret")
            {
                col.GetComponent<Turret>().Destroy();
                //currentLauncher.EarnPoints(30);
            }
            //    else if (enemyTag == "Player")
            //    {
            //        col.GetComponent<Player>().TakeDamage();
            //        if (currentLauncher != null)
            //            currentLauncher.EarnPoints(100);
            //    }

            Explode();
            //}

        }
        else if (col.CompareTag("Terrain"))
            Explode();
    }

    private void Explode()
    {
        rb.velocity = Vector3.zero;

        //if (enemyTag == "Turret")
        anim.SetTrigger("Explosion");
        //else if (enemyTag == "Player")
        //    anim.SetTrigger("EnemyExplosion");

        StartCoroutine(EndExplosion());
    }
    private IEnumerator EndExplosion()
    {
        yield return new WaitForSeconds(1);
        gameObject.SetActive(false);
    }
}