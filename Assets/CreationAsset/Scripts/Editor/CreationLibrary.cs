using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class PartData {
    public string name;
    public Texture sampleTex;
    public GameObject prefab;
}
[Serializable]
[CreateAssetMenu(menuName = "Custom/Creation Library")]
public class CreationLibrary : ScriptableObject {
    public Material Basic, NonBarrier;
    public LayerMask wallMask, partMask;
    public GameObject wallObject;
    public PartData[] parts;
    public GUIStyle[] styles;
}
