using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
    //to do:
    //turret
    //health indicators
    //play again (show inputs on playagain screen)

    //multiplayer/menu!!!!
    //entity spawning
    //speed boost
    //heal
    //arena larger
    //player/turret/missile art
    //sound effects
    //gas/missile trail



    //assigned in inspector:
    public Rigidbody rb;

    public Transform anchors;

    public TMP_Text leftCrosshair;
    public TMP_Text rightCrosshair;

    public LineRenderer leftLineRenderer;
    public LineRenderer rightLineRenderer;

    public GameObject leftAnchor;
    public GameObject rightAnchor;

    public Transform gasScaler;
    public Image gasAmountImage;

    public Transform missileScaler;
    public Image missileAmountImage;

    public ObjectPool objectPool;


    //assigned dynamically:
    private SpringJoint leftJoint;
    private SpringJoint rightJoint;


    //custom gravity
    private readonly float gravityScale = 3;

    //custom drag
    private float drag; //set in Gas()

    //rotate player with mouse
    private readonly float rotateSpeed = 8;

    //fires raycast through crosshairs
    private readonly float raycastOffset = .23f;

    //tether
    private readonly int maxTetherRange = 700;

    //reel
    private bool leftReeling;
    private bool rightReeling;
    private readonly float reelAmount = 100;
    //increase when reeling both tethers
    private readonly float doubleReelAmount = 200;

    //gas
    private readonly float gasBoost = 2;
    private float gasAmount = 30; //max 30
    private readonly float gasRefillSpeed = 8;
    private readonly float gasDrainSpeed = 4;
    //true when gas tap is successful:
    private bool gasHoldAvailable;

    //missile
    private float missileAmount = 30; //max 30
    private readonly float missileRefillSpeed = 8;

    private void Start()
    {
        anchors.SetParent(null);
        anchors.position = Vector3.zero;
        anchors.localScale = Vector3.one;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void FixedUpdate()
    {
        //custom gravity
        Vector3 gravity = -9.81f * gravityScale * Vector3.up;
        rb.AddForce(gravity, ForceMode.Acceleration);

        //custom drag
        rb.velocity *= 1 - (Time.fixedDeltaTime * drag);
    }

    private void Update()
    {
        RotateWithMouse();

        LaunchTether1();

        ReelTether();

        Gas();

        FireMissile();

        Peek();
    }

    private void RotateWithMouse() //run in update
    {
        float yaw = rotateSpeed * Input.GetAxis("Mouse X");
        float pitch = rotateSpeed * Input.GetAxis("Mouse Y");
        //to avoid changing rotation.z, rotate yaw in worldspace and pitch in localspace
        transform.Rotate(0, yaw, 0, Space.World);
        transform.Rotate(-pitch, 0, 0, Space.Self);
    }

    private void LaunchTether1() //run in update
    {
        LaunchTether2(-1, leftCrosshair, "LeftTether", leftLineRenderer, leftAnchor);
        LaunchTether2(1, rightCrosshair, "RightTether", rightLineRenderer, rightAnchor);
    }

    private void LaunchTether2(int posOrNeg, TMP_Text crosshair, string tetherInput, LineRenderer tetherRenderer, GameObject anchor)
    {
        bool isLeft = posOrNeg == -1;
        //else isRight, posOrNeg == 1;

        //get offset angles
        //used for raycast:
        Vector3 hitOffset = raycastOffset * posOrNeg * transform.right;
        //used only for linerenderers:
        Vector3 startOffset = (1 * posOrNeg * transform.right) + (-1 * transform.up);

        int layerMask = 1 << 7;
        if (!Physics.Raycast(transform.position, transform.forward + hitOffset, out RaycastHit hit, maxTetherRange, layerMask))
            crosshair.color = Color.black;
        else //if terrain is in range
        {
            crosshair.color = Input.GetButton(tetherInput) ? Color.black : Color.red;

            if (Input.GetButtonDown(tetherInput))
            {
                anchor.transform.position = hit.point;
                if (isLeft)
                    leftJoint = CreateJoint(leftAnchor);
                else
                    rightJoint = CreateJoint(rightAnchor);

                tetherRenderer.enabled = true;
                tetherRenderer.SetPosition(1, hit.point);
            }
        }

        if (Input.GetButtonUp(tetherInput))
        {
            tetherRenderer.enabled = false;
            if (isLeft)
                Destroy(leftJoint);
            else
                Destroy(rightJoint);
        }

        if (tetherRenderer.enabled)
        {
            tetherRenderer.SetPosition(0, transform.position + startOffset);

            float distance = Vector3.Distance(transform.position, anchor.transform.position);

            SpringJoint joint = isLeft ? leftJoint : rightJoint;
            joint.minDistance = 0;
            //if not reeling
            if ((isLeft && !leftReeling) || (!isLeft && !rightReeling))
            {
                joint.spring = 4.5f;
                joint.maxDistance = distance;
            }
        }
    }

    private SpringJoint CreateJoint(GameObject jointObject)
    {
        SpringJoint joint = jointObject.AddComponent<SpringJoint>();
        joint.autoConfigureConnectedAnchor = false;
        joint.connectedAnchor = Vector3.zero;
        joint.damper = 7;
        joint.massScale = 4.5f;
        joint.connectedBody = rb;
        return joint;
    }

    private void ReelTether() //run in update
    {
        leftReeling = Input.GetButton("LeftReel") & leftJoint != null;
        rightReeling = Input.GetButton("RightReel") & rightJoint != null;

        float newReelAmount = leftReeling && rightReeling ? doubleReelAmount : reelAmount;

        if (leftReeling)
        {
            float distance = Vector3.Distance(transform.position, leftAnchor.transform.position);
            leftJoint.spring = newReelAmount;
            leftJoint.maxDistance = distance - 10;
        }
        if (rightReeling)
        {
            float distance = Vector3.Distance(transform.position, rightAnchor.transform.position);
            rightJoint.spring = newReelAmount;
            rightJoint.maxDistance = distance - 10;
        }
    }

    private void Gas() //run in update
    {
        //update meter
        gasScaler.localScale = new Vector2(gasScaler.localScale.x, gasAmount / 30);
        gasAmountImage.color = gasAmount > 10 ? Color.white : Color.red;

        //gas tap
        if (Input.GetButtonDown("Gas") && gasAmount > 10)
        {
            gasAmount -= 10;
            gasHoldAvailable = true;
            StartCoroutine(GasDelay());
        }

        if (Input.GetButton("Gas") && gasHoldAvailable)
        {
            //gas hold
            if (gasAmount > 0)
            {
                drag = 0;
                gasAmount -= gasDrainSpeed * Time.deltaTime;
            }
            else //gas freeze
                drag = .5f;

            return;
        }

        //gas refill
        gasHoldAvailable = false;
        drag = .5f;
        if (gasAmount > 30)
            gasAmount = 30;
        else if (gasAmount < 30)
            gasAmount += gasRefillSpeed * Time.deltaTime;
    }
    private IEnumerator GasDelay()
    {
        yield return new WaitForSeconds(.8f);
        rb.velocity *= gasBoost;
    }

    private void FireMissile() //run in update
    {
        //update meter
        missileScaler.localScale = new Vector2(missileScaler.localScale.x, missileAmount / 30);
        missileAmountImage.color = missileAmount > 10 ? Color.blue : Color.gray;

        //refill meter
        if (missileAmount > 30)
            missileAmount = 30;
        else if (missileAmount < 30)
            missileAmount += missileRefillSpeed * Time.deltaTime;

        if (Input.GetButtonDown("Fire") && missileAmount > 10)
        {
            missileAmount -= 10;
            objectPool.GetPooledInfo().missile.Launch(transform, "Turret");
        }
    }

    private void Peek() //run in update
    {
        if (Input.GetButtonDown("Peek") && rb.velocity != Vector3.zero)
            transform.rotation = Quaternion.LookRotation(rb.velocity.normalized, Vector3.up);
    }


    public void TakeDamage()
    {

    }
}