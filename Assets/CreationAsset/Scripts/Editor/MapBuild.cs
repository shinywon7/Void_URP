using System.IO;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.SceneManagement;

namespace MapCreation {
    public class MapBuild : MonoBehaviour {
        public MapData data;
        public int[,,] edge, chk;
        public string pathName;
        string path;
        int t;
        int size;
        public CreationLibrary creationLibrary;
        ref int Edge(Vector3Int v) { return ref edge[v.x, v.y, v.z]; }
        ref int Chk(Vector3Int v) { return ref chk[v.x, v.y, v.z]; }
        PolygonCollider2D polygonCollider;
        int[] side =
        {
            1<<0,1<<1,1<<2,1<<3,1<<4,1<<5,1<<6,1<<7,1<<8,1<<9,1<<10,1<<11,1<<12
        };
        public void Build(MapData _data) {
            data = _data;
            size = data.size;
            chk = new int[size, size, size];
            t = 0;

            GameObject polygonObject = new GameObject("PolygonObject");
            polygonCollider = polygonObject.AddComponent<PolygonCollider2D>();
            edge = new int[size, size, size];
            Vector3Int pos = Vector3Int.zero;
            for (pos.x = 1; pos.x < size-1; pos.x++) for (pos.y = 1; pos.y < size-1; pos.y++) for (pos.z = 1; pos.z < size-1; pos.z++) {
                        if(data.getType(pos) != 0) CornerSetting(pos);
                    }
            for (pos.x = 1; pos.x < size - 1; pos.x++) for (pos.y = 1; pos.y < size - 1; pos.y++) for (pos.z = 1; pos.z < size - 1; pos.z++) {
                        int type = data.getType(pos);
                        if (type != 0 && Edge(pos) != 0 && (Chk(pos) & side[12]) == 0) Run(pos, type);
                    }
            DestroyImmediate(polygonObject);
        }
        void CornerSetting(Vector3Int pos) {
            int type = data.getType(pos);
            for (int i = 0; i < 6; i++) {
                if (data.getType(CreationUtils.NextPos(pos, i)) != type) Edge(pos) |= side[i];
            }
        }
        bool ChkOutOfBound(Vector3Int pos) {
            if (1 > pos.x || size - 2 < pos.x) return true;
            if (1 > pos.y || size - 2 < pos.y) return true;
            if (1 > pos.z || size - 2 < pos.z) return true;
            return false;
        }
        List<Vector3> list;
        void Run(Vector3Int initPos, int type) {
            list = new List<Vector3>();
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> UV = new List<Vector2>();
            List<int> triangles = new List<int>();
            Queue<Vector3Int> q = new Queue<Vector3Int>();
            q.Enqueue(initPos);
            Chk(initPos) |= side[12];
            while (q.Count > 0) {
                Vector3Int pos = q.Dequeue();
                //Chk(pos) |= side[12];
                list.Add(pos);
                for (int i = 0; i < 6; i++) {
                    var nextPos = CreationUtils.NextPos(pos, i);
                    if((Edge(pos) & side[i])!=0 && (Chk(pos) & side[i]) == 0) Run2(pos, i, vertices, triangles, UV, type);

                    if (type == data.getType(nextPos) && (Chk(nextPos) & side[12]) == 0) {
                        if (Edge(nextPos) != 0) { q.Enqueue(nextPos); Chk(nextPos) |= side[12]; }
                        else {
                            for (int j = 0; j < 6; j++) {
                                if (j / 2 == i / 2) continue;
                                var nextPos2 = CreationUtils.NextPos(nextPos, j);
                                if (type == data.getType(nextPos2) && (Chk(nextPos2) & side[12]) == 0) {
                                    if (Edge(nextPos2) != 0) { q.Enqueue(nextPos2); Chk(nextPos2) |= side[12]; }
                                }
                            }
                        }
                    }
                }
            }
            Mesh mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(triangles, 0);
            mesh.SetUVs(0, UV);
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            GameObject targetObject = new GameObject("Wall");
            //var meshCollider = targetObject.AddComponent<MeshCollider>();
            var meshFilter = targetObject.AddComponent<MeshFilter>();
            var meshRenderer = targetObject.AddComponent<MeshRenderer>();
            var notTraveller = targetObject.AddComponent<VoidNotTraveller>();
            notTraveller.isRigidbody = false;
            notTraveller.fixedMesh = true;
            //meshCollider.sharedMesh = mesh;
            meshFilter.sharedMesh = mesh;
            if (type == 1) {
                meshRenderer.material = creationLibrary.NonBarrier;
                notTraveller.generateBerrier = false;
            }
            else if (type == 2) {
                meshRenderer.material = creationLibrary.Basic;
                notTraveller.generateBerrier = true;
            }

            t++;
            path = Path.Combine("Assets/CreationAsset/GeneratedMeshs",pathName + t.ToString()+".mesh");
            AssetDatabase.CreateAsset(mesh,path);
            if (File.Exists("E:/unity/Void/Void_URP/Assets/CreationAsset/GeneratedMeshs") == true) {
                Debug.Log("a");
                //AssetDatabase.SaveAssets();
            }

            //SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            //SceneManager.MoveGameObjectToScene(targetObject, SceneManager.GetSceneByName(sceneName));
        }
        void Run2(Vector3Int initPos, int fixedSide, List<Vector3> totVertices, List<int> totTriangles, List<Vector2> totUV, int type) {
            polygonCollider.pathCount = 0;
            Queue<Vector3Int> q = new Queue<Vector3Int>();
            q.Enqueue(initPos);
            Chk(initPos) |= side[fixedSide];
            while (q.Count != 0) {
                Vector3Int pos = q.Dequeue();
                for(int i = 0; i < 6; i++) {
                    if (fixedSide / 2 == i / 2) continue;
                    var nextPos = CreationUtils.NextPos(pos, i);
                    if ((type != data.getType(nextPos) || (Edge(nextPos) & side[fixedSide]) == 0)) {
                        if ((Chk(pos) & side[fixedSide+6]) != 0) continue;
                        for (int j = 0; j < 6; j++) {
                            if (fixedSide / 2 == j / 2 || i / 2 == j / 2) continue;
                            Run3(pos, fixedSide, i, j, type);
                            break;
                        }
                    }
                    else {
                        if ((Chk(nextPos) & side[fixedSide]) == 0) { q.Enqueue(nextPos); Chk(nextPos) |= side[fixedSide]; }
                    }
                }
            }
            float offset = Element[fixedSide](initPos);
            Mesh generatedMesh = polygonCollider.CreateMesh(false, false);
            if (!generatedMesh) { Debug.Log("s"); return; }
            int start = totTriangles.Count;
            int sum = totVertices.Count;
            totTriangles.AddRange(generatedMesh.GetTriangles(0));
            for (int i = start; i < totTriangles.Count; i++) {
                totTriangles[i] += sum;
            }
            foreach (var v in generatedMesh.vertices) {
                totVertices.Add(revGet[fixedSide](v,offset) - new Vector3(.5f,.5f,.5f));
                totUV.Add(v);
            }
        }
        void Run3(Vector3Int pos, int fixedSide,int beside, int frontSide, int type) {
            Vector3Int initPos = pos;
            int initBeside = beside, initFrontSide = frontSide;
            List<Vector3> vertices = new List<Vector3>();
            List<Vector2> UV = new List<Vector2>();
            do {
                Vector3Int nextPos = CreationUtils.NextPos(pos, frontSide);
                Vector3Int nextNearPos = CreationUtils.NextPos(nextPos, beside);
                Chk(pos) |= side[fixedSide+6];
                if ((Edge(nextPos) & side[fixedSide]) == 0 || type != data.getType(nextPos)) {
                    vertices.Add(pos +singleOffset[fixedSide]+singleOffset[beside]+singleOffset[frontSide]);
                    int tempSide = beside;
                    beside = frontSide;
                    frontSide = CreationUtils.OppositeDir(tempSide);
                }
                else if((Edge(nextNearPos) & side[fixedSide]) != 0 && type == data.getType(nextNearPos)) {
                    vertices.Add(pos + singleOffset[fixedSide] + singleOffset[beside] + singleOffset[frontSide]);
                    pos = nextNearPos;
                    int tempSide = frontSide;
                    frontSide = beside;
                    beside = CreationUtils.OppositeDir(tempSide);
                }
                else {
                    pos = nextPos;
                }
            } while (!(pos == initPos && beside == initBeside && frontSide == initFrontSide)) ;
            foreach (var v in vertices) {
                //Debug.Log(v);
                Vector2 v2 = get[fixedSide](v);
                UV.Add(v2);
            }
            //Debug.Log(fixedSide);

            polygonCollider.pathCount += 1;
            polygonCollider.SetPath(polygonCollider.pathCount-1, UV);
        }
        delegate Vector2 del(Vector3 v);
        del[] get = { (v) => new Vector2(v.z, v.y), (v) => new Vector2(-v.z, v.y), (v) => new Vector2(v.x, v.z), (v) => new Vector2(-v.x, v.z), (v) => new Vector2(-v.x, v.y), (v) => new Vector2(v.x, v.y) };
        delegate Vector3 Del(Vector3 v, float offset);
        Del[] revGet = { (v, o) => new Vector3(o,v.y,v.x), (v, o) => new Vector3(o, v.y, -v.x), (v, o) => new Vector3(v.x, o, v.y), (v, o) => new Vector3(-v.x, o, v.y), (v, o) => new Vector3(-v.x, v.y, o), (v, o) => new Vector3(v.x, v.y, o)};

        public static Vector3[] singleOffset = { new Vector3(1, 0, 0), new Vector3(0, 0, 0), new Vector3(0, 1, 0), new Vector3(0, 0, 0), new Vector3(0, 0, 1), new Vector3(0, 0, 0) };
        public delegate float element(Vector3Int v);
        public static element[] Element = { (v) => v.x +1, (v) => v.x, (v) => v.y+1, (v) => v.y, (v) => v.z+1, (v) => v.z };
        [ContextMenu("Gizmos")]
        void setGizmos() {
            seeGizmos = !seeGizmos;
        }
        bool seeGizmos;
        void OnDrawGizmos() {
            if (!seeGizmos) return;
            foreach(var i in list) {
                Gizmos.DrawCube(i, new Vector3(0.2f, 0.2f, 0.2f));
            }
        }
    }
    
}
