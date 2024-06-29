using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

public class VoidNotTraveller : MonoBehaviour
{
    public bool isRigidbody;
    public bool fixedMesh;
    public bool generateBerrier;
    GameObject Section;
    public Mesh colliderMesh;
    Mesh mesh;
    ObjectPooler barrierColliderPooler;
    ObjectPooler barrierModelPooler;
    public ComputeShader slice;

    Vector3Int[] nearTriangles;

    PolygonCollider2D Sample2DCollider;
    MeshCollider sectionCollider;

    public GameObject ColliderObject;
    GameObject sectionObject;
    GameObject upperObject, lowerObject;
    Transform initTransform;
    MeshCollider upperCollider, lowerCollider;

    void Start()
    {
        if(!colliderMesh) colliderMesh = transform.GetComponent<MeshFilter>().mesh;
        Sample2DCollider = GameObject.Find("Sample2DCollider").GetComponent<PolygonCollider2D>();
        Section = GameObject.Find("Section");
        ResetCollider();

        sectionObject = Instantiate(ColliderObject);
        sectionObject.transform.parent = Section.transform;
        sectionObject.transform.localPosition = Vector3.zero;
        sectionObject.transform.rotation = Quaternion.identity;
        sectionObject.layer = LayerMask.NameToLayer("Section");
        sectionCollider = sectionObject.GetComponent<MeshCollider>();

        upperObject = Instantiate(ColliderObject);
        upperObject.transform.localScale = Vector3.one;
        upperObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
        upperObject.transform.parent = transform;
        upperObject.layer = LayerMask.NameToLayer("Default");
        upperCollider = upperObject.GetComponent<MeshCollider>();
        upperObject.tag = transform.tag;
        upperCollider.sharedMesh = mesh;

        initTransform = upperObject.transform;

        lowerObject = Instantiate(ColliderObject);
        lowerObject.transform.localScale = Vector3.one;
        lowerObject.transform.SetPositionAndRotation(transform.position, transform.rotation);
        lowerObject.transform.parent = transform;
        lowerObject.layer = LayerMask.NameToLayer("DefaultBack");
        lowerCollider = lowerObject.GetComponent<MeshCollider>();
        lowerCollider.enabled = false;

        barrierColliderPooler = GameObject.Find("BarrierColliders").GetComponent<ObjectPooler>();
        barrierModelPooler = GameObject.Find("BarrierModels").GetComponent<ObjectPooler>();

        if (fixedMesh) VoidManager.voidSet += Slice;
        VoidManager.voidFade += VoidFade;
    }

    void Update()
    {
        if (VoidManager.isVoidGenerated)
        {
            if (!fixedMesh)
            {
                Slice();
            }
            // if (isRigidbody) {
            //     RigidbodySync();
            // }
            // else TransformSync();
        }
    }
    // public void RigidbodySync(Rigidbody rb, float side)
    // {
    //     rb.position = transform.position + VoidManager.voidTransform.up * VoidManager.voidWidth * side;
    //     rb.rotation = transform.rotation;
    //     rb.velocity = myrb.velocity;
    //     rb.angularVelocity = myrb.angularVelocity;
    // }
    // public void RigidbodySync()
    // {
    //     RigidbodySync(upperRigidbodyFake, -sameSide);
    //     RigidbodySync(lowerRigidbodyFake, sameSide);
    // }
    // public void TransformSync(GameObject gameObject, float side)
    // {
    //     gameObject.transform.position = transform.position + VoidManager.voidTransform.up * VoidManager.voidWidth * side;
    // }
    // public void TransformSync()
    // {
    //     TransformSync(upperObjectFake, sameSide);
    //     TransformSync(lowerObjectFake, -sameSide);
    // }
    void VoidFade()
    {
        upperCollider.enabled = true;
        lowerCollider.enabled = false;
        upperCollider.sharedMesh = mesh;
    }
    
    void Slice()
    {
        Matrix4x4 localToPlaneMatrix = VoidManager.voidTransform.worldToLocalMatrix * initTransform.localToWorldMatrix;
        slice.SetMatrix("localToPlaneMatrix", localToPlaneMatrix);
        Vector3 normal = initTransform.InverseTransformVector(VoidManager.voidTransform.up);
        Vector3 position = initTransform.InverseTransformPoint(VoidManager.voidTransform.position);

        Vector4 plane = new Vector4(normal.x, normal.y, normal.z, -(normal.x * position.x + normal.y * position.y + normal.z * position.z));
        slice.SetVector("plane", plane);
        slice.SetVector("position", initTransform.InverseTransformVector(transform.position - VoidManager.voidTransform.position));



        int trianglePointCount = mesh.triangles.Length;
        int triangleCount = trianglePointCount / 3;
        ComputeBuffer _vertices = new ComputeBuffer(mesh.vertexCount + triangleCount, 12);
        _vertices.SetData(new Vector3[mesh.vertexCount + triangleCount]);
        _vertices.SetData(mesh.vertices);
        slice.SetBuffer(0, "vertices", _vertices);

        ComputeBuffer _triangles = new ComputeBuffer(trianglePointCount, sizeof(int));
        _triangles.SetData(mesh.triangles);
        slice.SetBuffer(0, "triangles", _triangles);

        ComputeBuffer _upperTriangles = new ComputeBuffer(trianglePointCount * 2, sizeof(int));
        ComputeBuffer _lowerTriangles = new ComputeBuffer(trianglePointCount * 2, sizeof(int));
        ComputeBuffer _nextTriangles = new ComputeBuffer(trianglePointCount, sizeof(int));
        ComputeBuffer _planeVertices = new ComputeBuffer(trianglePointCount, 8);
        ComputeBuffer _nearTriangles = new ComputeBuffer(trianglePointCount, 12);

        int[] emptyTriangles = new int[trianglePointCount * 2];
        Array.Fill(emptyTriangles, -1);
        _upperTriangles.SetData(emptyTriangles);
        _lowerTriangles.SetData(emptyTriangles);
        _nextTriangles.SetData(new int[triangleCount]);
        _planeVertices.SetData(new Vector2[triangleCount]);
        _nearTriangles.SetData(nearTriangles);
        slice.SetBuffer(0, "upperTriangles", _upperTriangles);
        slice.SetBuffer(0, "lowerTriangles", _lowerTriangles);
        slice.SetBuffer(0, "nextTriangles", _nextTriangles);
        slice.SetBuffer(0, "planeVertices", _planeVertices);
        slice.SetBuffer(0, "nearTriangles", _nearTriangles);

        slice.SetInt("count", mesh.vertexCount);
        slice.SetInt("triangleCount", trianglePointCount);
        slice.Dispatch(0, Mathf.CeilToInt(trianglePointCount / 48.0f), 1, 1);


        Vector3[] vertices = new Vector3[mesh.vertexCount + triangleCount];
        _vertices.GetData(vertices);
        int[] upperTriangles = new int[trianglePointCount * 2];
        _upperTriangles.GetData(upperTriangles);
        int[] lowerTriangles = new int[trianglePointCount * 2];
        _lowerTriangles.GetData(lowerTriangles);
        int[] nextTriangles = new int[triangleCount];
        _nextTriangles.GetData(nextTriangles);
        Vector2[] planeVertices = new Vector2[triangleCount];
        _planeVertices.GetData(planeVertices);

        _nearTriangles.Dispose();
        _vertices.Dispose();
        _upperTriangles.Dispose();
        _lowerTriangles.Dispose();
        _triangles.Dispose();
        _nextTriangles.Dispose();
        _planeVertices.Dispose();

        List<int> upper = new List<int> { }, lower = new List<int> { };
        for (int i = 0; i < trianglePointCount * 2; i += 1)
        {
            if (upperTriangles[i] != -1)
            {
                upper.Add(upperTriangles[i]);
            }
            if (lowerTriangles[i] != -1)
            {
                lower.Add(lowerTriangles[i]);
            }
        }

        if(upper.Count == 0)
        {
            upperCollider.enabled = false;
            SetMesh(lowerCollider, vertices, lower);
            return;
        }
        if(lower.Count == 0)
        {
            lowerCollider.enabled = false;
            SetMesh(upperCollider, vertices, upper);
            return;
        }

        SetMesh(upperCollider, vertices, upper);
        SetMesh(lowerCollider, vertices, lower);

        bool[] chk = new bool[triangleCount];
        Sample2DCollider.pathCount = 0;
        for (int i = 0; i < triangleCount; i++)
        {
            if (nextTriangles[i] != 0 && !chk[i])
            {

                Stack<Vector2> PointStack = new Stack<Vector2> { };
                PointStack.Push(planeVertices[i]);
                int j = nextTriangles[i];
                chk[j] = true;
                PointStack.Push(planeVertices[j]);
                for (j = nextTriangles[j]; j != i; j = nextTriangles[j])
                {
                    chk[j] = true;
                    while (PointStack.Count >=2)
                    {
                        Vector2 p2 = PointStack.Pop();
                        Vector2 p1 = PointStack.Peek();
                        if (MathF.Abs(Ccw(p1, p2, planeVertices[j])) > 0.2f)
                        {
                            PointStack.Push(p2);
                            break;
                        }
                    }
                    PointStack.Push(planeVertices[j]);
                }
                while (PointStack.Count >= 2)
                {
                    Vector2 p2 = PointStack.Pop();
                    Vector2 p1 = PointStack.Peek();
                    if (MathF.Abs(Ccw(p1, p2, planeVertices[i])) > 0.2f)
                    {
                        PointStack.Push(p2);
                        break;
                    }
                }
                List<Vector2> PointArray = PointStack.ToList();
                List<Vector2> DistArray = new List<Vector2>();
                int n = PointArray.Count;
                if(MathF.Abs(Ccw(PointArray[n-2], PointArray[n-1], PointArray[0])) < 0.2f){
                    n-=1;
                }
                if (generateBerrier) {
                    Vector2 bef = PointArray[0] - PointArray[n-1];
                    Vector2 temp = bef;
                    for(int k = 0; k < n-1;k++){
                        Vector2 now =PointArray[k+1]-PointArray[k];
                        PointArray[k] += GetMargin(bef.normalized, now.normalized);
                        bef = now;
                    }
                    PointArray[n-1] += GetMargin(bef.normalized, temp.normalized);

                    bef = PointArray[n-1];
                    for (int k = 0; k < n; k++) {
                        Vector2 now = PointArray[k];
                        GameObject barrierCollider = barrierColliderPooler.SpawnFromPool(planeVertices[j]);
                        GameObject barrierModel = barrierModelPooler.SpawnFromPool(planeVertices[j]);
                        barrierCollider.GetComponent<BarrierCollider>().Setup(now,bef);
                        barrierModel.GetComponent<BarrierModel>().Setup(now,bef);
                        bef = now;
                    }
                }
                else {
                    Sample2DCollider.pathCount += 1;
                    Sample2DCollider.SetPath(Sample2DCollider.pathCount - 1, PointArray);
                }
            }
        }

        if (!generateBerrier) {
            Mesh generatedMesh = Sample2DCollider.CreateMesh(false, false);
            generatedMesh.SetIndices(generatedMesh.GetIndices(0).Concat(generatedMesh.GetIndices(0).Reverse()).ToArray(), MeshTopology.Triangles, 0);
            sectionCollider.sharedMesh = generatedMesh;
        }
    }
    
    Vector2 GetMargin(Vector2 d1, Vector2 d2){
        Vector2 n = (d1-d2).normalized;
        float cross = Cross(d1.normalized, n);
        return -(1/cross)*n*0.08f;
    }
    float Cross(Vector2 u, Vector2 v){
        return u.x*v.y - u.y*v.x;
    }
    float Ccw(Vector2 A, Vector2 B, Vector2 C)
    {
        Vector2 BO = B - A;
        Vector2 CO = C - A;
        return (BO.x * CO.y - BO.y * CO.x);
    }
    void SetMesh(MeshCollider target, Vector3[] vertices, List<int> triangles)
    {
        target.enabled = true;
        Mesh mesh = new Mesh();
        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        target.sharedMesh = mesh;
    }

    void ResetCollider()
    {
        if (mesh != null) Destroy(mesh);
        mesh = new Mesh();

        List<Vector3> newVertices = new List<Vector3> { };
        int[] begin = new int[colliderMesh.vertexCount];
        int triangleCount = colliderMesh.triangles.Count();

        for (int i = 0; i < colliderMesh.vertexCount; i++)
        {
            bool flag = false;
            begin[i] = newVertices.Count;
            for (int j = 0; j < i; j++)
            {
                if (Vector3.Distance(colliderMesh.vertices[i], colliderMesh.vertices[j]) < 0.01f)
                {
                    begin[i] = begin[j];
                    flag = true;
                    break;
                }
            }
            if (!flag)
            {
                newVertices.Add(Vector3.Scale(colliderMesh.vertices[i], transform.lossyScale));
            }
        }
        int[] newTriangles = new int[triangleCount];
        for (int i = 0; i < triangleCount; i++)
        {
            newTriangles[i] = begin[colliderMesh.triangles[i]];
        }
        mesh.SetVertices(newVertices);
        mesh.SetTriangles(newTriangles, 0);
        nearTriangles = new Vector3Int[triangleCount];
        for (int i = 0; i < triangleCount; i += 3)
        {
            int a = newTriangles[i], b = newTriangles[i + 1], c = newTriangles[i + 2];
            for (int j = 0; j < triangleCount; j += 3)
            {
                if (i == j) continue;
                int na = newTriangles[j], nb = newTriangles[j + 1], nc = newTriangles[j + 2];
                bool fa = a == na || a == nb || a == nc;
                bool fb = b == na || b == nb || b == nc;
                bool fc = c == na || c == nb || c == nc;
                if (fa && fb) nearTriangles[i / 3].z = j / 3;
                else if (fa && fc) nearTriangles[i / 3].y = j / 3;
                else if (fb && fc) nearTriangles[i / 3].x = j / 3;
            }
        }
    }
}
