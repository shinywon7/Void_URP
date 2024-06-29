using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
[CustomEditor(typeof(TestCreator))]
public class TestEditor : Editor
{
    TestCreator creator;
    void OnEnable() {
        creator = (TestCreator)target;
    }
    
    void OnSceneGUI() {
        Undo.RecordObject(creator,"test");
        if(Event.current.type == EventType.MouseDown) {
            creator.Move();
        }
        if (Event.current.type == EventType.Layout) {
            HandleUtility.AddDefaultControl(0);
        }
    }
}
