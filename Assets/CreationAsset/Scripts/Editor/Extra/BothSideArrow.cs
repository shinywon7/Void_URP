using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class BothSideArrow : MonoBehaviour
{
    public Vector3 dir;
    public void Update() {
        transform.LookAt(transform.position + dir, SceneView.lastActiveSceneView.camera.transform.position - transform.position);
    }
}
