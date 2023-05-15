using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Turret : MonoBehaviour
{
    public void Destroy()
    {
        Destroy(gameObject);
    }
}