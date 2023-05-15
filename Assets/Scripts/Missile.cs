using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Missile : MonoBehaviour
{
    //assigned in inspector
    public Rigidbody rb;
    public Animator anim;
    public SphereCollider sphereCollider;

    private readonly float missileSpeed = 400;
    private string enemyTag; //"Turret" or "Player"

    public void Launch(Transform launcher, string newEnemyTag) //called by player/turret
    {
        enemyTag = newEnemyTag;

        sphereCollider.enabled = false;
        transform.SetPositionAndRotation(launcher.position + (launcher.forward * 10), launcher.rotation);

        gameObject.SetActive(true);
        rb.velocity = missileSpeed * transform.forward;
        StartCoroutine(EnableTrigger());

        if (enemyTag == "Turret")
            anim.SetTrigger("Missile");
        else if (enemyTag == "Player")
            anim.SetTrigger("EnemyMissile");
    }
    private IEnumerator EnableTrigger()
    {
        yield return new WaitForSeconds(.25f);
        sphereCollider.enabled = true;
    }

    private void OnTriggerEnter(Collider col)
    {
        //prioritize enemies over terrain by checking for them first
        if (col.CompareTag(enemyTag))
        {
            if (enemyTag == "Turret")
                col.GetComponent<Turret>().Destroy();
            else if (enemyTag == "Player")
                col.GetComponent<Player>().TakeDamage();

            Explode();
        }
        else if (col.CompareTag("Terrain"))
            Explode();

    }

    private void Explode()
    {
        rb.velocity = Vector3.zero;

        if (enemyTag == "Turret")
            anim.SetTrigger("Explosion");
        else if (enemyTag == "Player")
            anim.SetTrigger("EnemyExplosion");

        StartCoroutine(EndExplosion());
    }
    private IEnumerator EndExplosion()
    {
        yield return new WaitForSeconds(1);
        gameObject.SetActive(false);
    }
}