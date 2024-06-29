using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Holder : MonoBehaviour
{
    [HideInInspector] public bool backObject = false;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public Holder connectedHolder;
    public virtual void Awake() {
        rb = GetComponent<Rigidbody>();
    }
    public Vector3 GetPosition(){
        return rb.position;
    }
    public virtual void Flip(Gun gun){
    }
    public virtual void VoidFade(Gun gun){
    }
    public virtual void SetVelocity(Vector3 velocity){
        rb.velocity = velocity;
    }
}
