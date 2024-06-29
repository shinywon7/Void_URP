using System;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Events;
using Unity.Mathematics;
using System.Runtime.InteropServices;
using UnityEngine.Rendering.Universal.Internal;
using DG.Tweening.Plugins.Options;

namespace Wave
{
    [ExecuteInEditMode]
    public class WaveSurface : MonoBehaviour
    {
        Transform frontSurface, backSurface;
        public int Resolution {get => _resolution; set => SetResolution(value); }
        public float Size {get => _size; set => SetSize(value); }
        public ComputeShader surfaceSimulation;

        [Min(0f)]    
        public float propagationSpeed;

        [Range(0f,10f)]
        public float damping;

        [Range(0,1)]
        public float waveSmoothness;

        [Range(0,2)]
        public float speedTweak;

        [Min(1)]
        public int substeps;    

        public float verticalPushScale;
        public float horizontalPushScale;
        [Range(0,4)]
        public float windPower;

        [Range(0.5f,1.5f)]
        public float refractionIndex;

        [Range(0,1)]
        public float fogDensity;
        public Color fogColor;
        float maxOffset;
        float sampleSize;
        float precalculation;

        MeshManager _meshManager;
        public Camera frontCam, backCam;
        public Transform frontTransform, backTransform;
        
        public int _resolution = 50;
        public float _size = 10;
        
        const int MaxResolution = 1000;
        const int MaxSize = 1000;

        public Matrix4x4 cameraMatrix;

        public void Initialize(){
            Uninitialize();
            frontSurface = transform.Find("FrontSurface");
            backSurface = transform.Find("BackSurface");

            _meshManager = new MeshManager(frontSurface.GetComponent<MeshFilter>(), backSurface.GetComponent<MeshFilter>(), _resolution, _size);
            //backSurface.GetComponent<MeshFilter>().mesh = frontSurface.GetComponent<MeshFilter>().sharedMesh;
            if(Application.isPlaying){
                SetSimulationProperties();
                SetShaderProperties();
            }

            frontCam = frontSurface.GetComponentInChildren<Camera>();
            backCam = transform.Find("BackCam").GetComponent<Camera>();
            frontTransform = frontCam.transform;
            backTransform = frontSurface.Find("BackTransform");
            UpdateProjectionMatrix();
        }
        public void UpdateProjectionMatrix()
        {
            float camSize = _size * (_resolution/(float)(_resolution-1));
            //camSize = _size; 
            Matrix4x4 mat = new Matrix4x4();
            mat.SetRow(0,new Vector4(2/camSize,(2/Mathf.Sqrt(3))/camSize,0,0));
            mat.SetRow(1,new Vector4(0,(4/Mathf.Sqrt(3))/camSize,0,0));
            mat.SetRow(2,new Vector4(0,0,-0.02f,-1));
            mat.SetRow(3,new Vector4(0,0,0,1));

            frontCam.projectionMatrix = mat;
            backCam.projectionMatrix = mat;

            mat.SetRow(2,new Vector4(0,0,-1f,0));

            cameraMatrix = mat;
        }
        public void Uninitialize(){
            _meshManager?.Dispose();
        }
        public void UpdateMesh(){
            _meshManager.UpdateMesh(_resolution, _size);
            Initialize();
        }
        public void SetSimulationProperties(){
            sampleSize = _size / _resolution;
            precalculation = propagationSpeed / sampleSize; 
            maxOffset = (1 - waveSmoothness) * sampleSize;
            Shader.SetGlobalFloat("_Damping", damping);
            Shader.SetGlobalFloat("_Precalculation", precalculation);
            Shader.SetGlobalFloat("_SpeedTweak", speedTweak);
            Shader.SetGlobalFloat("_SampleSize", sampleSize);
            Shader.SetGlobalFloat("_MaxOffset", maxOffset);
            Shader.SetGlobalFloat("_Resolution", Resolution);
            Shader.SetGlobalFloat("_VoidSize", Size);
            Shader.SetGlobalFloat("_VerticalPushScale", verticalPushScale);
            Shader.SetGlobalFloat("_HorizontalPushScale", horizontalPushScale);
            Shader.SetGlobalFloat("_WindPower", windPower);
        }
        public void SetShaderProperties(){
            Shader.SetGlobalFloat("_RefractionIndex", refractionIndex);
            Shader.SetGlobalFloat("_FogDensity", fogDensity);
            Shader.SetGlobalColor("_FogColor", fogColor);
        }
        private void Awake() {
            if(!Application.isPlaying)
                return;

            Initialize();
        }
        private void OnEnable(){
            Initialize();
        }
        private void OnDisable() {
            Uninitialize();
        }
        private void SetResolution(int resolution){
            resolution = math.clamp(resolution,3,MaxResolution);
            _resolution = resolution;
        }
        private void SetSize(float size){
            size = math.clamp(size,0.1f,MaxSize);
            _size = size;
        }
        private void Start() {
            VoidManager.voidUpdate += VoidUpdate;
        }
        public void LateUpdate(){
            Shader.SetGlobalMatrix("_w2fMatrix", cameraMatrix*frontTransform.worldToLocalMatrix);
            Shader.SetGlobalMatrix("_w2bMatrix", cameraMatrix*backTransform.worldToLocalMatrix);
            Shader.SetGlobalMatrix("_CameraMatrix", cameraMatrix);
        }
        public void VoidUpdate(){
            backSurface.position = transform.position - VoidManager.voidTransform.up * VoidManager.voidWidth;
        }
    }
}
