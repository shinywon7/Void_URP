using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MapCreation {
    [ExecuteInEditMode]
    public class Wall : MonoBehaviour {
        public int type = 1;
        public int dir;
        public Vector3Int pos, frontPos;
        public GameObject part;
        public CreationLibrary creationLibrary;
        public Renderer myRenderer;
        public void Initialize(Vector3Int v,int _dir, int _type, GameObject part = null) {
            Undo.RecordObject(transform,"SetWallTransform");
            pos = v;
            dir = _dir;
            type = _type;
            frontPos = CreationUtils.NextPos(pos, dir);
            transform.SetPositionAndRotation(v + (Vector3)CreationUtils.dv[_dir] * 0.5f, Quaternion.Euler(CreationUtils.dr[_dir]));
            if (type == 1) myRenderer.material = creationLibrary.NonBarrier;
            else if (type == 2) myRenderer.material = creationLibrary.Basic;
            Undo.undoRedoPerformed += OnEnable;
        }
        public void TypeChange(int _type) {
            Undo.RecordObject(this, "TypeChange");
            Undo.RecordObject(myRenderer, "RendererTypeChange");
            if(_type == 1) myRenderer.material = creationLibrary.NonBarrier;
            else myRenderer.material = creationLibrary.Basic;
            type = _type;
        }
        public GameObject DestroyPart() {
            return part;
        }
        public int prevFrontType = -1;
        public void OnDestroy() {
            prevFrontType =  MapCreator.editorData.getType(frontPos);
            Undo.undoRedoPerformed -= OnEnable;
        }
        public void OnEnable() {
            if(prevFrontType != -1 && pos != Vector3Int.zero) {
                MapCreator.editorData.setType(pos,type);
                MapCreator.editorData.setType(frontPos,prevFrontType);
            }
        }
    }
}
