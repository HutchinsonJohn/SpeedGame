using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    Rigidbody[] rigidbodies;
    bool isAlive = true;

    // Start is called before the first frame update
    void Start()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        ToggleRagdoll(true);
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void Killed()
    {
        if (isAlive)
        {
            ToggleRagdoll(false);
            isAlive = false;
        }
    }
    
    public void ToggleRagdoll(bool v)
    {
        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.isKinematic = v;
        }
    }
}
