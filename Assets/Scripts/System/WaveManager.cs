using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WaveManager :  MonoBehaviour
{
    Vector4 wave1, wave2, wave3;

    public static void GetDistanceToWave(Vector3 point){
        point = VoidManager.voidTransform.InverseTransformPoint(point);
        Debug.Log(point);
    }
}
