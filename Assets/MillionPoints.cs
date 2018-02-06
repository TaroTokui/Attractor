using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.InteropServices;

public class MillionPoints : MonoBehaviour {

    // ==============================
    #region // Defines
        
    const int ThreadBlockSize = 256;

    struct ParticleData
    {
        Vector3 Position;
        Vector3 Velocity;
        Vector3 Albedo;
        float Life;
        bool isActive;
    }

    #endregion // Defines

    // --------------------------------------------------
    #region // Serialize Fields

    [SerializeField]
    int _particleCount = 1000000;
    
    [SerializeField]
    float _life = 10;

    [SerializeField]
    [Range(0, 100)]
    float _range = 50;

    [SerializeField]
    ComputeShader _ComputeShader;
    
    [SerializeField]
    Material _material;

    [SerializeField]
    Vector3 _MeshScale = new Vector3(1f, 1f, 1f);

    /// 表示領域の中心座標
    [SerializeField]
    Vector3 _BoundCenter = Vector3.zero;
    
    /// 表示領域のサイズ
    [SerializeField]
    Vector3 _BoundSize = new Vector3(300f, 300f, 300f);

    #endregion // Serialize Fields

    // --------------------------------------------------
    #region // Private Fields

    ComputeBuffer _ParticleDataBuffer;
    
    /// GPU Instancingの為の引数
    uint[] _GPUInstancingArgs = new uint[5] { 0, 0, 0, 0, 0 };
    
    /// GPU Instancingの為の引数バッファ
    ComputeBuffer _GPUInstancingArgsBuffer;

    // point for particle
    Mesh _pointMesh;

    Vector3 mousePos;
    float attraction;
    new Camera camera;

    #endregion // Private Fields

    // --------------------------------------------------
    #region // MonoBehaviour Methods

    void Awake()
    {
        Application.targetFrameRate = 90;
        QualitySettings.vSyncCount = 0;
    }

    void Start()
    {
        // バッファ生成
        _ParticleDataBuffer = new ComputeBuffer(_particleCount, Marshal.SizeOf(typeof(ParticleData)));
        _GPUInstancingArgsBuffer = new ComputeBuffer(1, _GPUInstancingArgs.Length * sizeof(uint), ComputeBufferType.IndirectArguments);

        // 初期化
        int kernelId = _ComputeShader.FindKernel("Init");
        _ComputeShader.SetFloat("_Life", _life);
        _ComputeShader.SetFloat("_Range", _range);
        _ComputeShader.SetBuffer(kernelId, "_ParticleDataBuffer", _ParticleDataBuffer);
        _ComputeShader.Dispatch(kernelId, (Mathf.CeilToInt(_particleCount / ThreadBlockSize) + 1), 1, 1);
        
        // creat point mesh
        _pointMesh = new Mesh();
        _pointMesh.vertices = new Vector3[] {
            new Vector3 (0, 0),
        };
        _pointMesh.normals = new Vector3[] {
            new Vector3 (0, 1, 0),
        };
        _pointMesh.SetIndices(new int[] { 0 }, MeshTopology.Points, 0);

        camera = Camera.main;
    }

    void Update()
    {
        mousePos = Input.mousePosition;
        mousePos.z = 5f;
        mousePos = camera.ScreenToWorldPoint(mousePos);
        attraction = Input.GetMouseButton(0) ? 0.1f : 0f;

        // ComputeShader
        int kernelId = _ComputeShader.FindKernel("Update");
        _ComputeShader.SetFloat("_time", Time.time / 5.0f);
        _ComputeShader.SetFloat("dt", Time.deltaTime);
        _ComputeShader.SetFloat("_Life", _life);
        _ComputeShader.SetFloat("_Range", _range);
        _ComputeShader.SetVector("mousePos", mousePos);
        _ComputeShader.SetFloat("attraction", attraction);
        _ComputeShader.SetBuffer(kernelId, "_ParticleDataBuffer", _ParticleDataBuffer);
        _ComputeShader.Dispatch(kernelId, (Mathf.CeilToInt(_particleCount / ThreadBlockSize) + 1), 1, 1);
        
        // GPU Instaicing
        _GPUInstancingArgs[0] = (_pointMesh != null) ? _pointMesh.GetIndexCount(0) : 0;
        _GPUInstancingArgs[1] = (uint)_particleCount;
        _GPUInstancingArgsBuffer.SetData(_GPUInstancingArgs);
        _material.SetBuffer("_ParticleDataBuffer", _ParticleDataBuffer);
        _material.SetVector("_MeshScale", _MeshScale);
        Graphics.DrawMeshInstancedIndirect(_pointMesh, 0, _material, new Bounds(_BoundCenter, _BoundSize), _GPUInstancingArgsBuffer);
    }

    void OnDestroy()
    {
        if (_ParticleDataBuffer != null)
        {
            _ParticleDataBuffer.Release();
            _ParticleDataBuffer = null;
        }
        if (_GPUInstancingArgsBuffer != null)
        {
            _GPUInstancingArgsBuffer.Release();
            _GPUInstancingArgsBuffer = null;
        }
    }
    
    #endregion // MonoBehaviour Method
}
