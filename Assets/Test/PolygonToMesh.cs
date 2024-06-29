using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PolygonToMesh : MonoBehaviour
{
    [ContextMenu("Convert")]
    void Convert() {
        PolygonCollider2D polygonCollider = GetComponent<PolygonCollider2D>();
        Mesh generatedMesh = polygonCollider.CreateMesh(false, true);
        DestroyImmediate(polygonCollider);
        MeshCollider collider = gameObject.AddComponent<MeshCollider>();
        collider.sharedMesh = generatedMesh;
    }
}
