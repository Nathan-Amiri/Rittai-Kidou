using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    //assigned in inspector:
    public Rigidbody rb;
    public Transform anchors;

    public LineRenderer leftLineRenderer;
    public LineRenderer rightLineRenderer;

    public Transform leftAnchor;
    public Transform rightAnchor;

    public ConfigurableJoint leftJoint;
    public ConfigurableJoint rightJoint;

    //rotate player with mouse:
    private readonly float rotateSpeed = 8;

    private void Start()
    {
        anchors.SetParent(null);
        anchors.position = Vector3.zero;
        anchors.localScale = Vector3.one;
    }

    private void Update()
    {
        RotateWithMouse();

        LaunchTether();

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
        //to avoid changing rotation.z, rotate yaw in worldspace and pitch in localspace
        transform.Rotate(0, yaw, 0, Space.World);
        transform.Rotate(-pitch, 0, 0, Space.Self);
    }

    private void LaunchTether()
    {

        if (Input.GetButton("LeftTether"))
        {
            int layerMask = 1 << 7;
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, 3000, layerMask))
            {
                leftLineRenderer.SetPosition(0, transform.position + new Vector3(0, -1, 0));
                leftLineRenderer.SetPosition(1, hit.point);
            }
        }
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