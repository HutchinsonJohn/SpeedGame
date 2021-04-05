using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swing : MonoBehaviour
{

    private bool swingDown;
    private bool swinging;

    public LayerMask enemy;

    public Animator swordAnimator;
    public Animator gunAnimator;

    public Transform mainCamera;

    public Transform Player;

    public float swordLength;

    // This variable is moronic hard coding in action
    // The animation has 40 frames played at 60 fps, but sped up, fixedUpdate is updated 50 times a second so some ugly ass approximations have to be made
    // This is all subject to be changed and will ruin this code
    private int animationFrames = 0;  

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void LateUpdate()
    {
        swingDown = Input.GetButtonDown("Fire2");
        if (swingDown)
        {
            swordAnimator.SetTrigger("Swing");
            gunAnimator.SetTrigger("Swing");
            swinging = true;
        }
    }

    void FixedUpdate()
    {
        if (swinging)
        {
            if (animationFrames > 5 && animationFrames < 15)
            {
                Collider[] hits = Physics.OverlapCapsule(mainCamera.position + mainCamera.forward, mainCamera.position + mainCamera.forward * 2, 1, enemy);
                foreach (Collider hit in hits)
                {
                    hit.transform.SendMessageUpwards("Killed", false, SendMessageOptions.DontRequireReceiver);
                    // TODO: Change layer of enemy hit to dead enemy layer
                    Player.SendMessage("RefillBoost", SendMessageOptions.DontRequireReceiver);
                }
                animationFrames++;
            } else if (animationFrames > 14)
            {
                swinging = false;
                animationFrames = 0;
            } else
            {
                animationFrames++;
            }
        }
    }

    //Hitbox visualization
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mainCamera.position + mainCamera.forward, 1);
        Gizmos.DrawSphere(mainCamera.position + mainCamera.forward * 2, 1);
    }
}
