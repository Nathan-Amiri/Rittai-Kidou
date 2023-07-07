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
    public TrailRenderer trailRenderer;
    public AudioSource audioSource;
    public AudioClip launchClip;
    public AudioClip explodeClip;

    [NonSerialized] public readonly float missileSpeed = 400; //read by missilelauncher

    private Player immunePlayer;
    private bool isEnemy;

    //public delegate void ScreenShakeAction(Vector3 explodePosition);
    //public static event ScreenShakeAction ScreenShake;

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

        string animTrigger = isEnemy ? "EnemyMissile" : "Missile";
        anim.SetTrigger(animTrigger);

        Color trailColor = isEnemy ? Color.red : Color.blue;
        trailRenderer.startColor = trailColor;
        trailRenderer.endColor = trailColor;

        //when player fires missile, launch sound is delayed from unity due to unity bug, so playerAudio plays it instead
        if (launcher == null || !launcher.IsOwner)
            audioSource.PlayOneShot(launchClip);
    }

    private void OnTriggerEnter(Collider col)
    {
        //players have a main hitbox, and also a second, larger hitbox for missiles shot by enemy players (not turrets)
        string playerTag = immunePlayer == null ? "PlayerHitbox" : "EnemyPlayerHitbox";

        //prioritize enemies over terrain by checking for them first
        if (col.CompareTag(playerTag))
        {
            Player hitPlayer = col.transform.parent.GetComponent<Player>();
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

        //ScreenShake?.Invoke(transform.position);

        audioSource.PlayOneShot(explodeClip);

        StartCoroutine(EndExplosion());
    }
    private IEnumerator EndExplosion()
    {
        yield return new WaitForSeconds(1); //explosion animations are 1 second long
        gameObject.SetActive(false);
    }
}