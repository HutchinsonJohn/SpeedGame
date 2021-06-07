using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contains functions for finding targets in objects in field of view
/// </summary>
public class FieldOfView : MonoBehaviour
{

    //Either needs to be huge or removed
    public float viewRadius;
    [Range(0, 360)]
    public float viewAngle;

    public LayerMask targetMask;
    public LayerMask obstacleMask;

    [HideInInspector]
    public Transform bestTarget;

    /// <summary>
    /// Find target in fov from headPosition
    /// </summary>
    /// <param name="headPosition"></param>
    /// <returns></returns>
    public bool FindTarget(Vector3 headPosition)
    {
        Collider[] targetsInViewRadius = Physics.OverlapSphere(headPosition, viewRadius, targetMask);
        bestTarget = null;
        float lowestAngle = viewAngle;
        for (int i = 0; i < targetsInViewRadius.Length; i++)
        {
            Transform target = targetsInViewRadius[i].transform;
            Vector3 dirToTarget = (target.position - headPosition).normalized;
            float angleBetweenTargetAndLook = Vector3.Angle(transform.forward, dirToTarget);
            if (angleBetweenTargetAndLook < viewAngle / 2 && angleBetweenTargetAndLook < lowestAngle)
            {
                float disToTarget = Vector3.Distance(headPosition, target.position);

                if (!Physics.Raycast(headPosition, dirToTarget, disToTarget, obstacleMask))
                {
                    bestTarget = target;
                    lowestAngle = angleBetweenTargetAndLook;
                }
            }
        }
        return (bestTarget != null);
    }

    // <summary>
    /// Debug for displaying fov cone
    /// </summary>
    /// <param name="angleInDegrees"></param>
    /// <param name="angleIsGlobal"></param>
    /// <returns></returns>
    public Vector3 DirFromAngle(float angleInDegrees, bool angleIsGlobal)
    {
        if (!angleIsGlobal)
        {
            angleInDegrees += transform.eulerAngles.y;
        }
        return new Vector3(Mathf.Sin(angleInDegrees * Mathf.Deg2Rad), 0, Mathf.Cos(angleInDegrees * Mathf.Deg2Rad));
    }

}
