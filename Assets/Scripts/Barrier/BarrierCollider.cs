using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BarrierCollider : MonoBehaviour
{
    public ObjectPooler pooler;
    public BarrierColliders parent;
    public GameObject BackSide, FrontSide;
    //bool sameSide = true;
    void OnEnable()
    {
        if (!pooler) { 
            pooler = GameObject.Find("BarrierColliders").GetComponent<ObjectPooler>();
            parent = GameObject.Find("BarrierColliders").GetComponent<BarrierColliders>();
            transform.parent = pooler.transform;
        }
    }
    public void Setup(Vector2 p1, Vector2 p2)
    {
        transform.localPosition = (p1+p2)/2;
        transform.LookAt(pooler.transform.TransformPoint(p1), VoidManager.voidTransform.up);
        float length = (p2 - p1).magnitude;
        transform.localScale = new Vector3(1, 1, length);
        SideSet();
        //sameSide =false;
        VoidManager.voidFade += VoidFade;
        //Player.flip += SideFlip;
    }
    public void VoidFade()
    {
        gameObject.SetActive(false);
    }
    // public void SideFlip()
    // {
    //     sameSide = !sameSide;
    //     SideSet(sameSide);
    // }
    public void SideSet()
    {
        FrontSide.layer = LayerMask.NameToLayer("TravellerFront");
        BackSide.layer = LayerMask.NameToLayer("TravellerBack");
        // if (flag)
        // {
        //     Inside.layer = LayerMask.NameToLayer("TravellerFront");
        //     Outside.layer = LayerMask.NameToLayer("TravelerBack");
        // }
        // else
        // {
        //     Inside.layer = LayerMask.NameToLayer("TravelerBack");
        //     Outside.layer = LayerMask.NameToLayer("TravellerFront");
        // }
    }
    void OnDisable()
    {
        VoidManager.voidFade -= VoidFade;
        //Player.flip -= SideFlip;
        pooler.ReturnToPool(gameObject);
    }
}
