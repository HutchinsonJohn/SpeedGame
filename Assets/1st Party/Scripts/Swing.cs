using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Swing : MonoBehaviour
{

    private bool swingDown;

    public LayerMask enemy;

    public Animator swordAnimator;
    public Animator gunAnimator;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        swingDown = Input.GetButtonDown("Fire2");
        if (swingDown)
        {
            swordAnimator.SetTrigger("Swing");
            gunAnimator.SetTrigger("Swing");
        }
    }
}
