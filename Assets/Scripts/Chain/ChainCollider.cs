using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ChainCollider : MonoBehaviour
{
    public bool onCollision = false;
    void OnCollisionEnter(Collision collision)
    {
        onCollision = true;
    }
    void OnCollisionExit(Collision collision)
    {
        onCollision = false;
    }

    void OnDrawGizmos()
    {
        if (onCollision) Gizmos.color = Color.red;
        else Gizmos.color = Color.blue;
        Gizmos.DrawSphere(transform.position, 0.5f);
    }
}
