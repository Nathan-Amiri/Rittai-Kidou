using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DemoMovement : MonoBehaviour
{
    public float Acceleration = 5;
    private Vector3 Speed;

    void Update()
    {
        Vector3 force = transform.right * Input.GetAxis("Horizontal");
        force += transform.up * Input.GetAxis("Vertical");
        if (Input.GetKey(KeyCode.LeftShift))
        {
            force += transform.forward;
        }
        if (Input.GetKey(KeyCode.Space))
        {
            force -= transform.forward;
        }
        Speed *= Mathf.Pow(0.4f, Time.deltaTime); //only 40% of its speed remains after 1 second (when not accelerating)
        Speed += force * Acceleration * Time.deltaTime;
        transform.position += Speed * Time.deltaTime;
        Debug.Log("speed: " + Speed);
    }
}
