using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour
{

    private bool shootDown;

    public Transform gunTip, mainCamera;

    public LayerMask enemy;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        shootDown = Input.GetButtonDown("Fire1");
        RaycastHit hit;
        if (shootDown)
        {
            if (Physics.Raycast(mainCamera.position, mainCamera.forward, out hit, 10000))
            {
                if (enemy == (enemy | (1 << hit.collider.gameObject.layer)))
                {
                    hit.transform.SendMessageUpwards("Killed", SendMessageOptions.DontRequireReceiver);
                }
            }
        }
    }
}
