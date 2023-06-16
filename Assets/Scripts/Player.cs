using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using FishNet.Object;
using System;
using FishNet.Object.Synchronizing;

public class Player : NetworkBehaviour
{
    //assigned in prefab:
    public Rigidbody rb;
    public SphereCollider col;
    public Transform anchors;
    public LineRenderer leftLineRenderer;
    public LineRenderer rightLineRenderer;
    public GameObject leftAnchor;
    public GameObject rightAnchor;
    public MeshRenderer playerRenderer;

    //assigned by Setup:
    [NonSerialized] public Camera mainCamera;

    [NonSerialized] public TMP_Text leftCrosshair;
    [NonSerialized] public TMP_Text rightCrosshair;

    [NonSerialized] public Transform gasScaler;
    [NonSerialized] public Image gasAmountImage;

    [NonSerialized] public Transform missileScaler;
    [NonSerialized] public Image missileAmountImage;

    [NonSerialized] public MissileLauncher missileLauncher;
    [NonSerialized] public EscapeMenu escapeMenu;
    [NonSerialized] public ScoreTracker scoreTracker;

    [NonSerialized] public List<GameObject> hearts = new();
    [NonSerialized] public List<Vector3> spawnPositions = new();

    //assigned dynamically:
    [SyncVar]
    private Color playerColor;
    private SpringJoint leftJoint;
    private SpringJoint rightJoint;

    private bool stunned;

    //custom gravity
    private readonly float gravityScale = 1.5f;

    //custom drag
    private readonly float defaultDrag = .25f;
    private float drag = .25f; //dynamic

    //rotate player with mouse
    private readonly float mediumRotateSpeed = 8;
    private readonly float sensitivityChangeAmount = 1.5f;

    //fires raycast through crosshairs
    private readonly float raycastOffset = .23f;

    //tether
    private readonly int maxTetherRange = 500;

    //reel
    private bool leftReeling;
    private bool rightReeling;
    private readonly float reelAmount = 50;
    //increase when reeling both tethers
    private readonly float doubleReelAmount = 100;

    //gas
    private readonly float gasBoost = 1.5f;//2;
    private float gasAmount = 30; //max 30
    private readonly float gasRefillSpeed = 8;
    private readonly float gasDrainSpeed = 4;
    //true when gas tap is successful:
    private bool gasHoldAvailable = false;

    //missile
    private float missileAmount = 30; //max 30
    private readonly float missileRefillSpeed = 8;

    //peek
    private bool peeking;

    //health/points/elimination
    [SyncVar]
    private int health = 5;
    private readonly float respawnTime = 3;

    public void OnSpawn(Color newColor) //run by Setup
    {
        if (IsServer)
            playerColor = newColor;

        anchors.SetParent(null);
        anchors.position = Vector3.zero;
        anchors.localScale = Vector3.one;
        if (IsOwner)
        {
            mainCamera.transform.SetParent(transform);
            mainCamera.transform.SetPositionAndRotation(transform.position, transform.rotation);
            mainCamera.enabled = true;
        }
    }
    
    private void OnDisable()
    {
        //anchors destroy when player disconnects
        if (anchors != null)
            Destroy(anchors.gameObject);
    }

    private void FixedUpdate()
    {
        if (!IsOwner || stunned) return;

        //custom gravity
        Vector3 gravity = -9.81f * gravityScale * Vector3.up;
        rb.AddForce(gravity, ForceMode.Acceleration);

        //custom drag
        rb.velocity *= 1 - (Time.fixedDeltaTime * drag);
    }

    private void Update()
    {
        playerRenderer.material.color = playerColor;

        if (!IsOwner || stunned) return;

        RotateWithMouse();

        LaunchTether1();
        ReelTether();

        Gas();

        Missile();

        Peek();
    }

    //mouse rotate
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

    //tether
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

    //gas
    private void Gas() //run in update
    {
        //update meter
        gasScaler.localScale = new Vector2(gasScaler.localScale.x, gasAmount / 30);
        gasAmountImage.color = gasAmount > 10 ? Color.white : Color.red;

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
            else
            {
                drag = defaultDrag;
                gasAmount = 0;
            }
            return; //don't start refilling until gas button is released
        }

        if (Input.GetButtonUp("Gas"))
        {
            gasHoldAvailable = false;
            drag = defaultDrag;
        }

        //gas refill
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

    //missile
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
            MissileInfo info = new()
            {
                firePosition = transform.position,
                fireRotation = transform.rotation,
                launcher = this
            };
            missileLauncher.Fire(info);
        }
    }

    //peek
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




    //health/points
    [Server]
    public void TakeDamage() //called by Missile
    {
        if (health <= 0) return;

        if (health == 1)
        {
            RpcHalvePoints();
            Eliminate();
        }

        ClientTakeDamage();
        health -= 1; //health is a syncvar
    }
    [ObserversRpc]
    private void ClientTakeDamage()
    {
        if (!IsOwner) return;

        //turn off one heart
        foreach (GameObject heart in hearts)
            if (heart.activeSelf)
            {
                heart.SetActive(false);
                return;
            }
    }
    [ObserversRpc]
    private void RpcHalvePoints()
    {
        if (IsOwner)
            scoreTracker.RpcHalveScore(GameManager.playerNumber);
    }

    [Server]
    public void EarnPoints(int amount) //called by Missile
    {
        //get playernumber from owner so that the correct score is changed
        RpcEarnPoints(amount);
    }
    [ObserversRpc]
    private void RpcEarnPoints(int amount)
    {
        if (IsOwner)
            scoreTracker.RpcChangeScore(GameManager.playerNumber, amount, false);
    }


    //eliminate/respawn
    [Server]
    private void Eliminate()
    {
        TurnPlayerOnOff(false);
        RpcClientTurnPlayerOnOff(false);
        StartCoroutine(RespawnCooldown());
    }
    [ObserversRpc]
    private void RpcClientTurnPlayerOnOff(bool on)
    {
        if (!IsServer)
            TurnPlayerOnOff(on);
    }
    //run on server and client
    private void TurnPlayerOnOff(bool on)
    {
        if (IsServer && on)
            health = 5; //health is a syncvar

        if (IsOwner)
        {
            //freeze rotation is default for player
            rb.constraints = on ? RigidbodyConstraints.FreezeRotation : RigidbodyConstraints.FreezeAll;
            stunned = !on;

            if (on)
            {
                gasHoldAvailable = false;
                drag = defaultDrag;
                gasAmount = 30;
                missileAmount = 30;

                foreach (GameObject heart in hearts)
                    heart.SetActive(true);

                escapeMenu.EliminateRespawn(false, 0, 0);

                int random = UnityEngine.Random.Range(0, spawnPositions.Count);
                transform.position = spawnPositions[random];
                transform.LookAt(new Vector3(0, transform.position.y, 0));
                mainCamera.transform.SetPositionAndRotation(transform.position, transform.rotation);
            }
            else
            {
                leftLineRenderer.enabled = false;
                rightLineRenderer.enabled = false;
                if (leftJoint != null) Destroy(leftJoint);
                if (rightJoint != null) Destroy(rightJoint);

                Vector3 cachedCameraPosition = mainCamera.transform.position; //must be world, not local
                transform.position = new Vector3(0, -1000, 0); //turrets don't fire if y position is below -900
                mainCamera.transform.position = cachedCameraPosition;
            }
        }
    }
    [Server]
    private IEnumerator RespawnCooldown()
    {
        RpcClientStartCooldown(TimeManager.Tick);
        yield return new WaitForSeconds(respawnTime);
        TurnPlayerOnOff(true);
        RpcClientTurnPlayerOnOff(true);
    }
    [ObserversRpc]
    private void RpcClientStartCooldown(uint tick)
    {
        if (!IsOwner) return;

        float serverTime = (float)TimeManager.TicksToTime(tick);
        float clientTime = (float)TimeManager.TicksToTime(TimeManager.Tick);
        float timeDifference = Mathf.Abs(serverTime - clientTime);

        escapeMenu.EliminateRespawn(true, respawnTime, timeDifference);
    }
}