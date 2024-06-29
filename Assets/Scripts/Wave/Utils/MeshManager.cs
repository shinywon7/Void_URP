using UnityEngine;

using Unity.Mathematics;
using System.Collections.Generic;

namespace Wave{
    public class MeshManager
    {
        Mesh frontMesh, backMesh;

        MeshFilter frontMeshFilter, backMeshFilter;

        public List<Vector3> vertices, normals;
        public MeshManager(MeshFilter frontMeshFilter,MeshFilter backMeshFilter, int resolution, float size){
            this.frontMeshFilter = frontMeshFilter;
            this.backMeshFilter = backMeshFilter;
            UpdateMesh(resolution, size);
        }
        
        public void UpdateMesh(int resolution, float size){
            Dispose();
            frontMesh = new Mesh();
            backMesh = new Mesh();
            frontMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            backMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            float cellSize = size / (resolution-1);

            int sqrResolution = resolution*resolution;
            int nVertices = sqrResolution+4;

            vertices = new List<Vector3>();
            normals =  new List<Vector3>();

            //List<int> triangles = new List<int>((resolution-1) * (resolution-1) * 2 * 3+24);
            List<int> triangles = new List<int>();
            
            Vector3 d1 = new Vector3(1,0,0);
            Vector3 d2 = new Vector3(-0.5f, 0, math.sqrt(3)/2);
            Vector3 initPoint = -((d1+d2) * (resolution-1)) / 2;
            int idx = 0;
            for(int i = 0; i < resolution; i++){
                for(int j = 0; j < resolution; j++){
                    vertices.Add((initPoint + d1*i + d2*j)*cellSize);
                    normals.Add(Vector3.up);
                    //uvs[idx] = new Vector2(vertices[idx].x, vertices[idx].z);
                    if(0 < i && 0 < j){
                        triangles.Add(idx-resolution-1);
                        triangles.Add(idx);
                        triangles.Add(idx-1);
                        
                        triangles.Add(idx-resolution-1);
                        triangles.Add(idx-resolution);
                        triangles.Add(idx);
                    }
                    idx++;
                }
            }
            vertices.Add(new Vector3(-1000,0,-1000)); 
            vertices.Add(new Vector3(-1000,0,1000));
            vertices.Add(new Vector3(1000,0,1000));
            vertices.Add(new Vector3(1000,0,-1000));
            int[] edges = new int[]{0,resolution-1, sqrResolution-1,sqrResolution-resolution};
            for(int i = 0;i < 4;i++){
                normals.Add(Vector3.up);
                int j = (i+1)%4;
                triangles.Add(sqrResolution+i);
                triangles.Add(sqrResolution+j);
                triangles.Add(edges[i]);

                triangles.Add(edges[i]);
                triangles.Add(sqrResolution+j);
                triangles.Add(edges[j]);
            }

            SetMesh(backMeshFilter, backMesh, vertices, normals, triangles);

            vertices.Add(new Vector3(-1000,-1000,-1000)); 
            vertices.Add(new Vector3(-1000,-1000,1000));
            vertices.Add(new Vector3(1000,-1000,1000));
            vertices.Add(new Vector3(1000,-1000,-1000));
            for(int i = 0;i < 4;i++){
                normals.Add(Vector3.up);
                int j = (i+1)%4;
                triangles.Add(sqrResolution+i+4);
                triangles.Add(sqrResolution+j+4);
                triangles.Add(sqrResolution+i);

                triangles.Add(sqrResolution+i);
                triangles.Add(sqrResolution+j+4);
                triangles.Add(sqrResolution+j);
            }

            triangles.Add(sqrResolution+4);
            triangles.Add(sqrResolution+6);
            triangles.Add(sqrResolution+5);
            triangles.Add(sqrResolution+6);
            triangles.Add(sqrResolution+4);
            triangles.Add(sqrResolution+7);

            SetMesh(frontMeshFilter, frontMesh, vertices, normals, triangles);
        }
        public void SetMesh(MeshFilter meshFilter, Mesh mesh, List<Vector3> vertices,List<Vector3> normals,List<int> triangles){
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(triangles, 0);
            meshFilter.sharedMesh = mesh;
        }
        public void Dispose(){
            if(frontMeshFilter != null)
                frontMeshFilter.sharedMesh = null;
            if(frontMesh != null)
                UnityEngine.Object.DestroyImmediate(frontMesh);
            if(backMeshFilter != null)
                backMeshFilter.sharedMesh = null;
            if(backMesh != null)
                UnityEngine.Object.DestroyImmediate(backMesh);
        }


    }
}

