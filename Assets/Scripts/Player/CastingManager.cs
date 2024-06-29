using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

[ExecuteInEditMode]
public class CastingManager : MonoBehaviour
{
    [Range(0,2)]
    public float r;
    [Range(0,1)]
    public float d;
    [Range(0,2)]
    public float h;
    public float time;
    public Material mat;
    Transform center, top;
    
    Mesh mesh;
    List<Vector3> vertices = new List<Vector3>();
    void SetVertices(){
        vertices.Clear();
        vertices.Add(new Vector3(0,0,h));
        for(int i = 0; i < 3;i++){
            float angle = Mathf.PI * (2/3f) * i + time;
            Vector3 vertex = new Vector3(Mathf.Cos(angle),Mathf.Sin(angle),0)*d;
            vertices.Add(vertex);
        }
        mesh.SetVertices(vertices);
    }
    private void OnEnable() {
        center = transform;
        top = transform.GetChild(0);
        mesh = new Mesh();
        SetVertices();
        List<int> triangles = new List<int>();
        for(int i = 0; i < 3;i++){
            triangles.Add(0);
            triangles.Add(i+1);
            triangles.Add(i == 2 ? 1 : i+2);
        }
        mesh.SetTriangles(triangles,0);
        GetComponent<MeshFilter>().sharedMesh = mesh;
    }
    private void Update() {
        r = Mathf.Max(d+0.01f, r);
        SetVertices();
        float under = Mathf.Sqrt(r*r - d*d);
        Vector3 topPos = new Vector3(0,0,h);
        mat.SetFloat("R",r);
        mat.SetFloat("D",d);
        mat.SetFloat("H",h);
        mat.SetVector("Top", topPos);
        mat.SetVector("Under", new Vector3(0,0,-under));
        Vector3 v1 = (vertices[1]-topPos).normalized;
        Vector3 v2 = (vertices[2]-topPos).normalized;
        Vector3 v3 = (vertices[3]-topPos).normalized;
        Vector3 w1 = ((-2)*vertices[1]-topPos).normalized;
        Vector3 w2 = ((-2)*vertices[2]-topPos).normalized;
        Vector3 w3 = ((-2)*vertices[3]-topPos).normalized;
        Vector3 n1 = Vector3.Cross(w2,w3).normalized;
        Vector3 n2 = Vector3.Cross(w3,w1).normalized;
        Vector3 n3 = Vector3.Cross(w1,w2).normalized;

        float ratio = h/d;
        float grad = Mathf.Sqrt(3f / (1f + ratio*ratio));
        float grad2 = 1/Mathf.Sqrt(1+grad*grad);
        mat.SetVector("V1", v1);
        mat.SetVector("V2", v2);
        mat.SetVector("V3", v3);
        mat.SetVector("W1", w1);
        mat.SetVector("W2", w2);
        mat.SetVector("W3", w3);
        mat.SetVector("N1", n1);
        mat.SetVector("N2", n2);
        mat.SetVector("N3", n3);
        mat.SetFloat("Grad", grad);
        mat.SetFloat("Grad2", grad2);
        mat.SetMatrix("w2lMatrix", transform.worldToLocalMatrix);
    }
}
