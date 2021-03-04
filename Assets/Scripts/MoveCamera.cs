using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MoveCamera : MonoBehaviour 
{ 

    public Transform player;

    // Start is called before the first frame update
    void Start()
    {
        //QualitySettings.vSyncCount = 0;
        //Application.targetFrameRate = 500;
    }

    // Update is called once per frame
    void Update()
    {
        transform.position = player.transform.position;
    }
}
