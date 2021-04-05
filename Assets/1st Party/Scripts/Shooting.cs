using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Shooting : MonoBehaviour
{

    private bool shootDown;

    public Transform gunTip, mainCamera;

    public LayerMask enemy;

    public float bulletSize = 0.1f;

    private FieldOfView fow;

    // Start is called before the first frame update
    void Start()
    {
        fow = GetComponent<FieldOfView>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        //fow.FindTarget();
        shootDown = Input.GetButtonDown("Fire1");
        if (shootDown)
        {
            if (Physics.SphereCast(mainCamera.position, bulletSize, mainCamera.forward, out RaycastHit hit))
            {
                if(enemy == (enemy | (1 << hit.collider.gameObject.layer)))
                {
                    hit.transform.SendMessageUpwards("Killed", true, SendMessageOptions.DontRequireReceiver);
                }
            }
            //if (Physics.Raycast(mainCamera.position, mainCamera.forward, out hit, 10000))
            //{
            //    if (enemy == (enemy | (1 << hit.collider.gameObject.layer)))
            //    {
            //        hit.transform.SendMessageUpwards("Killed", SendMessageOptions.DontRequireReceiver);
            //    }
            //}
        }
    }
}
