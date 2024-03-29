﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// End of level screen trigger
/// </summary>
public class LevelEndTrigger : MonoBehaviour
{

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            other.SendMessage("EndReached");
        }
    }
}
