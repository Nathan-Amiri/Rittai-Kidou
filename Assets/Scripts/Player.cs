using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Player : MonoBehaviour
{
    //to do:
    //Crosshairs
    //tether swing
    //reel
    //gas
    //gas meter
    //missile
    //turret
    //health meter
    //play again

    //multiplayer/menu!!!!
    //entity spawning
    //speed boost
    //other entity?
    //player/turret/missile art
    //sound effects
    //gas/missile trail



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

    //fires raycast through crosshairs
    private readonly float raycastOffset = .23f;

    private void Start()
    {
        anchors.SetParent(null);
        anchors.position = Vector3.zero;
        anchors.localScale = Vector3.one;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
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
            rb.velocity = 100 * transform.forward;
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
        //get offset angles

        //used for raycast
        Vector3 hitOffset = raycastOffset * transform.right;
        //used only for linerenderers
        Vector3 startOffset = (transform.right * -1) + (transform.up * -1);

        if (Input.GetButtonDown("LeftTether"))
        {
            int layerMask = 1 << 7;
            if (Physics.Raycast(transform.position, transform.forward - hitOffset, out RaycastHit hit, 3000, layerMask))
            {
                leftLineRenderer.enabled = true;
                leftLineRenderer.SetPosition(1, hit.point);
            }
        }
        else if (Input.GetButtonUp("LeftTether"))
            leftLineRenderer.enabled = false;

        if (leftLineRenderer.enabled)
            leftLineRenderer.SetPosition(0, transform.position + startOffset);

        startOffset = (transform.right * 1) + (transform.up * -1);

        if (Input.GetButtonDown("RightTether"))
        {
            int layerMask = 1 << 7;
            if (Physics.Raycast(transform.position, transform.forward + hitOffset, out RaycastHit hit, 3000, layerMask))
            {
                rightLineRenderer.enabled = true;
                rightLineRenderer.SetPosition(0, transform.position + startOffset);
                rightLineRenderer.SetPosition(1, hit.point);
            }
        }
        else if (Input.GetButtonUp("RightTether"))
            rightLineRenderer.enabled = false;

        if (rightLineRenderer.enabled)
            rightLineRenderer.SetPosition(0, transform.position + startOffset);
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