using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class JoyStick : MonoBehaviour
{
    void Update() {
        transform.LookAt(transform.position+ SceneView.lastActiveSceneView.camera.transform.position - transform.position);
    }
}
