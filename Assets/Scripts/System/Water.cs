using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Water : MonoBehaviour
{
    public Camera _camera;
    public Transform waterTransform;
    Material _water;
    void Start(){
        _water = waterTransform.GetComponent<MeshRenderer>().material;
    }
    void LateUpdate()
    {
        
        Vector3 offset = transform.InverseTransformPoint(_camera.transform.position);
        offset = new Vector3(offset.x,0,offset.z);
        waterTransform.localPosition = offset;
        _water.SetVector("_Offset",offset);
    }
}
