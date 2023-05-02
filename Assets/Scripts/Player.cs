using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public Rigidbody rb;

    //rotate player with mouse:
    private readonly float rotateSpeed = 8;

    //lineRenderer.SetPosition(0, transform.position + new Vector3(0, -1, 0));
    //lineRenderer.SetPosition(1, transform.position + new Vector3(0, -1, 0) + (transform.forward * 50));

    private void Update()
    {
        RotateWithMouse();

        ShootTether();

        ReelTether();

        Gas();

        FireMissile();

        Peek();

        if (Input.GetButton("Fire"))
            rb.velocity = 50 * transform.forward;
    }

    private void RotateWithMouse()
    {
        float yaw = rotateSpeed * Input.GetAxis("Mouse X");
        float pitch = rotateSpeed * Input.GetAxis("Mouse Y");

        //to avoid changing rotation.z, rotate yaw in world and pitch in self
        transform.Rotate(0, yaw, 0, Space.World);
        transform.Rotate(-pitch, 0, 0, Space.Self);

        //a backup solution, in which z is manually set to 0 (in case
        //above method causes errors later in the project) This method
        //will have an identical result
        //Vector3 eulers = transform.localEulerAngles;
        //eulers.x += -pitch;
        //eulers.y += yaw;
        //eulers.z = 0;
        //transform.localEulerAngles = eulers;
    }

    private void ShootTether()
    {

    }

    private void ReelTether()
    {

    }

    private void Gas()
    {

    }

    private void FireMissile()
    {

    }

    private void Peek()
    {
        if (Input.GetButtonDown("Peek") && rb.velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(rb.velocity.normalized, Vector3.up);
    }
}