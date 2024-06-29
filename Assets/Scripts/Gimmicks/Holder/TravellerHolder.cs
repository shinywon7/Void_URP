using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TravellerHolder : Holder
{
    VoidTraveller traveller;
    public void Start()
    {
        if(backObject) traveller = connectedHolder.transform.GetComponent<VoidTraveller>();
        else traveller = GetComponent<VoidTraveller>();
    }
    public override void Flip(Gun gun){
        gun.holder = connectedHolder;
    }
    public override void VoidFade(Gun gun){
        if(backObject) gun.holder = connectedHolder;
    }
    public override void SetVelocity(Vector3 velocity){
        if(VoidManager.isVoidGenerated) connectedHolder.rb.velocity = velocity;
        rb.velocity = velocity;
    }
}
