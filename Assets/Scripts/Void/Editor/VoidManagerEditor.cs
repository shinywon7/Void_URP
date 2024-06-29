using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VoidManager))]
public class VoidManagerEditor : Editor
{
    bool _secondOrderPropertiesChanged;
    VoidManager _voidManager;

    SerializedProperty maxVoidWidth, minVoidWidth;
    SerializedProperty appearSpeed, disappearSpeed;
    SerializedProperty f,z,r;

    private void OnEnable() {
        _voidManager = (VoidManager)target;

        maxVoidWidth = serializedObject.FindProperty("maxVoidWidth");
        minVoidWidth = serializedObject.FindProperty("minVoidWidth");
        appearSpeed = serializedObject.FindProperty("appearSpeed");
        disappearSpeed = serializedObject.FindProperty("disappearSpeed");
        f = serializedObject.FindProperty("_f");
        z = serializedObject.FindProperty("_z");
        r = serializedObject.FindProperty("_r");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        
        DefaultPropertiesGUI();
        EditorGUILayout.Space();
        SecondOrderPropertiesGUI();
        serializedObject.ApplyModifiedProperties();

        if(_secondOrderPropertiesChanged){
            _voidManager.SecondOrderDynamics();
            _secondOrderPropertiesChanged = false;
        }
    }
    private void DefaultPropertiesGUI(){
        GUILayout.Label("Default Properties", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(maxVoidWidth);
        EditorGUILayout.PropertyField(minVoidWidth);
        EditorGUILayout.PropertyField(appearSpeed);
        EditorGUILayout.PropertyField(disappearSpeed);
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
