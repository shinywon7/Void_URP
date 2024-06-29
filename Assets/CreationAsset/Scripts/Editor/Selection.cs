using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MapCreation {
    public class AreaSelection {
        public GameObject sideSelectionObject, spaceSelectionObject;
        public SideSelection sideSelection;
        public SpaceSelection spaceSelection;
        public Action markInvisible;
        public Action markVisible;
        public MapCreator creator;
        GameObject mode;
        public SelectionData data;
        bool beginChanged;
        public GameObject Begin
        {
            get { return data.Begin; }
            set {
                if (data.Begin != value) {
                    data.Begin = value;
                    beginChanged = true;
                }
            }
        }
        public GameObject End
        {
            get { return data.End; }
            set {
                if (beginChanged || data.End != value) {
                    beginChanged = false;
                    data.End = value;
                    if (!value) {
                        Invisible();
                        return;
                    }
                    SelectionUpdate();
                }
            }
        }
        public GameObject Mode
        {
            get { return mode; }
            set {
                if (mode == value) return;
                mode?.SetActive(false);
                mode = value;
                mode?.SetActive(true);
            }
        }
        void SelectionUpdate() {
            if (data.FlatCheck()) {
                Mode = sideSelectionObject;
                sideSelection.UpdateSelection();
            }
            else {
                Mode = spaceSelectionObject;
                spaceSelection.UpdateSelection();
            }
        }
        public void ChangeType(int type) {
            Undo.SetCurrentGroupName("ChangeType");
            creator.TypeChange(data, type);
        }
        public void Invisible() {
            Mode = null;
        }
        public void Initialize(MapCreator _creator) {
            data = new SelectionData();
            creator = _creator;
            sideSelectionObject = GameObject.Instantiate(creator.sideSelectionObject);
            sideSelectionObject.SetActive(false);
            sideSelection = sideSelectionObject.GetComponent<SideSelection>();
            sideSelection.Initialize(this);

            spaceSelectionObject = GameObject.Instantiate(creator.spaceSelectionObject);
            spaceSelectionObject.SetActive(false);
            spaceSelection = spaceSelectionObject.GetComponent<SpaceSelection>();
            spaceSelection.Initialize(this);
            Invisible();
        }
        public void Disabled() {
            GameObject.DestroyImmediate(sideSelectionObject);
            GameObject.DestroyImmediate(spaceSelectionObject);
        }
    }

}