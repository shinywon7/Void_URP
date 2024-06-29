using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class BarrierColliders : MonoBehaviour
{
    void Start()
    {
        VoidManager.voidSet += VoidSet;
        VoidManager.voidFade += VoidFade;
        VoidManager.voidUpdate += VoidUpdate;
    }
    void VoidSet()
    {
        transform.SetPositionAndRotation(VoidManager.SectionTransform.position, VoidManager.SectionTransform.rotation);
    }
    void VoidUpdate()
    {
        transform.localScale = new Vector3(1, 1, VoidManager.voidWidth);
    }
    void VoidFade()
    {
        transform.SetPositionAndRotation(VoidManager.DeactiveTransform.position, VoidManager.DeactiveTransform.rotation);
    }
}
