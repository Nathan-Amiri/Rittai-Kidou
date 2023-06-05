using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using System;
using Unity.VisualScripting;
using UnityEngine.SocialPlatforms;

public class Player : NetworkBehaviour
{
    //assigned in prefab:
    public Rigidbody rb;
    public Transform anchors;
    public LineRenderer leftLineRenderer;
    public LineRenderer rightLineRenderer;
    public GameObject leftAnchor;
    public GameObject rightAnchor;
    public MeshRenderer playerRenderer; //accessed by setup

    //assigned by Setup:
    [NonSerialized] public Camera mainCamera;

    [NonSerialized] public TMP_Text leftCrosshair;
    [NonSerialized] public TMP_Text rightCrosshair;

    [NonSerialized] public Transform gasScaler;
    [NonSerialized] public Image gasAmountImage;

    [NonSerialized] public Transform missileScaler;
    [NonSerialized] public Image missileAmountImage;

    [NonSerialized] public ObjectPool objectPool;
    [NonSerialized] public EscapeMenu escapeMenu;
    [NonSerialized] public ScoreTracker scoreTracker;

    [NonSerialized] public List<GameObject> hearts = new();

    //assigned dynamically:
    private SpringJoint leftJoint;
    private SpringJoint rightJoint;

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

    public void OnSpawn() //run by Setup
    {
        anchors.SetParent(null);
        anchors.position = Vector3.zero;
        anchors.localScale = Vector3.one;
        if (IsOwner)
        {
            mainCamera.transform.SetParent(transform);
            mainCamera.transform.SetPositionAndRotation(transform.position, transform.rotation);
        }
    }

    private void FixedUpdate()
    {
        if (!IsOwner) return;

        //custom gravity
        Vector3 gravity = -9.81f * gravityScale * Vector3.up;
        rb.AddForce(gravity, ForceMode.Acceleration);

        //custom drag
        rb.velocity *= 1 - (Time.fixedDeltaTime * drag);
    }

    private void Update()
    {
        if (!IsOwner) return;

        RotateWithMouse();

        LaunchTether1();

        ReelTether();

        Gas();

        Missile();

        Peek();
    }

    private void RotateWithMouse() //run in update
    {
        if (EscapeMenu.paused || peeking) return;

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

            if (Input.GetButtonDown(tetherInput) && !EscapeMenu.paused)
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
        if (EscapeMenu.paused) return;

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

        //gas refill
        gasHoldAvailable = false;
        drag = defaultDrag;
        if (gasAmount > 30)
            gasAmount = 30;
        else if (gasAmount < 30)
            gasAmount += gasRefillSpeed * Time.deltaTime;

        if (EscapeMenu.paused)
            return;

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
    }
    private IEnumerator GasDelay()
    {
        yield return new WaitForSeconds(.8f);
        rb.velocity *= gasBoost;
    }










    //Missiles:
    private void Missile() //run in update
    {
        //update meter
        missileScaler.localScale = new Vector2(missileScaler.localScale.x, missileAmount / 30);
        missileAmountImage.color = missileAmount > 10 ? Color.blue : Color.gray;

        //refill meter
        if (missileAmount > 30)
            missileAmount = 30;
        else if (missileAmount < 30)
            missileAmount += missileRefillSpeed * Time.deltaTime;

        //fire missile
        if (Input.GetButtonDown("Fire") && missileAmount > 10 && !EscapeMenu.paused)
        {
            missileAmount -= 10;
            CreateMissile(transform.position, transform.rotation, 0);
            RpcServerCreateMissile(transform.position, transform.rotation, TimeManager.Tick);
        }

        //MissileTimer();
    }
    private const float maxPassedTime = 0.3f; //never change this!
    [ServerRpc]
    private void RpcServerCreateMissile(Vector3 firePosition, Quaternion fireRotation, uint tick)
    {
        if (!IsOwner)
        {
            float passedTime = (float)TimeManager.TimePassed(tick, false); //false prevents negative
            passedTime = Mathf.Min(maxPassedTime / 2f, passedTime);

            CreateMissile(firePosition, fireRotation, passedTime);
        }

        RpcClientCreateMissile(firePosition, fireRotation, tick);
    }
    [ObserversRpc]
    private void RpcClientCreateMissile(Vector3 firePosition, Quaternion fireRotation, uint tick)
    {
        if (IsServer || IsOwner)
            return;

        float passedTime = (float)TimeManager.TimePassed(tick, false); //false prevents negative
        passedTime = Mathf.Min(maxPassedTime / 2f, passedTime);

        CreateMissile(firePosition, fireRotation, passedTime);
    }
    private void CreateMissile(Vector3 firePosition, Quaternion fireRotation, float passedTime)
    {
        Missile newMissile = objectPool.GetPooledMissile();
        //missileObject = newMissile.gameObject; //used for missile timer

        float displacementMagnitude = passedTime / 13.29f; //(number of ticks missile has already traveled) / 13.29 = the distance the missile has traveled
        Vector3 fireForward = fireRotation * Vector3.forward;
        Vector3 displacement = newMissile.missileSpeed * displacementMagnitude * fireForward;
        Vector3 missilePosition = firePosition += displacement;

        newMissile.Launch(!IsOwner, this, missilePosition, fireRotation);
    }

    //missile timer code used to initially test the average distance a missile travels per tick
    //(13.29 according to last test)
    //private readonly List<float> distancesPerTick = new();
    //private int ticks = 0;
    //private GameObject missileObject;
    //private Vector3 cachedMissilePosition;
    //private void MissileTimer() //run in update
    //{
    //    if (missileObject != null && TimeManager.Tick > ticks)
    //    {
    //        ticks = (int)TimeManager.Tick;

    //        if (cachedMissilePosition != default)
    //            distancesPerTick.Add(Vector3.Distance(cachedMissilePosition, missileObject.transform.position));
    //        cachedMissilePosition = missileObject.transform.position;
    //    }
    //    else if (missileObject == null)
    //        cachedMissilePosition = default;

    //    float total = 0f;
    //    foreach (float f in distancesPerTick)  //Calculate the total of all floats
    //        total += f;
    //    Debug.Log(total / distancesPerTick.Count); //average
    //}


















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