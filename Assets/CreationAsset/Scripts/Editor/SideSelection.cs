using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MapCreation {
    [ExecuteInEditMode]
    public class SideSelection : MonoBehaviour {
        public SelectionData data;
        public AreaSelection areaSelection;
        public Wall begin => data.beginWall;
        public Wall end => data.endWall;
        public Transform bound;
        public GameObject arrow;
        public Collider arrowCollider;
        public bool isHoldMark, isMouseOn;
        public Color noHighlight, highlight;
        public Material arrowMat;
        public RaycastHit hitInfo;
        public MapCreator creator;
        int dir;
        public void UpdateSelection() {
            data.SideUpdate();
            transform.position = (Vector3)(begin.pos + end.pos) / 2;
            transform.rotation = data.beginWall.transform.rotation;
            dir = data.dir;
            int dx, dy;
            if (dir == 0 || dir == 1) (dx,dy) = (data.max.z - data.min.z+1, data.max.y- data.min.y+1);
            else if (dir == 2 || dir == 3) (dx,dy) = (data.max.x - data.min.x+1, data.max.z- data.min.z+1);
            else (dx,dy)=(data.max.x - data.min.x+1, data.max.y- data.min.y+1);
            bound.localScale = new Vector3(dx,dy,1);
        }
        public void MouseEnterMark(Ray ray) {
            if (arrowCollider.Raycast(ray, out hitInfo, 1000f)) {
                IsMouseOn = true;
            }
        }
        public void MouseExitMark(Ray ray) {
            if (!arrowCollider.Raycast(ray, out hitInfo, 1000f)) {
                IsMouseOn = false;
            }
        }
        Plane surface, criteria;
        int level, targetLevel;
        int lastUndoGroupIndex;
        public void MouseDownMark() {
            IsHoldMark = true;
            MapEditor.IsHoldMark = true;
            surface.SetNormalAndPosition(arrowCollider.transform.up, hitInfo.point);
            criteria.SetNormalAndPosition(arrowCollider.transform.forward, hitInfo.point);
            level = targetLevel = 0;
            lastUndoGroupIndex = Undo.GetCurrentGroup();
        }
        public void MouseDragMark(Ray ray) {
            surface.Raycast(ray, out float dist);
            targetLevel = Mathf.RoundToInt(criteria.GetDistanceToPoint(ray.origin+ray.direction*dist));
            if (targetLevel > 0) while (targetLevel > level) LevelUp();
            else while (targetLevel > level) UndoLevelUp();
            if (targetLevel < 0) while (targetLevel < level) LevelDown();
            else while (targetLevel < level) UndoLevelDown();

        }
        public void MouseUpMark() {
            IsHoldMark = false;
            MapEditor.IsHoldMark = false;
            GameObject begin = GameObject.Find(CreationUtils.GenerateName(data.min,dir));
            GameObject end = GameObject.Find(CreationUtils.GenerateName(data.max,dir));
            Undo.CollapseUndoOperations(lastUndoGroupIndex);
            if(!begin || !end) {
                areaSelection.End = areaSelection.Begin = null;
                MapEditor.isAreaSelected = false;
            }
            else {
                areaSelection.Begin = begin;
                areaSelection.End = end;
            }
        }
        public void LevelUp() {
            creator.LevelUp(data);
            DataLevelUp();
            Undo.SetCurrentGroupName("SideLevelUp");
            Undo.IncrementCurrentGroup();
        }
        public void UndoLevelUp() {
            Undo.PerformUndo();
            DataLevelUp();
        }
        public void LevelDown() {
            creator.LevelDown(data);
            DataLevelDown();
            Undo.SetCurrentGroupName("SideLevelDown");
            Undo.IncrementCurrentGroup();
        }
        public void UndoLevelDown() {
            Undo.PerformUndo();
            DataLevelDown();
        }
        public void DataLevelUp() {
            data.tempShift(dir);
            transform.position += CreationUtils.dv[dir];
            level++;
        }
        public void DataLevelDown() {
            data.tempShift(CreationUtils.OppositeDir(dir));
            transform.position += CreationUtils.dv[CreationUtils.OppositeDir(dir)];
            level--;
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
            if(!isHoldMark) arrow.SetActive(false);
        }
        public void MarkVisible() {
            arrow.SetActive(true);
        }
        public void OnEnable() {
            MapEditor.MouseEnterMark += MouseEnterMark;
        }
        public void OnDisable() {
            MapEditor.MouseEnterMark -= MouseEnterMark;
        }
    }
}
