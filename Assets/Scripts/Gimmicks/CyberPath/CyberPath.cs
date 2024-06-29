using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;

using Unity.Collections;

namespace CyberPath{
    public class CyberPath : MonoBehaviour
    {

        public float meshResolution;
        public LayerMask frontMask, backMask;
        int meshSize;
        NativeArray<RaycastCommand> frontCommands, backCommands;
        NativeArray<RaycastHit> frontHitinfo, backHitinfo;
        NativeArray<Vector3> vertices;
        NativeArray<Vector2> uvs;
        NativeArray<float> dists;
        NativeArray<bool> frontSide;
        NativeArray<Vector3> from;
        List<int> triangles;
        List<Vector3> normals;

        public float edgeDistThreshold;
        public float width;
        Vector3 start, end;


        Transform front, back;
        MeshFilter frontFilter, backFilter;
        MeshCollider frontUpperCol, frontLowerCol, backUpperCol, backLowerCol;
        Mesh mesh;
        
        // Start is called before the first frame update
        void Start()
        {
            front = transform.Find("Front");
            GameObject oriObject = front.gameObject;
            back = Instantiate(oriObject, transform).transform;
            back.name = "Back";
            back.gameObject.layer = LayerMask.NameToLayer("TravellerBack");
            foreach (Transform child in back)
                    child.gameObject.layer = LayerMask.NameToLayer("TravellerBack");
            meshSize = (int)(width/meshResolution);
            meshResolution = width/meshSize;
            frontFilter = front.GetComponent<MeshFilter>();
            backFilter = back.GetComponent<MeshFilter>();

            
            vertices = new NativeArray<Vector3>(meshSize * 4, Allocator.Persistent);
            uvs = new NativeArray<Vector2>(meshSize*4,Allocator.Persistent);
            dists = new NativeArray<float>(meshSize+1, Allocator.Persistent);
            from = new NativeArray<Vector3>(meshSize+1, Allocator.Persistent);
            frontSide = new NativeArray<bool>(meshSize+1, Allocator.Persistent);
            frontCommands = new NativeArray<RaycastCommand>(meshSize+1, Allocator.Persistent);
            backCommands = new NativeArray<RaycastCommand>(meshSize+1, Allocator.Persistent);
            frontHitinfo = new NativeArray<RaycastHit>(meshSize+1, Allocator.Persistent);
            backHitinfo = new NativeArray<RaycastHit>(meshSize+1, Allocator.Persistent);

            normals = new List<Vector3>();
            triangles = new List<int>();
            for(int i = 0; i < meshSize;i++){
                int vert = i*4;
                for(int j = 0; j < 4;j++) normals.Add(Vector3.up);
                triangles.Add(vert);
                triangles.Add(vert+1);
                triangles.Add(vert+2);
                triangles.Add(vert+2);
                triangles.Add(vert+1);
                triangles.Add(vert+3);
            }
            for(int i = 0; i <= meshSize;i++){
                QueryParameters queryParameters = new QueryParameters(layerMask: frontMask);
                frontCommands[i] = new RaycastCommand(transform.position + transform.right*i*meshResolution, transform.forward, queryParameters);
                from[i] = transform.position + transform.right*i*meshResolution;
            }
            start = from[0];
            end = from[meshSize];

            mesh = new Mesh();

            frontUpperCol = front.Find("Upper").GetComponent<MeshCollider>();
            frontLowerCol = front.Find("Lower").GetComponent<MeshCollider>();
            backUpperCol = back.Find("Upper").GetComponent<MeshCollider>();
            backLowerCol = back.Find("Lower").GetComponent<MeshCollider>();
            frontFilter.sharedMesh = mesh;
            backFilter.sharedMesh = mesh;
            
            VoidManager.voidSet += VoidSet;
            VoidManager.voidFade += VoidFade;
            FixedUpdate();
        }
        void VoidSet(){
            var calcSideJob = new CalcSideJob{
                frontCommands = frontCommands,
                frontSide =  frontSide,
                from = from,
                forward = transform.forward,
                frontMask = frontMask,
                backMask = backMask,
                start = VoidManager.voidPlane.GetDistanceToPoint(start),
                end = VoidManager.voidPlane.GetDistanceToPoint(end),
                size = meshSize
            };
            JobHandle handle = calcSideJob.Schedule(meshSize+1,16);
            handle.Complete();
        }
        public void VoidFade()
        {
            var resetSideJob = new ResetSideJob{
                frontCommands = frontCommands,
                frontSide = frontSide,
                from = from,
                frontMask = frontMask,
                forward = transform.forward
            };
        }
        void LateUpdate(){
            back.position = transform.position + VoidManager.initVoidNormal * VoidManager.voidWidth;
        }
        void FixedUpdate(){
            var beforeRaycastJob = new BeforeRaycastJob{
                backCommands = backCommands,
                from = from,
                frontSide = frontSide,
                forward = transform.forward,
                shift = VoidManager.initVoidNormal * VoidManager.voidWidth,
                frontMask = frontMask,
                backMask = backMask
            };
            JobHandle handle = beforeRaycastJob.Schedule(meshSize+1,16);

            handle = RaycastCommand.ScheduleBatch(frontCommands, frontHitinfo, 1, handle);
            handle = RaycastCommand.ScheduleBatch(backCommands, backHitinfo, 1, handle);
            
            var calcDistJob = new CalcDistJob{
                dists = dists,
                frontHitinfo = frontHitinfo,
                backHitinfo = backHitinfo,
            };
            handle = calcDistJob.Schedule(meshSize+1,16,handle);
            handle.Complete();
        }
        void Update(){
            var afterRaycastJob = new AfterRaycastJob{
                vertices = vertices,
                uvs =uvs,
                frontSide = frontSide,
                dists = dists,
                shift = transform.InverseTransformVector(VoidManager.initVoidNormal * VoidManager.voidWidth),
                size = meshSize,
                resolution = meshResolution,
                edgeDistThreshold = edgeDistThreshold,
            };
            var handle = afterRaycastJob.Schedule(meshSize,16);
            handle.Complete();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0,uvs);
            mesh.RecalculateBounds();
            frontUpperCol.sharedMesh = mesh;
            frontLowerCol.sharedMesh = mesh;
            backUpperCol.sharedMesh = mesh;
            backLowerCol.sharedMesh = mesh;
        }
        void OnDestroy() {
            vertices.Dispose();
            uvs.Dispose();
            dists.Dispose();
            from.Dispose();
            frontSide.Dispose();
            frontCommands.Dispose();
            backCommands.Dispose();
            frontHitinfo.Dispose();
            backHitinfo.Dispose();
            VoidManager.voidSet -= VoidSet;
            VoidManager.voidFade -= VoidFade;
        }
    }
}
