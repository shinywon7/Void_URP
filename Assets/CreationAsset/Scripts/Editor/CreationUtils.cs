using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MapCreation {
    public class CreationUtils {
        public static Vector3Int[] dv = { new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0), new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0), new Vector3Int(0, 0, 1), new Vector3Int(0, 0, -1) };
        public static Vector3[] dr = { new Vector3(0, -90, 0), new Vector3(0, 90, 0), new Vector3(90, 0, 0), new Vector3(-90, 0, 0), new Vector3(0, 180, 0), new Vector3(0, 0, 0) };
        public static int OppositeDir(int dir) {
            return dir % 2 == 0 ? dir + 1 : dir - 1;
        }
        public static string GenerateName(Vector3Int v, int dir) {
            return string.Format("Wall({0},{1},{2},{3})", v.x, v.y, v.z, dir);
        }
        public static Vector3Int NextPos(Vector3Int v, int dir) {
            return v + CreationUtils.dv[dir];
        }
        public delegate int element(Vector3Int v);
        public static element[] Element = {(v)=>v.x, (v) => -v.x, (v) => v.y, (v) => -v.y, (v) => v.z, (v) => -v.z };
    }
}
