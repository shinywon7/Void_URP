using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Mathematics;

public class Gun : MonoBehaviour
{
    public Transform HoldingTarget;
    public LayerMask frontLayerMask, backLayerMask;
    LayerMask layerMask, flipLayerMask;
    public static RaycastHit hitInfo;
    public Holder holder = null;
    bool isHoldingObject;
    Vector3 px, x, dx,y, dy;
    float w,z,d,k1, k2, k3;
    [Range(0, 15)]
    public float _f;
    [Range(0,5)]
    public float _z;
    [Range(-5,5)]
    public float _r;
    void Start()
    {
        layerMask = frontLayerMask;
        flipLayerMask = frontLayerMask ^ backLayerMask;
        SecondOrderDynamics();
        Player.flip += VoidFlip;
        VoidManager.voidFade += VoidFade;
    }

    void Update()
    {
        if(Input.GetMouseButtonDown(1)){
            TryInteract();
        }
        
    }

    public void TryInteract()
    {
        if (!isHoldingObject && Physics.Raycast(Player.headTransform.position, Player.headTransform.forward, out hitInfo, 1000f, layerMask))
        {
            if (!VoidManager.isVoidGenerated && hitInfo.transform.tag == "VoidGenerator") {
                VoidManager.VoidGenerate(hitInfo);
            }
            else if (hitInfo.transform.tag == "YellowCube") {
                isHoldingObject = true;
                holder = hitInfo.transform.GetComponent<Holder>();
                y = holder.GetPosition();
                px = HoldingTarget.position;
                dy = Vector3.zero;
            }
        }
        else if(isHoldingObject){
            isHoldingObject = false;
            holder = null;
        }
    }
    void VoidFade(){
        layerMask = frontLayerMask;
        if(isHoldingObject){
            holder.VoidFade(this);
        }
    }
    void VoidFlip(){
        layerMask ^= flipLayerMask;
        if(isHoldingObject){
            px -= VoidManager.voidTransform.up * VoidManager.voidWidth;
            holder.Flip(this);
        }
    }
    void LateUpdate(){
        if(isHoldingObject) {
            float T = Time.deltaTime;
            x = HoldingTarget.position;
            y = holder.GetPosition();
            dx = (x - px) / T;
            px = x;
            float k1_stable, k2_stable;
            if (w * T < z) {
                k1_stable = k1;
                k2_stable = Mathf.Max(k2, T * T / 2 + T * k1 / 2, T * k1);
            }
            else {
                float t1 = Mathf.Exp(-z * w * T);
                float alpha = 2 * t1 * (z <= 1 ? Mathf.Cos(T * d) : math.cosh(T * d));
                float beta = t1 * t1;
                float t2 = T / (1 + beta - alpha);
                k1_stable = (1 - beta) * t2;
                k2_stable = T * t2;
            }
            y = y + T * dy;
            dy = dy + T * (x + k3 * dx - y - k1_stable * dy) / k2_stable;
        }
    }
    void FixedUpdate() {
        if(isHoldingObject) {
            holder.SetVelocity(dy);
        }
    }
    public void SecondOrderDynamics() {
        w = 2 * Mathf.PI * _f;
        z = _z;
        d = w * Mathf.Sqrt(Mathf.Abs(_z * _z - 1));
        k1 = _z / (Mathf.PI * _f);
        k2 = 1 / (w * w);
        k3 = _r * _z / w;
    }
}
