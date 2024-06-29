using System.Collections;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEditor.Callbacks;
using UnityEditor.Rendering;
using UnityEngine;
using UnityEngine.UIElements;

public class VoidTraveller : MonoBehaviour
{
    public Vector3 gravity = new(0,-9.8f,0);
    Vector3 buoyancyBound;
    Transform mainTransform;
    Rigidbody mainRb, subRb;
    Rigidbody frontRb;
    Rigidbody backRb;
    GameObject front;
    GameObject back;
    float side;
    float voidDist;
    Material mat;

    public virtual void Start()
    {
        front = transform.gameObject;
        frontRb = GetComponent<Rigidbody>();
        mat = GetComponent<MeshRenderer>().material;
        back = Instantiate(front);
        Destroy(back.GetComponent<VoidTraveller>());
        back.layer = LayerMask.NameToLayer("TravellerBack");
        backRb = back.GetComponent<Rigidbody>();
        back.SetActive(false);

        Holder holder = GetComponent<Holder>();
        if(holder != null){
            Holder connectedHolder = back.GetComponent<Holder>();
            holder.connectedHolder = connectedHolder;
            connectedHolder.connectedHolder = holder;
            connectedHolder.backObject = true;
        }

        Bounds bound = GetComponent<MeshFilter>().sharedMesh.bounds;
        buoyancyBound = Vector3.Scale(transform.localScale, bound.size)/2;
        VoidManager.voidSet += VoidSet;
        VoidManager.voidFade += VoidFade;
    }

    // Update is called once per frame
    public virtual void Update()
    {
        if (VoidManager.isVoidGenerated)
        {
            voidDist = side * VoidManager.voidPlane.GetDistanceToPoint(mainTransform.position);
            //backActive = voidDist < 1;
            if (voidDist < -VoidManager.halfVoidWidth)
            {
                side *= -1;
                SideSet();
            }
        }
        SetMaterial();
    }
    public virtual void VoidSet()
    {
        side = VoidManager.voidPlane.GetDistanceToPoint(transform.position) > 0 ? 1 : -1;
        SideSet();
        back.SetActive(true);
        backRb.position = frontRb.position;
        backRb.rotation = frontRb.rotation;
        //backRb.velocity = frontRb.velocity;
        //backRb.angularVelocity = frontRb.angularVelocity;
    }
    public void SideSet()
    {
        if (side > 0)
        {
            mainRb = frontRb;
            subRb = backRb;
            mainTransform = front.transform;
        }
        else
        {
            mainRb = backRb;
            subRb = frontRb; 
            mainTransform = back.transform;
        }
        //mainRb.isKinematic = false;
        mainRb.velocity = subRb.velocity;
        mainRb.angularVelocity = subRb.angularVelocity;
        //subRb.isKinematic = true;

    }

    public virtual void VoidFade()
    {
        side = 1;
        SideSet();
        back.SetActive(false);
    }
    public virtual void FixedUpdate()
    {
        AddGravity();
        if(back.activeSelf) Sync();
    }
    public void AddGravity(){
        if(!VoidManager.isVoidGenerated) frontRb.AddForce(gravity, ForceMode.Acceleration);
        else{
            Vector3 buoyancyAxis = transform.TransformVector(Vector3.Scale(transform.InverseTransformVector(VoidManager.voidTransform.up), buoyancyBound)); 
            float halfWidth = buoyancyAxis.magnitude;
            float location = Mathf.Clamp(voidDist/halfWidth,-1,1)*0.5f;
            float r1 = 0.5f + location;
            float r2 = 0.5f - location;
            Vector3 p1 = buoyancyAxis * r2;
            Vector3 p2 = buoyancyAxis * r1;
            mainRb.AddForceAtPosition(gravity * r1, mainRb.worldCenterOfMass + p1, ForceMode.Acceleration);
            mainRb.AddForceAtPosition(VoidManager.gravity * r2, mainRb.worldCenterOfMass - p2, ForceMode.Acceleration);
        }
        
    }

    public void SetMaterial(){
        Vector4 velocity = frontRb.velocity;
        Vector4 angularVelocity = frontRb.angularVelocity;
        mat.SetVector("_Velocity",transform.localToWorldMatrix*velocity);
        mat.SetVector("_AngularVelocity", transform.localToWorldMatrix*angularVelocity);
    }

    public virtual void Sync()
    {
        //var befshift = VoidManager.fixedBefVoidWidth * VoidManager.initVoidNormal * side;
        var shift = VoidManager.fixedVoidWidth * VoidManager.initVoidNormal * side;
        //var position = (frontRb.position + backRb.position - befshift)/2;
        //var rotation = Quaternion.Lerp(frontRb.rotation, backRb.rotation, 0.5f);
        //var velocity = (frontRb.velocity + backRb.velocity)/2;
        //var angularVelocity = (frontRb.angularVelocity+backRb.angularVelocity)/2;
        
        subRb.position = mainRb.position + shift;
        subRb.rotation = mainRb.rotation;

        //if(side > 0){
        //    frontRb.position = position;
        //    backRb.position = frontRb.position + shift;
        //}
        //else{
        //    backRb.position = position;
        //    frontRb.position = position + shift;
        //}
        //frontRb.rotation = rotation;
        //frontRb.velocity = velocity;
        //frontRb.angularVelocity = angularVelocity;
        //backRb.rotation = rotation;
        //backRb.velocity = velocity;
        //backRb.angularVelocity = angularVelocity;
    }
    // public void SetVelocity(Vector3 velocity){
    //     frontRb.velocity = velocity;
    //     if(back.activeSelf) backRb.velocity =velocity;
    // }
}
