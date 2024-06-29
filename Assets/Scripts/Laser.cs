using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;

public class Laser : MonoBehaviour
{
    public VisualEffect source, originLaser, cloneLaser, Fragment;
    public VisualEffect inLaser, outLaser;
    public GameObject sourceObj, originLaserObj, cloneLaserObj, FragmentObj;
    public float sameSide = 1;
    public RaycastHit hitInfo;
    public LayerMask inMask, outMask;
    LayerMask originMask;
    // Start is called before the first frame update
    void Start()
    {
        Player.flip += SideFlip;
        VoidManager.voidSet += VoidSet;
        VoidManager.voidFade += VoidFade;
        SideSet(true);
    }
    public void VoidSet()
    {
        //clone.SetActive(true);
        SideSet(VoidManager.voidPlane.GetDistanceToPoint(transform.position) > 0);
    }
    public void VoidFade()
    {
        SideSet(true);
    }
    public virtual void SideFlip()
    {
        sameSide *= -1;
        SideSet(sameSide == 1);
    }
    void SideSet(bool flag)
    {
        if (flag)
        {
            sameSide = 1;
            sourceObj.layer = LayerMask.NameToLayer("Main");
            originLaserObj.layer = LayerMask.NameToLayer("Main");
            cloneLaserObj.layer = LayerMask.NameToLayer("Extra");
            originMask = inMask;
            originLaser.SetBool("IsReal", true);
            cloneLaser.SetBool("IsReal", false);
        }
        else
        {
            sameSide = -1;
            sourceObj.layer = LayerMask.NameToLayer("Extra");
            originLaserObj.layer = LayerMask.NameToLayer("Extra");
            cloneLaserObj.layer = LayerMask.NameToLayer("Main");
            originMask = outMask;
            originLaser.SetBool("IsReal", false);
            cloneLaser.SetBool("IsReal", true);
        }
    }
    void Update()
    {
        Physics.Raycast(transform.position, transform.forward, out hitInfo, 10000f, originMask);
        originLaser.SetFloat("Length", hitInfo.distance);
        cloneLaser.SetFloat("Length", hitInfo.distance);
        cloneLaserObj.transform.position = transform.position + VoidManager.voidTransform.up * VoidManager.voidWidth * sameSide;
        float dist = VoidManager.voidPlane.GetDistanceToPoint(hitInfo.point) * sameSide;
        if(dist > 0)
        {
            FragmentObj.transform.position = hitInfo.point;
            FragmentObj.transform.LookAt(hitInfo.point + hitInfo.normal);
        }
        else if(dist < -VoidManager.voidWidth)
        {
            Vector3 point = hitInfo.point + VoidManager.voidTransform.up * VoidManager.voidWidth * sameSide;
            FragmentObj.transform.position = point;
            FragmentObj.transform.LookAt(point + hitInfo.normal);
        }
        else{
            FragmentObj.transform.position = VoidManager.DeactiveTransform.position;
        }
    }
}
