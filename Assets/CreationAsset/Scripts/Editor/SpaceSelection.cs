using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MapCreation {
    [ExecuteInEditMode]
    public class SpaceSelection : MonoBehaviour {
        public SelectionData data;
        public AreaSelection areaSelection;
        public Transform bound;
        public GameObject markObject;
        public GameObject joystick,arrow;
        public Collider joystickCollider;
        public bool isHoldMark, isMouseOn;
        public Color noHighlight, highlight;
        public Material arrowMat;
        public RaycastHit hitInfo;
        public MapCreator creator;
        public BothSideArrow bothSideArrow;

        public void UpdateSelection() {
            data.SpaceUpdate();
            transform.position = (Vector3)(data.max + data.min) / 2;
            bound.localScale = data.max - data.min + Vector3Int.one;
        }
        public void MouseEnterMark(Ray ray) {
            if (joystickCollider.Raycast(ray, out hitInfo, 1000f)) {
                IsMouseOn = true;
            }
        }
        public void MouseExitMark(Ray ray) {
            if (!joystickCollider.Raycast(ray, out hitInfo, 1000f)) {
                IsMouseOn = false;
            }
        }
        int lastUndoGroupIndex;
        Plane baseSurface,surface, criteria;
        public void MouseDownMark() {
            IsHoldMark = true;
            MapEditor.IsHoldMark = true;
            baseSurface.SetNormalAndPosition(joystickCollider.transform.forward, hitInfo.point);
            level = targetLevel = 0;
            joystick.SetActive(false);
            arrow.SetActive(true);
            lastUndoGroupIndex = Undo.GetCurrentGroup();
        }
        int dir;
        float maxDist;
        public int level, targetLevel;
        Vector3 toCam;
        Vector3 a, b;
        public void MouseDragMark(Ray ray) {
            maxDist = -1f;
            targetLevel = 0;
            baseSurface.SetNormalAndPosition(joystickCollider.transform.forward, hitInfo.point);

            toCam = ray.origin - hitInfo.point;
            if (level == 0) {
                baseSurface.Raycast(ray, out float mouseDist);
                Vector3 vec = (ray.origin + ray.direction * mouseDist) - hitInfo.point;
                CalcDist(Vector3.right, vec, 0);
                CalcDist(Vector3.up, vec, 2);
                CalcDist(Vector3.forward, vec, 4);
                bothSideArrow.Update();
            }
            surface.Raycast(ray, out float dist);
            targetLevel = Mathf.Clamp(Mathf.RoundToInt(criteria.GetDistanceToPoint(ray.origin + ray.direction * dist)),0,100);
            while (targetLevel > level) Shift();
            while (targetLevel < level) UndoShift();
        }
        public void CalcDist(Vector3 unitVec, Vector3 vec, int _dir) {
            float dist = Vector3.Dot(vec, Vector3.ProjectOnPlane(unitVec, baseSurface.normal).normalized);
            if(Mathf.Abs(dist) > maxDist) {
                if (dist > 0) { 
                    maxDist = dist;
                    dir = _dir;
                    criteria.SetNormalAndPosition(unitVec,hitInfo.point);
                }
                else {
                    maxDist = -dist;
                    dir = CreationUtils.OppositeDir(_dir);
                    criteria.SetNormalAndPosition(-unitVec, hitInfo.point);
                }
                surface.SetNormalAndPosition(Vector3.ProjectOnPlane(toCam, criteria.normal).normalized, hitInfo.point);
                bothSideArrow.dir = CreationUtils.dv[_dir];
            }
        }
        public void MouseUpMark() {
            IsHoldMark = false;
            MapEditor.IsHoldMark = false;
            joystick.SetActive(true);
            arrow.SetActive(false);
            Undo.CollapseUndoOperations(lastUndoGroupIndex);
        }
       
        public void Shift() {

            Debug.Log("shifted!");
            level++;
            creator.Shift(data, dir);
            data.tempShift(dir);
            Undo.RecordObject(transform,"SpaceShift");
            transform.position += CreationUtils.dv[dir];
            Undo.SetCurrentGroupName("SpaceShifted");
            Undo.IncrementCurrentGroup();
        }
        public void UndoShift() {
            Debug.Log("Undoshifted!");
            level--;
            Undo.PerformUndo();
            data.tempShift(CreationUtils.OppositeDir(dir));
            //transform.position += CreationUtils.dv[CreationUtils.OppositeDir(dir)];
        }
        bool IsHoldMark
        {
            get { return IsHoldMark; }
            set {
                isHoldMark = value;
            }
        }
        bool IsMouseOn
        {
            get { return isMouseOn; }
            set {
                if (isMouseOn == value) return;
                isMouseOn = value;
                if (value) {
                    arrowMat.SetColor("_Color", highlight);
                    MapEditor.IsHoverMark = true;
                    MapEditor.MouseDownMark += MouseDownMark;
                    MapEditor.DragMark += MouseDragMark;
                    MapEditor.MouseUpMark += MouseUpMark;
                    MapEditor.MouseExitMark += MouseExitMark;
                }
                else {
                    arrowMat.SetColor("_Color", noHighlight);
                    MapEditor.IsHoverMark = false;
                    MapEditor.MouseDownMark -= MouseDownMark;
                    MapEditor.DragMark -= MouseDragMark;
                    MapEditor.MouseUpMark -= MouseUpMark;
                    MapEditor.MouseExitMark -= MouseExitMark;
                }
            }
        }

        public void Initialize(AreaSelection _areaSelection) {
            areaSelection = _areaSelection;
            areaSelection.markInvisible = MarkInvisible;
            areaSelection.markVisible = MarkVisible;
            creator = areaSelection.creator;
            data = areaSelection.data;
        }
        public void MarkInvisible() {
            if (!isHoldMark) markObject.SetActive(false);
        }
        public void MarkVisible() {
            markObject.SetActive(true);
        }
        public void OnEnable() {
            MapEditor.MouseEnterMark += MouseEnterMark;
        }
        public void OnDisable() {
            MapEditor.MouseEnterMark -= MouseEnterMark;
        }
        void OnDrawGizmos() {
            Gizmos.DrawSphere(hitInfo.point, 0.1f);
            Gizmos.DrawSphere(a, 0.1f);
            Gizmos.DrawLine(a,b);
        }
    }
}
