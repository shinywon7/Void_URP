using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using System;


namespace MapCreation {
    [CustomEditor(typeof(MapCreator))]
    public class MapEditor : Editor {
        public MapCreator creator;
        PartSelection partSelection;
        static AreaSelection areaSelection;
        public static Action<Ray> MouseEnterMark, MouseExitMark, DragMark;
        public static Action MouseDownMark, MouseUpMark;
        public static bool isSelectPart, isDragSelection, isHoldMark, isAreaSelected;
        public static bool isHoverMark, isHoldPart, isSnapPart;
        public static CreationLibrary library;
        public static GameObject holdingPart;
        public static Wall snappedWall;
        Vector2 scrollPosition;
        Event e;
        void OnSceneGUI() {
            Handles.BeginGUI();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, creator.styles[0]);
            foreach(var part in creator.creationLibrary.parts) {
                if (GUILayout.Button(part.sampleTex, creator.styles[1])) {
                    if (!isHoldPart) {
                        Debug.Log("Click");
                        holdingPart = Instantiate(part.prefab);
                        Undo.RegisterCreatedObjectUndo(holdingPart, "PartCreated");
                        isHoldPart = true;
                    }
                }
            }
            GUILayout.EndScrollView();
            Handles.EndGUI();
            
            Ray mouseRay = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
            e = Event.current;
            if (!IsHoldMark && !isDragSelection) {
                MouseExitMark?.Invoke(mouseRay);
                if(!IsHoverMark) MouseEnterMark?.Invoke(mouseRay); 
            }
            if(e.type == EventType.KeyDown) {
                KeyDown();
            }
            if (e.type == EventType.MouseMove && isHoldPart) {
                RaycastHit hitInfo;
                Physics.Raycast(mouseRay, out hitInfo, 1000f, library.wallMask);
                snappedWall = hitInfo.transform?.GetComponent<Wall>();
                if (hitInfo.collider && data.getType(snappedWall.frontPos) == 0) {
                    holdingPart.transform.position = hitInfo.transform.position;
                    holdingPart.transform.LookAt(hitInfo.transform.position + hitInfo.transform.right, hitInfo.transform.up);
                    isSnapPart = true;
                }
                else {
                    holdingPart.transform.position = mouseRay.GetPoint(8f);
                    isSnapPart = false;
                }
            }
            else if (e.type == EventType.MouseDown && e.button == 0) {
                MouseDown(mouseRay);
            }
            else if (e.type == EventType.MouseDrag && e.button == 0) {
                if (IsHoldMark) {
                    DragMark(mouseRay);
                }
                else if (isDragSelection) {
                    RaycastHit hitInfo;
                    Physics.Raycast(mouseRay, out hitInfo, 1000f, library.wallMask);
                    if (hitInfo.collider) areaSelection.End = hitInfo.collider.gameObject;
                }
            }
            else if (e.type == EventType.MouseUp && e.button == 0) {
                if (IsHoldMark) MouseUpMark();
                else if(isDragSelection) isDragSelection = false;
            }
            HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive));
        }
        public void KeyDown() {
            if (isAreaSelected) {
                if(e.keyCode == KeyCode.Minus) {
                    areaSelection.ChangeType(0);
                }
                else if (e.keyCode == KeyCode.LeftBracket) {
                    areaSelection.ChangeType(1);
                }
                else if (e.keyCode == KeyCode.RightBracket) {
                    areaSelection.ChangeType(2);
                }
            }
        }
        public void MouseDown(Ray mouseRay) {
            if (isSnapPart) {
                isSnapPart = false;
                isHoldPart = false;
                snappedWall.part = holdingPart;
                holdingPart.GetComponent<Part>().wall = snappedWall;
                Undo.RecordObject(snappedWall,"PartLocated");
                Undo.RecordObject(holdingPart.GetComponent<Part>(), "PartLocated");
                Undo.IncrementCurrentGroup();
            }
            else if (IsHoverMark) { 
                MouseDownMark();
            }
            else{
                RaycastHit hitInfo;
                Physics.Raycast(mouseRay, out hitInfo);
                if (hitInfo.collider) {
                    if ((hitInfo.transform.gameObject.layer & library.wallMask) != 0) {
                        areaSelection.End = areaSelection.Begin = hitInfo.collider.gameObject;
                        isAreaSelected = true;
                        isDragSelection = true;
                    }
                    else if((hitInfo.transform.gameObject.layer & library.partMask) != 0) {
                        isSelectPart = true;
                        areaSelection.End = areaSelection.Begin = hitInfo.transform.GetComponent<Part>().wall.gameObject;
                        isAreaSelected = true;
                    }
                    Repaint();
                }
                else {
                    areaSelection.End = areaSelection.Begin = null;
                    isAreaSelected = false;
                }
            }
        }
        public static bool IsHoverMark
        {
            get { return isHoverMark; }
            set { isHoverMark = value; }
        }
        public static bool IsHoldMark
        {
            get { return isHoldMark; }
            set {
                if (isHoldMark == value) return;
                if(value) {
                    if (isAreaSelected) areaSelection.markInvisible();
                }
                else if (!value) {
                    if (isAreaSelected) areaSelection.markVisible();
                }
                isHoldMark = value;
            }
        }
        void OnEnable() {
            creator = (MapCreator)target;
            library = creator.creationLibrary;
            isSelectPart= isDragSelection= isHoldMark= isAreaSelected =isHoverMark = false;
            MouseEnterMark = MouseExitMark = DragMark = null;
            MouseDownMark = MouseUpMark = null;
            creator.InitializeMapData(50);
            partSelection = new PartSelection();
            areaSelection = new AreaSelection();
            areaSelection.Initialize(creator);
            Undo.undoRedoPerformed -= SelectReset;
            Undo.undoRedoPerformed += SelectReset;
        }
        void OnDisable() {
            areaSelection.Disabled();
        }
        void SelectReset() {
            areaSelection.End = areaSelection.Begin = null;
            isAreaSelected = false;
            isDragSelection = false;
            if (isHoldPart) {
                isHoldPart = false;
                isSnapPart = false;
            }
        }
        
        MapData data => creator.EditorData;
    }
    
    public class PartSelection {
        public void Invisible() {

        }
        public void Visible() {

        }
    }
    
    
}
