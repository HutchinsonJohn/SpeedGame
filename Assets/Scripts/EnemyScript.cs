using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyScript : MonoBehaviour
{
    Rigidbody[] rigidbodies;
    bool isRagdoll = false;

    // Start is called before the first frame update
    void Start()
    {
        rigidbodies = GetComponentsInChildren<Rigidbody>();
        ToggleRagdoll(true);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetButtonDown("Fire2"))
        {
            ToggleRagdoll(false);
            Debug.Log("we here");
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        
    }
    
    private void ToggleRagdoll(bool v)
    {
        isRagdoll = !v;

        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.isKinematic = v;
        }
    }
}
