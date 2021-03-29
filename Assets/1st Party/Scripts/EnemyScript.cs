using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    Rigidbody[] rigidbodies;
    bool isAlive = true;

    public Transform mainCamera;
    public Rigidbody rb;

    public bool wasShot;

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

    public void Killed(bool wasShot)
    {
        if (isAlive)
        {
            this.wasShot = wasShot;
            ToggleRagdoll(false);
            isAlive = false;
        }
    }
    
    public void ToggleRagdoll(bool v)
    {
        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.isKinematic = v;
            if (!v)
            {
                if (wasShot)
                {
                    rigidbody.AddForce(mainCamera.forward * 500);
                } else
                {
                    rigidbody.AddForce(mainCamera.forward * 500 + rb.velocity * 100);
                }
            }
        }
    }
}
