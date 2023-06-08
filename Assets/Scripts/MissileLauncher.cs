using FishNet.Managing.Timing;
using FishNet.Object;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MissileLauncher : NetworkBehaviour
{
    //assigned in scene
    public ObjectPool objectPool;

    private const float maxPassedTime = 0.3f; //never change this!

    public void Fire(MissileInfo info) //called by player and turret
    {
        if (IsServer)
            ServerCreateMissile(info, 0);
        else //if clientonly
        {
            CreateMissile(info, 0); //create missile on this client before sending to server
            RpcServerFire(info, TimeManager.Tick);
        }
    }
    [ServerRpc (RequireOwnership = false)]
    private void RpcServerFire(MissileInfo info, uint tick)
    {
        ServerCreateMissile(info, tick);
    }

    [Server]
    private void ServerCreateMissile(MissileInfo info, uint tick)
    {
        float passedTime = (float)TimeManager.TimePassed(tick, false); //false prevents negative
        passedTime = Mathf.Min(maxPassedTime / 2f, passedTime);
        
        CreateMissile(info, passedTime);

        RpcClientCreateMissile(info, tick);
    }
    [ObserversRpc]
    private void RpcClientCreateMissile(MissileInfo info, uint tick)
    {
        if (IsServer) //missile has already been created on the server
            return;
        if (info.launcher.IsOwner) //missile has already been created on this client
            return;

        float passedTime = (float)TimeManager.TimePassed(tick, false); //false prevents negative
        passedTime = Mathf.Min(maxPassedTime / 2f, passedTime);

        CreateMissile(info, passedTime);
    }
    private void CreateMissile(MissileInfo info, float passedTime)
    {
        Missile newMissile = objectPool.GetPooledMissile();
        //missileObject = newMissile.gameObject; //used for missile timer

        float displacementMagnitude = passedTime / 13.29f; //(number of ticks missile has already traveled) / 13.29 = the distance the missile has traveled
        Vector3 fireForward = info.fireRotation * Vector3.forward;
        Vector3 displacement = newMissile.missileSpeed * displacementMagnitude * fireForward;
        Vector3 missilePosition = info.firePosition += displacement;

        newMissile.Launch(!info.launcher.IsOwner, info.launcher, missilePosition, info.fireRotation);
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

}
public struct MissileInfo
{
    public Vector3 firePosition;
    public Quaternion fireRotation;
    public Player launcher;
}