using System.Collections;
using System.Collections.Generic;
using System.Reflection.Emit;
using Unity.Mathematics;
using UnityEngine;

public class Test : MonoBehaviour
{
    public Rigidbody rb, syncRb;
    public bool main;
    private void FixedUpdate() {
        if (main){
            Vector3 pos = (rb.position + syncRb.position)/2;
            Vector3 vel = (rb.velocity + syncRb.velocity)/2;
            Quaternion rot = Quaternion.Lerp(rb.rotation, syncRb.rotation, 0.5f);
            Vector3 angVel = Vector3.Lerp(rb.angularVelocity, syncRb.angularVelocity, 0.5f);
            rb.velocity = vel;
            syncRb.velocity = vel;
            rb.position = pos;
            syncRb.position = pos;
            rb.rotation = rot;
            syncRb.rotation = rot;
            rb.angularVelocity = angVel;
            syncRb.angularVelocity = angVel;
            Physics.SyncTransforms();
            // syncRb.position = rb.position;
            // syncRb.velocity = rb.velocity;
            // syncRb.rotation = rb.rotation;
            // syncRb.angularVelocity = rb.angularVelocity;
        }
    }    
    void syncCollide(Collision other){
        return;
        if(main) return;
        int n = other.contactCount;
        for(int i = 0; i < n; i++){
            syncRb.AddForceAtPosition(other.impulse/n,other.contacts[i].point,ForceMode.Impulse);
        }
    }
    private void OnCollisionEnter(Collision other) {
        syncCollide(other);
    }
    private void OnCollisionStay(Collision other) {
        syncCollide(other);
    }
    private void OnCollisionExit(Collision other) {
        syncCollide(other);
    }
}
