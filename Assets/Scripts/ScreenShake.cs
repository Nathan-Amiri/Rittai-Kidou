using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenShake : MonoBehaviour
{
    //private readonly float defaultShakeAmount = .3f;
    //private readonly float decreaseFactor = 30;

    ////dynamic
    //private float shake = 0;
    //private float shakeAmount;

    //private void OnEnable()
    //{
    //    Missile.ScreenShake += ScreenShake;
    //}
    //private void OnDisable()
    //{
    //    Missile.ScreenShake -= ScreenShake;
    //}

    //private void ScreenShake(Vector3 explodePosition)
    //{
    //    float distance = (explodePosition - transform.position).magnitude;
    //    if (distance < 30) return; //prevent shaking when fired missile explodes immediately

    //    float fraction = distance / 2300; //fraction of the maximum distance
    //    float intensity = 1 - fraction;
    //    shakeAmount = defaultShakeAmount * intensity;

    //    shake = 1;
    //}

    //private void Update()
    //{
    //    if (shake > 0)
    //    {
    //        transform.localPosition = Random.insideUnitSphere * shakeAmount;
    //        shake -= Time.deltaTime * decreaseFactor;
    //    }
    //    else
    //    {
    //        shake = 0;
    //        transform.localPosition = Vector3.zero;
    //    }
    //}
}