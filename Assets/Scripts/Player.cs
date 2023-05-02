using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public LineRenderer lineRenderer;

    public float moveSpeed;

    //rotate player with mouse:
    public float rotateSpeed;
    private float yaw;
    private float pitch;

    private void Update()
    {
        //rotate player with mouse:
        yaw += rotateSpeed * Input.GetAxis("Mouse X");
        pitch -= rotateSpeed * Input.GetAxis("Mouse Y");
        transform.eulerAngles = new Vector3(pitch, yaw, 0);

        if (Input.GetButton("Jump"))
            transform.Translate(moveSpeed * Time.deltaTime * transform.forward, Space.World);

        //lineRenderer.SetPosition(0, transform.position + new Vector3(0, -1, 0));
        //lineRenderer.SetPosition(1, transform.position + new Vector3(0, -1, 0) + (transform.forward * 50));
    }
}