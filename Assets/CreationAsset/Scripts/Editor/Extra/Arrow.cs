using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class Arrow : MonoBehaviour
{
    void Update() {
        transform.LookAt(transform.position+transform.forward, SceneView.lastActiveSceneView.camera.transform.position-transform.position);
    }
}
