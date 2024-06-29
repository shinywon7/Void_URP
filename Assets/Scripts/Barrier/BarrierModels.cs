using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BarrierModels : MonoBehaviour
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
        transform.position = VoidManager.voidTransform.position - VoidManager.voidTransform.up * VoidManager.halfVoidWidth;
        transform.localScale = new Vector3(1, 1, VoidManager.voidWidth);
    }
    void VoidFade()
    {
        transform.SetPositionAndRotation(VoidManager.DeactiveTransform.position, VoidManager.DeactiveTransform.rotation);
    }
}
