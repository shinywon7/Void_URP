using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Timeline;

public class WaveInteractor : MonoBehaviour
{
    Rigidbody rb;
    Material mat;
    Transform camTransform;
    Matrix4x4 l2cMatrix;
    public float force, torque;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        mat = GetComponent<MeshRenderer>().material;
    }
    
    private void FixedUpdate() {
        float x = Input.GetAxis("Horizontal");
        float y = Input.GetAxis("Vertical");
        rb.AddForce(Vector3.forward*y*force);
        rb.AddTorque(Vector3.up*x*torque);
    }

    private void Update() {
        Vector4 centerOfMass = rb.centerOfMass;
        centerOfMass.w = 1;
        mat.SetVector("_CenterOfMass", transform.localToWorldMatrix*centerOfMass);
        mat.SetVector("_Velocity",rb.velocity);
        mat.SetVector("_AngularVelocity",rb.angularVelocity);
    }
}
