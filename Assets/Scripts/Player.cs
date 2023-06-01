using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour
{
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
    public EscapeMenu escapeMenu;
    public ScoreTracker scoreTracker;

    public List<GameObject> hearts = new List<GameObject>();


    //assigned dynamically:
    private SpringJoint leftJoint;
    private SpringJoint rightJoint;

    //cache velocity when pausing
    private Vector3 pauseVelocity;

    //custom gravity
    private readonly float gravityScale = 1.5f;//3;

    //custom drag
    private readonly float defaultDrag = .25f;//.5f;
    private float drag; //dynamic

    //rotate player with mouse
    private readonly float mediumRotateSpeed = 8;
    private readonly float sensitivityChangeAmount = 1.5f;

    //fires raycast through crosshairs
    private readonly float raycastOffset = .23f;

    //tether
    private readonly int maxTetherRange = 700;

    //reel
    private bool leftReeling;
    private bool rightReeling;
    private readonly float reelAmount = 50;//100;
    //increase when reeling both tethers
    private readonly float doubleReelAmount = 100;//200;

    //gas
    private readonly float gasBoost = 1.5f;//2;
    private float gasAmount = 30; //max 30
    private readonly float gasRefillSpeed = 8;
    private readonly float gasDrainSpeed = 4;
    //true when gas tap is successful:
    private bool gasHoldAvailable;

    //missile
    private float missileAmount = 30; //max 30
    private readonly float missileRefillSpeed = 8;

    //peek
    private bool peeking;

    //health
    private int health = 5;

    private void Start()
    {
        anchors.SetParent(null);
        anchors.position = Vector3.zero;
        anchors.localScale = Vector3.one;
    }

    private void FixedUpdate()
    {
        //cache velocity and freeze when paused
        if (EscapeMenu.paused)
        {
            if (pauseVelocity == Vector3.zero)
                pauseVelocity = rb.velocity;
            rb.velocity = Vector3.zero;
            return;
        }
        //reset after pause
        if (pauseVelocity != Vector3.zero)
        {
            rb.velocity = pauseVelocity;
            pauseVelocity = Vector3.zero;
        }

        //custom gravity
        Vector3 gravity = -9.81f * gravityScale * Vector3.up;
        rb.AddForce(gravity, ForceMode.Acceleration);

        //custom drag
        rb.velocity *= 1 - (Time.fixedDeltaTime * drag);
    }

    private void Update()
    {
        if (EscapeMenu.paused) return;

        RotateWithMouse();

        LaunchTether1();

        ReelTether();

        Gas();

        FireMissile();

        Peek();
    }

    private void RotateWithMouse() //run in update
    {
        if (peeking) return;

        float rotateSpeed = mediumRotateSpeed + (sensitivityChangeAmount * escapeMenu.sensitivity);
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
                drag = defaultDrag;

            return;
        }

        //gas refill
        gasHoldAvailable = false;
        drag = defaultDrag;
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
            objectPool.GetPooledInfo().missile.Launch("Turret", this, transform);
        }
    }

    private void Peek() //run in update
    {
        if (Input.GetButton("Peek") && rb.velocity != Vector3.zero)
        {
            peeking = true;
            transform.rotation = Quaternion.LookRotation(rb.velocity.normalized, Vector3.up);
        }
        else
            peeking = false;
    }


    public void TakeDamage()
    {
        if (health <= 1)
        {
            //escapeMenu.GameEnd();
            return;
        }

        hearts[0].SetActive(false);
        hearts.RemoveAt(0);
        health -= 1;
    }

    public void EarnPoints(int amount)
    {
        //NEEDS TO BE CHANGED LATER! Right now, collision happens everywhere. When it happens on server,
        //this method will be called on the server, so playerNumber will always be the host!!
        scoreTracker.ChangeScore(GameManager.playerNumber, amount, false);
    }
}