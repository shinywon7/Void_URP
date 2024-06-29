using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Burst;
using Unity.Jobs;

using Unity.Collections;
using UnityEngine.Rendering.Universal;
using System;

namespace CyberPath{
    [BurstCompile]
    public struct ResetSideJob : IJobParallelFor{

        [WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<RaycastCommand> frontCommands;
        [WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<bool> frontSide;
        [ReadOnly] public NativeArray<Vector3> from;
        [ReadOnly] public LayerMask frontMask;
        [ReadOnly] public Vector3 forward;
        public void Execute(int index){
            frontSide[index] = true;
            QueryParameters queryParameters = new QueryParameters(layerMask: frontMask);
            frontCommands[index] = new RaycastCommand(from[index], forward, queryParameters);
        }
    }
    [BurstCompile]
    public struct CalcSideJob : IJobParallelFor{

        [WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<RaycastCommand> frontCommands;
        [WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<bool> frontSide;
        [ReadOnly] public NativeArray<Vector3> from;
        [ReadOnly] public Vector3 forward;
        [ReadOnly] public LayerMask frontMask, backMask;
        [ReadOnly] public float start, end;
        [ReadOnly] public int size;
        public void Execute(int index){
            QueryParameters queryParameters;
            if(Mathf.Lerp(start, end, (float)index/size) > 0){
                queryParameters = new QueryParameters(layerMask: frontMask);
                frontSide[index] = true;
            }
            else{
                queryParameters = new QueryParameters(layerMask: backMask);
                frontSide[index] = false;
            }
            frontCommands[index] = new RaycastCommand(from[index], forward, queryParameters);
        }
    }
    [BurstCompile]
    public struct CalcDistJob : IJobParallelFor
    {
        [WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<float> dists;
        [ReadOnly] public NativeArray<RaycastHit> frontHitinfo;
        [ReadOnly] public NativeArray<RaycastHit> backHitinfo;
        public void Execute(int index){
            float dist = 1000f;
            if(frontHitinfo[index].distance != 0) dist = frontHitinfo[index].distance;
            else if(backHitinfo[index].distance != 0) dist = backHitinfo[index].distance;
            dists[index] = dist;
        }
    }
    [BurstCompile]
    public struct AfterRaycastJob : IJobParallelFor
    {
        [WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<Vector3> vertices;
        [WriteOnly] [NativeDisableParallelForRestriction] public NativeArray<Vector2> uvs;
        [ReadOnly] public NativeArray<bool> frontSide;
        [ReadOnly] public NativeArray<float> dists;
        [ReadOnly] public Vector3 shift;
        [ReadOnly] public int size;
        [ReadOnly] public float resolution;
        [ReadOnly] public float edgeDistThreshold;
        float getDist(int index, bool side){
            if(index < 0 || index > size || frontSide[index] != side) return 0f;
            return dists[index];
        }
        public void Execute(int index){
            int vertexIndex = index*4;
            if(frontSide[index] != frontSide[index+1]){
                vertices[vertexIndex] = new Vector3(index*resolution,0, 0);
                vertices[vertexIndex+1] = new Vector3(index*resolution,0, 0.01f);
                vertices[vertexIndex+2] = new Vector3((index)*resolution+0.01f,0, 0);
                vertices[vertexIndex+3] = new Vector3((index)*resolution+0.01f,0, 0.01f);
                return;
            }
            bool side= frontSide[index];
            float llDist = getDist(index-1, side);
            float lDist = dists[index];
            float rDist = dists[index+1];
            float rrDist = getDist(index+2, side);
            if(lDist > rDist + edgeDistThreshold) lDist = rDist;
            else if(rDist > lDist + edgeDistThreshold) rDist = lDist;

            if(frontSide[index]){
                vertices[vertexIndex] = new Vector3(index*resolution,0, 0);
                vertices[vertexIndex+1] = new Vector3(index*resolution,0, lDist);
                vertices[vertexIndex+2] = new Vector3((index+1)*resolution,0, 0);
                vertices[vertexIndex+3] = new Vector3((index+1)*resolution,0, rDist);
            }
            else{
                vertices[vertexIndex] = new Vector3(index*resolution,0, 0) - shift;
                vertices[vertexIndex+1] = new Vector3(index*resolution,0, lDist) - shift;
                vertices[vertexIndex+2] = new Vector3((index+1)*resolution,0, 0) - shift;
                vertices[vertexIndex+3] = new Vector3((index+1)*resolution,0, rDist) - shift;
            }
            
            if(lDist > llDist+edgeDistThreshold){
                uvs[vertexIndex] = new Vector2(1,-llDist);
                uvs[vertexIndex+1] = new Vector2(1,lDist-llDist);
                uvs[vertexIndex+2] = new Vector2(0,-llDist);
                uvs[vertexIndex+3] = new Vector2(0,rDist-llDist);
            }
            else if(rDist > rrDist+edgeDistThreshold){
                uvs[vertexIndex] = new Vector2(0,-rrDist);
                uvs[vertexIndex+1] = new Vector2(0,lDist-rrDist);
                uvs[vertexIndex+2] = new Vector2(1,-rrDist);
                uvs[vertexIndex+3] = new Vector2(1,rDist-rrDist);
            }
            else{
                uvs[vertexIndex] = new Vector2(0,0);
                uvs[vertexIndex+1] = new Vector2(0,1);
                uvs[vertexIndex+2] = new Vector2(0,0);
                uvs[vertexIndex+3] = new Vector2(0,1);
            }
        }
    }
    [BurstCompile]
    public struct BeforeRaycastJob : IJobParallelFor
    {
        [NativeDisableParallelForRestriction] public NativeArray<RaycastCommand> backCommands;
        [ReadOnly] public NativeArray<Vector3> from;
        [ReadOnly] public NativeArray<bool> frontSide;
        [ReadOnly] public Vector3 forward;
        [ReadOnly] public Vector3 shift;
        [ReadOnly] public LayerMask frontMask, backMask;
        public void Execute(int index){
            if(frontSide[index]){
                QueryParameters queryParameters = new QueryParameters(layerMask: backMask);
                backCommands[index] = new RaycastCommand(from[index]+shift, forward, queryParameters);
            }
            else{
                QueryParameters queryParameters = new QueryParameters(layerMask: frontMask);
                backCommands[index] = new RaycastCommand(from[index]-shift, forward, queryParameters);
            }
        }
    }
}
