using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace Wave.Editor
{
    [CustomEditor(typeof(WaveSurface))]
    public class WaveSurfaceEditor : UnityEditor.Editor
    {
        float _auxSize;
        int _auxResolution;
        bool _meshShapeChanged = false;
        bool _simulationPropertiesChanged = false;
        bool _shaderPropertiesChanged = false;
        WaveSurface _surface;

        //SerializedProperty surfaceSimulation;
        SerializedProperty propagationSpeed;
        SerializedProperty damping;
        SerializedProperty waveSmoothness;
        SerializedProperty speedTweak;
        SerializedProperty substeps;
        SerializedProperty verticalPushScale;
        SerializedProperty horizontalPushScale;
        SerializedProperty windPower;
        SerializedProperty refractionIndex;
        SerializedProperty fogDensity;
        SerializedProperty fogColor;

        private void OnEnable() {
            _meshShapeChanged = false;
            _surface = (WaveSurface)target;

            _auxSize = _surface.Size;
            _auxResolution = _surface.Resolution;

            //surfaceSimulation = serializedObject.FindProperty("surfaceSimulation");
            propagationSpeed = serializedObject.FindProperty("propagationSpeed");
            damping = serializedObject.FindProperty("damping");
            waveSmoothness = serializedObject.FindProperty("waveSmoothness");
            speedTweak = serializedObject.FindProperty("speedTweak");
            substeps = serializedObject.FindProperty("substeps");
            verticalPushScale = serializedObject.FindProperty("verticalPushScale");
            horizontalPushScale = serializedObject.FindProperty("horizontalPushScale");
            windPower = serializedObject.FindProperty("windPower");
            refractionIndex = serializedObject.FindProperty("refractionIndex");
            fogDensity = serializedObject.FindProperty("fogDensity");
            fogColor = serializedObject.FindProperty("fogColor");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            if(!Application.isPlaying){
                //EditorGUILayout.Space();
                DrawMeshShapeGUI();
            }
            //EditorGUILayout.PropertyField(surfaceSimulation);

            EditorGUILayout.Space();
            DrawWaveSimulationPropertiesGUI();
            EditorGUILayout.Space();
            DrawWaveShaderPropertiesGUI();
            serializedObject.ApplyModifiedProperties();

            if(_simulationPropertiesChanged){
                _surface.SetSimulationProperties();
                _simulationPropertiesChanged = false;
            }
            if(_shaderPropertiesChanged){
                _surface.SetShaderProperties();
                _shaderPropertiesChanged = false;
            }
        }
        private void DrawWaveShaderPropertiesGUI(){
            EditorGUI.BeginChangeCheck();

            GUILayout.Label("Wave Shader Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(refractionIndex);
            EditorGUILayout.PropertyField(fogDensity);
            EditorGUILayout.PropertyField(fogColor);

            if(EditorGUI.EndChangeCheck()){
                _shaderPropertiesChanged = true;
            }
        }
        private void DrawWaveSimulationPropertiesGUI(){
            EditorGUI.BeginChangeCheck();

            GUILayout.Label("Wave Simulation Properties", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(propagationSpeed);
            EditorGUILayout.PropertyField(damping);
            EditorGUILayout.PropertyField(waveSmoothness);
            EditorGUILayout.PropertyField(speedTweak);
            EditorGUILayout.PropertyField(substeps);
            EditorGUILayout.PropertyField(verticalPushScale);
            EditorGUILayout.PropertyField(horizontalPushScale);
            EditorGUILayout.PropertyField(windPower);

            if(EditorGUI.EndChangeCheck()){
                _simulationPropertiesChanged = true;
            }
        }
        private void DrawMeshShapeGUI(){
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("Mesh Shape", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.LabelField("Size", GUILayout.MinWidth(15));
            _auxSize = EditorGUILayout.FloatField(_auxSize, GUILayout.ExpandWidth(true));
            EditorGUILayout.LabelField("Resolution", GUILayout.MinWidth(15));
            _auxResolution = EditorGUILayout.IntField(_auxResolution, GUILayout.ExpandWidth(true));
            EditorGUILayout.Space();

            if(EditorGUI.EndChangeCheck())
                _meshShapeChanged = true;

            EditorGUILayout.EndHorizontal();
            if(_meshShapeChanged){
                EditorGUILayout.Space();
                if(GUILayout.Button("Apply")){
                    Undo.RecordObject(_surface, "Wave surface shape changed");
                    _surface.Size = _auxSize;
                    _surface.Resolution = _auxResolution;
                    _surface.UpdateMesh();

                    _auxSize = _surface.Size;
                    _auxResolution = _surface.Resolution;
                    _meshShapeChanged = false;
                }
            }

            EditorGUILayout.Space();
            EditorGUILayout.EndVertical();
        }
    }
}
