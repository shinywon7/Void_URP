using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Gear))]
public class GearEditor : Editor
{
    Gear gear;
    static readonly string[] machineNames = { "Switch","Trigger" };
    public override void OnInspectorGUI()
    {
        int Index = GUILayout.Toolbar(0, machineNames);
        
    }
    private void OnEnable()
    {
        gear = (Gear)target;
        ScriptableObject a = (ScriptableObject)target;
        Debug.Log(a.name);
    }
}
