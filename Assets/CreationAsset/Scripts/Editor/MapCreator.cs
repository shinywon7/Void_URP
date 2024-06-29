using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MapCreation {
    public class MapCreator : MonoBehaviour {

        public static MapData editorData;
        public static GameObject wallObject;
        public GameObject sideSelectionObject, spaceSelectionObject;
        public CreationLibrary creationLibrary;
        bool initialized;
        public GUIStyle[] styles;
        public struct Event {
            public Vector3Int v;
            public int type;
            public Event(Vector3Int _v, int _type) {
                v = _v;
                type = _type;
            }
        }
        [ContextMenu("SetGlobalValues")]
        void SetGlobalValues() {
            Shader.SetGlobalVector("_InsidePosition", new Vector3(-100,0,0));
            Shader.SetGlobalVector("_VoidNormal", new Vector3(1,0,0));
        }
        [ContextMenu("Build")]
        void Build() {
            MapBuild mapBuild = GetComponent<MapBuild>();
            if (!mapBuild) return;
            mapBuild.Build(editorData);
        }
        Queue<Event> Events;
        public void InitializeMapData(int size) {
            if (!initialized) {
                initialized = true;
                editorData = new MapData();
                editorData.Initialize(size);
                wallObject = creationLibrary.wallObject;
                int m = size / 2;
                int s = m - 2, e = m + 2;
                Events = new Queue<Event>();
                foreach (var obj in GameObject.FindGameObjectsWithTag("Wall_Creation")) {
                    DestroyImmediate(obj);
                }
                for (int x = s; x < e; x++)
                    for (int y = s; y < e; y++)
                        for (int z = s; z < e; z++) {
                            SetGrid(new Vector3Int(x, y, z), 0);
                            Events.Enqueue(new Event(new Vector3Int(x, y, z), 0));
                        }
            }
        }
        [ContextMenu("reset")]
        public void SetReset() {
            initialized = false;
            InitializeMapData(50);
            
        }
        public static void SetGrid(Vector3Int pos, int nextType) {
            int prevType = editorData.getType(pos);
            if (prevType == nextType) return;
            for (int i = 0; i < 6; i++) {
                SetWall(pos, i, prevType, nextType);
            }
            editorData.setType(pos, nextType);
        }
        public static void SetWall(Vector3Int pos, int dir, int prevType, int nextType, GameObject part = null) {
            Vector3Int nearPos = CreationUtils.NextPos(pos, dir);
            int nearType = editorData.getType(nearPos);
            if (prevType < nearType && nextType >= nearType) {
                Undo.DestroyObjectImmediate(GameObject.Find(CreationUtils.GenerateName(nearPos, CreationUtils.OppositeDir(dir))));
            }
            else if (nextType < nearType) {
                if (prevType >= nearType) {
                    GameObject currentWallObject = Instantiate(wallObject);
                    Undo.RegisterCreatedObjectUndo(currentWallObject, "CreateWall");
                    currentWallObject.name = CreationUtils.GenerateName(nearPos, CreationUtils.OppositeDir(dir));
                    Wall wall = currentWallObject.GetComponent<Wall>();
                    wall.Initialize(nearPos, CreationUtils.OppositeDir(dir), nearType, part);
                }
                else {
                    GameObject.Find(CreationUtils.GenerateName(nearPos, CreationUtils.OppositeDir(dir))).GetComponent<Wall>().TypeChange(nearType);
                }
            }
            if (prevType > nearType && nextType <= nearType) {
                Undo.DestroyObjectImmediate(GameObject.Find(CreationUtils.GenerateName(pos, dir)));
            }
            else if (nextType > nearType) {
                if (prevType <= nearType) {
                    GameObject currentWallObject = Instantiate(wallObject);
                    Undo.RegisterCreatedObjectUndo(currentWallObject, "CreateWall");
                    currentWallObject.name = CreationUtils.GenerateName(pos, dir);
                    Wall wall = currentWallObject.GetComponent<Wall>();
                    wall.Initialize(pos, dir, nextType, part);
                }
                else {
                    GameObject.Find(CreationUtils.GenerateName(pos, dir)).GetComponent<Wall>().TypeChange(nextType);
                }
            }
        }
        public static void ShiftGrid(Vector3Int pos, int movedir) {
            int nextType = editorData.getType(pos);
            Vector3Int movepos = CreationUtils.NextPos(pos, movedir);
            int prevType = editorData.getType(movepos);
            for (int i = 0; i < 6; i++) {
                GameObject partdata = getPartData(pos, i, nextType);
                SetWall(movepos, i, prevType, nextType, partdata);
            }
            editorData.setType(movepos, nextType);
        }
        public static GameObject getPartData(Vector3Int pos, int dir, int type) {
            Vector3Int nearPos = CreationUtils.NextPos(pos, dir);
            int nearType = editorData.getType(nearPos);
            if (type > nearType) return GameObject.Find(CreationUtils.GenerateName(pos, dir))?.GetComponent<Wall>().DestroyPart();
            return null;
        }
        public void Shift(SelectionData data, int dir) {
            repeat[dir](data);
        }
        public static void ConnectGrid(Vector3Int pos, int dir) {
            Vector3Int nextpos = CreationUtils.NextPos(pos, dir);
            Vector3Int prevpos = CreationUtils.NextPos(pos, CreationUtils.OppositeDir(dir));
            int nextType = editorData.getType(nextpos);
            int prevType = editorData.getType(prevpos);
            if (nextType != 0 && nextType == prevType) SetGrid(pos, nextType);
            else SetGrid(pos,0);
        }
        #region repeat
        delegate void RepeatUnit(Vector3Int pos, int dir);
        delegate void Repeat(SelectionData data);
        Repeat[] repeat = { Xrev, Xdir,Yrev,Ydir,Zrev, Zdir };
        static void Zdir(SelectionData data) {
            for (int z = data.min.z; z <= data.max.z; z++) {
                repeatXY(data,z,ShiftGrid, 5);
            }
            repeatXY(data,data.max.z, ConnectGrid, 5);
        }
        static void Ydir(SelectionData data) {
            for (int y = data.min.y; y <= data.max.y; y++) {
                repeatXZ(data, y, ShiftGrid, 3);
            }
            repeatXZ(data, data.max.y, ConnectGrid, 3);
        }
        static void Xdir(SelectionData data) {
            for (int x = data.min.x; x <= data.max.x; x++) {
                repeatYZ(data, x, ShiftGrid, 1);
            }
            repeatYZ(data, data.max.x, ConnectGrid, 1);
        }
        static void Zrev(SelectionData data) {
            for (int z = data.max.z; z >= data.min.z; z--) {
                repeatXY(data, z, ShiftGrid, 4);
            }
            repeatXY(data, data.min.z, ConnectGrid, 4);
        }
        static void Yrev(SelectionData data) {
            for (int y = data.max.y; y >= data.min.y; y--) {
                repeatXZ(data, y, ShiftGrid, 2);
            }
            repeatXZ(data, data.min.y, ConnectGrid, 2);
        }
        static void Xrev(SelectionData data) {
            for (int x = data.max.x; x >= data.min.x; x--) {
                repeatYZ(data, x, ShiftGrid, 0);
            }
            //repeatYZ(data, data.min.x, ConnectGrid, 0);
        }
        static void repeatXY(SelectionData data, int z, RepeatUnit unit, int dir) {
            Vector3Int pos = new Vector3Int(0,0,z);
            for (pos.x = data.min.x; pos.x <= data.max.x; pos.x++) for (pos.y = data.min.y; pos.y <= data.max.y; pos.y++) {
                    unit(pos, dir);
                }
        }
        static void repeatXZ(SelectionData data, int y, RepeatUnit unit, int dir) {
            Vector3Int pos = new Vector3Int(0, y, 0);
            for (pos.x = data.min.x; pos.x <= data.max.x; pos.x++) for (pos.z = data.min.z; pos.z <= data.max.z; pos.z++) {
                    unit(pos, dir);
                }
        }
        static void repeatYZ(SelectionData data, int x, RepeatUnit unit, int dir) {
            Vector3Int pos = new Vector3Int(x, 0, 0);
            for (pos.y = data.min.y; pos.y <= data.max.y; pos.y++) for (pos.z = data.min.z; pos.z <= data.max.z; pos.z++) {
                    unit(pos, dir);
                }
        }
        #endregion
        public void LevelUp(SelectionData data) {
            Vector3Int pos = Vector3Int.zero;
            for(pos.x = data.min.x; pos.x<=data.max.x; pos.x++) for (pos.y = data.min.y; pos.y <= data.max.y; pos.y++) for (pos.z = data.min.z; pos.z <= data.max.z; pos.z++) {
                        ShiftGrid(pos, data.dir);
                    }
        }
        public void LevelDown(SelectionData data) {
            Vector3Int pos = Vector3Int.zero;
            for (pos.x = data.min.x; pos.x <= data.max.x; pos.x++) for (pos.y = data.min.y; pos.y <= data.max.y; pos.y++) for (pos.z = data.min.z; pos.z <= data.max.z; pos.z++) {
                        ShiftGrid(CreationUtils.NextPos(pos,data.dir), CreationUtils.OppositeDir(data.dir));
                    }
        }
        public void TypeChange(SelectionData data, int type) {
            Vector3Int pos = Vector3Int.zero;
            for (pos.x = data.min.x; pos.x <= data.max.x; pos.x++) for (pos.y = data.min.y; pos.y <= data.max.y; pos.y++) for (pos.z = data.min.z; pos.z <= data.max.z; pos.z++) {
                        SetGrid(pos, type);
                    }
        }
        
        
        public MapData EditorData => editorData;

        [ContextMenu("Gizmos")]
        void setGizmos(){
            seeGizmos = !seeGizmos;
        }
        bool seeGizmos;
        void OnDrawGizmos() {
            if (!seeGizmos) return;
            int m = 50 / 2;
            int s = m - 5, e = m + 5;
            for (int x = s; x < e; x++)
                for (int y = s; y < e; y++)
                    for (int z = s; z < e; z++) {
                        int i = editorData.getType(new Vector3Int(x,y,z));
                        if (i == 0) Gizmos.color = Color.white;
                        if (i == 1) Gizmos.color = Color.red;
                        if (i == 2) Gizmos.color = Color.blue;
                        Gizmos.DrawCube(new Vector3(x, y, z), new Vector3(0.2f, 0.2f, 0.2f));
                    }
        }
    }
}