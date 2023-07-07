using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Slam : MonoBehaviour
{
    //this script is attached to the Player's hitbox, since it's the right size to prevent a bug where
    //the Slam sound is played multiple times when moving alongside a chunk made up of multiple fragments

    public Player player;

    private void OnTriggerEnter(Collider col)
    {
        player.Slam(col);
    }
}