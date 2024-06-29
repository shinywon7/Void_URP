using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.VFX;
using DG.Tweening;
using System;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Unity.Mathematics;



public class VoidManager : MonoBehaviour
{
    public static VoidManager Inst { get; private set; }
    void Awake() => Inst = this;

    [Range(0.0f, 30.0f)]
    public static float destVoidWidth;
    public static float voidWidth = 0.01f;
    public static float befVoidWidth = 0.01f;
    public static float deltaVoidWidth;
    public static float fixedBefVoidWidth;
    public static float fixedVoidWidth;
    public static float halfVoidWidth = 0f;
    public float maxVoidWidth;
    public float minVoidWidth;
    public float appearSpeed = 3f;
    public float disappearSpeed = 3f;

    float px, x, dx, dy;
    float w,z,d,k1, k2, k3;
    [Range(0.1f, 15)]
    public float _f;
    [Range(0.1f,5)]
    public float _z;
    [Range(-5,5)]
    public float _r;

    public static Transform voidTransform;
    public static Transform DeactiveTransform;
    public static Transform SectionTransform;
    public static Camera voidCam;
    public static float voidAge;

    public static bool isVoidGenerated;
    public static bool isVoidChangeable;
    public static bool isVoidDisappearing;

    public static bool isShaking = false;
    public static float limitVoidWidth;

    public static Transform playerTransform;

    public static Plane voidPlane;

    public static Action voidFade;
    public static Action voidSet;
    public static Action voidRearrange;
    public static Action voidUpdate;
    public static Action setLimit;

    public static Vector3 initVoidNormal;
    public static Vector3 gravity = new Vector3(0,-9.8f,0);
    //public WaveSurface waveSurface;

    void Start()
    {
        playerTransform = GameObject.Find("Player").transform;
        voidTransform = transform;
        DeactiveTransform = GameObject.Find("DeactiveLoca").transform;
        SectionTransform = GameObject.Find("SectionLoca").transform;
        voidWidth = minVoidWidth;
        destVoidWidth = minVoidWidth;
        limitVoidWidth = 100f;
        //waveSurface = GetComponent<WaveSurface>();
        SecondOrderDynamics();
        setPlane();
    }
    void setPlane()
    {
        voidPlane.SetNormalAndPosition(transform.up, transform.position);
        
    }

    void Update()
    {
        if (!isVoidGenerated && !isVoidChangeable)
        {
            destVoidWidth = minVoidWidth;
            //Clip.Inst.flag = 1;
        }
        else
        {
            if (destVoidWidth < 2)
            {
                destVoidWidth = minVoidWidth;
                isVoidChangeable = false;
            }
            if (voidWidth < 0.1 && !isVoidChangeable)
            {   
                if(!isVoidDisappearing) {
                    isVoidDisappearing = true;
                    voidAge = 0.1f;
                    Shader.SetGlobalFloat("_VoidAppear", 0);
                }
                if(isVoidDisappearing){
                    voidAge -= Time.deltaTime * disappearSpeed;
                }
                if(voidAge < -1f){
                    voidWidth = minVoidWidth;
                    transform.SetPositionAndRotation(DeactiveTransform.position, DeactiveTransform.rotation);
                    isVoidGenerated = false;
                    isVoidDisappearing = false;
                    setPlane();
                    MyRenderPass.flipped = false;
                    voidFade.Invoke();
                }
            }
            if (isVoidChangeable)
            {
                destVoidWidth += Input.mouseScrollDelta.y * 0.8f;
                destVoidWidth = Mathf.Clamp(destVoidWidth, 0.1f, maxVoidWidth);
                limitVoidWidth = 100f;
                setLimit?.Invoke();
                voidAge += Time.deltaTime*appearSpeed;
            }
        }
        WidthUpdate();
        // if(limitVoidWidth < voidWidth)
        // {
        //     voidWidth = limitVoidWidth;
        //     destVoidWidth = limitVoidWidth;
        //     if (!isShaking)
        //     {
        //         float var = voidVelocity / 10;
        //         StartCoroutine(Shake(Mathf.Clamp(var,0.8f,1.3f), Mathf.Clamp(var, 0.3f, 0.4f)));
        //     }
        //     voidVelocity = 0f;
        // }
        halfVoidWidth = voidWidth / 2.0f;
        voidRearrange?.Invoke();

        UpdateVoid();
    }
    void FixedUpdate() {
        fixedBefVoidWidth = fixedVoidWidth;
        fixedVoidWidth = voidWidth;    
    }
    void WidthUpdate(){
        befVoidWidth = voidWidth;
        float T = Time.deltaTime;
        x = destVoidWidth;
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
        voidWidth = voidWidth + T * dy;
        if(voidWidth < minVoidWidth) voidWidth = minVoidWidth;
        dy = dy + T * (x + k3 * dx - voidWidth - k1_stable * dy) / k2_stable;
        deltaVoidWidth = voidWidth - befVoidWidth;
    }
    public void SecondOrderDynamics() {
        w = 2 * Mathf.PI * _f;
        z = _z;
        d = w * Mathf.Sqrt(Mathf.Abs(_z * _z - 1));
        k1 = _z / (Mathf.PI * _f);
        k2 = 1 / (w * w);
        k3 = _r * _z / w;
    }

    Vector3 shakeVec;
    IEnumerator Shake(float duration, float strength)
    {
        isShaking = true;
        DOTween.Shake(()=>shakeVec, x=>shakeVec=x, duration, strength);
        yield return new WaitForSeconds(duration+0.5f);
        isShaking = false;
    }
    public void UpdateVoid()
    {
        //waveSurface.backSurface.position = transform.position - voidTransform.up * voidWidth;
        voidUpdate.Invoke();
    }
    void LateUpdate()
    {
        MaterialClip();
    }
    
    public static void VoidFlip()
    {
        voidTransform.Rotate(Vector3.forward, 180.0f);
        playerTransform.position -= voidTransform.up * voidWidth;
        MyRenderPass.flipped = !MyRenderPass.flipped;
        Physics.SyncTransforms();
    }
    
    public static void VoidGenerate(RaycastHit hitInfo)
    {
        isVoidGenerated = true;
        isVoidChangeable = true;
        voidTransform.SetPositionAndRotation(hitInfo.point, Quaternion.LookRotation(hitInfo.transform.up, hitInfo.transform.right * (hitInfo.transform.InverseTransformPoint(Player.playerTransform.position).x > 0 ? 1 : -1)));
        gravity = -hitInfo.transform.up *9.8f;
        gravity = new Vector3(0,-3.8f,0);
        destVoidWidth = 5f;
        voidAge = 0;
        Inst.setPlane();
        initVoidNormal = voidTransform.up;
        voidSet.Invoke();
        Shader.SetGlobalFloat("_VoidAppear", 1);
        //Debug.Break();
    }
    public static void VoidStop(float limitWidth)
    {
        limitVoidWidth = limitWidth;
        destVoidWidth = limitWidth;
    }
    //고쳐야 함
    void MaterialClip()
    {
        Shader.SetGlobalVector("_VoidGap", transform.up*voidWidth);
        Shader.SetGlobalFloat("_VoidAge", voidAge);
        Shader.SetGlobalVector("_VoidNormal", transform.up);
        Shader.SetGlobalFloat("_VoidWidth", voidWidth);
        Shader.SetGlobalFloat("_HalfVoidWidth", halfVoidWidth);

        Shader.SetGlobalVector("_InsidePosition", transform.position);
        Shader.SetGlobalVector("_OutsidePosition", transform.position-transform.up*voidWidth);
    }
}
