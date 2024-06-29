using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Section : MonoBehaviour
{
    void Start()
    {
        VoidManager.voidSet += VoidSet;
        VoidManager.voidFade += VoidFade;
    }
    void VoidSet()
    {
        transform.SetPositionAndRotation(VoidManager.SectionTransform.position, VoidManager.SectionTransform.rotation);
    }
    void VoidFade()
    {
        transform.SetPositionAndRotation(VoidManager.DeactiveTransform.position, VoidManager.DeactiveTransform.rotation);
    }
    void LateUpdate(){
        
    }
}
