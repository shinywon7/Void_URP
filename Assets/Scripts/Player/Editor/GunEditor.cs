using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.TerrainTools;

[CustomEditor(typeof(Gun))]
public class GunEditor : Editor
{   
    bool _secondOrderPropertiesChanged;
    Gun _gun;

    SerializedProperty HoldingTarget;
    SerializedProperty frontLayerMask, backLayerMask;
    SerializedProperty f,z,r;

    private void OnEnable() {
        _gun = (Gun)target;

        HoldingTarget = serializedObject.FindProperty("HoldingTarget");
        frontLayerMask = serializedObject.FindProperty("frontLayerMask");
        backLayerMask = serializedObject.FindProperty("backLayerMask");
        f = serializedObject.FindProperty("_f");
        z = serializedObject.FindProperty("_z");
        r = serializedObject.FindProperty("_r");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        GunHolderGUI();
        EditorGUILayout.Space();
        SecondOrderPropertiesGUI();
        serializedObject.ApplyModifiedProperties();

        if(_secondOrderPropertiesChanged){
            _gun.SecondOrderDynamics();
            _secondOrderPropertiesChanged = false;
        }
    }
    private void GunHolderGUI(){
        GUILayout.Label("Holder Properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(HoldingTarget);
        EditorGUILayout.PropertyField(frontLayerMask);
        EditorGUILayout.PropertyField(backLayerMask);
    }
    private void SecondOrderPropertiesGUI(){
        EditorGUI.BeginChangeCheck();

        GUILayout.Label("Second Order Properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(f);
        EditorGUILayout.PropertyField(z);
        EditorGUILayout.PropertyField(r);

        if(EditorGUI.EndChangeCheck()){
            _secondOrderPropertiesChanged = true;
        }
    }
}
