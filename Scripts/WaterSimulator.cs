// TODO refactor code
using System.Collections;
using System.Linq;
using UnityEngine;
using URPCameraData = UnityEngine.Rendering.Universal.UniversalAdditionalCameraData;

[AddComponentMenu("WaterSimulation/Water Simulator"),
RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
[DefaultExecutionOrder(EXECUTION_ORDER)]
public class WaterSimulator : MonoBehaviour {
    public const int EXECUTION_ORDER = 100;
    // public struct DimensionInfo {
    //     // TODO lets see whether these need to be aligned
    //     public readonly Vector3 Size;
    //     public readonly Vector2 StepSize;
    //     public readonly Vector2 StepSizeInv;
    //     public readonly Vector2Int Resolution;

    //     public DimensionInfo (Vector3 size, Vector2Int resolution) {
    //         Resolution = resolution;
    //         Size = size;
    //         StepSize = new Vector2(Size.x / (Resolution.x - 1), Size.z / (Resolution.y - 1));
    //         StepSizeInv = new Vector2(1f / StepSize.x, 1f / StepSize.y);
    //     }
    // }

    public readonly static Color GIZMO_COLOR = Color.green;
    public const int KERNEL_SIZE = 8;

    [SerializeField, Step(KERNEL_SIZE, 2)]
    Vector2Int _resolution = Vector2Int.one * 256;
    [SerializeField]
    LayerMask _groundLayer = ~0;
    [SerializeField]
    Vector3 _size = Vector3.one * 10f;
    
    [SerializeField, HideInInspector]
    Camera _groundDepthCamera;
    [SerializeField, HideInInspector]
    GameObject _cameraHolder;
    [SerializeField, HideInInspector]
    MeshRenderer _meshRenderer;
    [SerializeField, HideInInspector]
    MeshFilter _meshFilter;

    RenderTexture _groundDepthTexture;

    // TODO set this up
    [SerializeField]
    int _depthRendererIndex = 1;

    [SerializeField]
    float _gravity = 9.81f;
    
#if UNITY_EDITOR
    Vector3 _oldSize;
    [SerializeField, HideInInspector]
    bool _hideCustomComponents = true;
    UnityEngine.Object[] _customComponents;
#endif

    [SerializeField]
    ComputeShader _simulationComputeShader;
    [SerializeField]
    ComputeShader _complexSimulationComputeShader;

    [SerializeField] ComputeShader _manipulationBakeShader;
    ComputeBuffer _manipulationBuffer;
    PackedComputeBuffer<WaterManipulator, Vector4> _manipulators;

    [SerializeField]
    ComputeShader _swapComputeShader;
    [SerializeField] ComputeShader _setToGroundComputeShader;

    [SerializeField] bool _swap = true;
    [SerializeField] bool _step = false;

    [SerializeField] float _deltaTime = 0.005f;
    [SerializeField] float _simulationInterval = 0.5f;

    IWaterSimulator _waterSimulator;
    [SerializeField]
    bool _useSimple = true;

    void Update() {
        if (_step) {
            _step = false;
            Dispatch();
        }
        UpdateManipulationBuffer();
        // Texture2D texture = new Texture2D(256, 256, UnityEngine.Experimental.Rendering.GraphicsFormat.R32G32B32A32_SFloat, UnityEngine.Experimental.Rendering.TextureCreationFlags.DontInitializePixels);

        _meshRenderer.material.SetVector("WaterSimulator_Size", _size);
        _meshRenderer.material.SetVector("WaterSimulator_Resolution", new Vector4(_resolution.x, _resolution.y));
        _meshRenderer.material.SetVector("WaterSimulator_StepSize", new Vector4(_size.x / (_resolution.x - 1), _size.z / (_resolution.y - 1)));
        _meshRenderer.material.SetVector("WaterSimulator_StepSizeInv", new Vector4((_resolution.x - 1) / _size.x, (_resolution.y - 1) / _size.z));
        _meshRenderer.material.SetBuffer("WaterSimulator_Data", _waterSimulator.GetSimulationData());
    }

    IEnumerator Sim() {
        yield return new WaitForSeconds(1f);
        while (true) {
            Dispatch();
            SetToGround();
            if (_swap) {
                SwapAccordingToHeight();
            }
            yield return new WaitForSeconds(_simulationInterval);
        }
    }

    void Dispatch() {
        _waterSimulator.Dispatch(_deltaTime);
    }

    Vector3 Divide(Vector3 A, Vector3 B) {
        return new Vector3(A.x / B.x, A.y / B.y, A.z / B.z);
    }

    void SetToGround() {
        // _meshFilter.mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;
        GraphicsBuffer vertexBuffer = _meshFilter.mesh.GetVertexBuffer(0);
        _setToGroundComputeShader.SetBuffer(0, ShaderIDs.VertexBuffer, vertexBuffer);
        _setToGroundComputeShader.SetBuffer(1, ShaderIDs.VertexBuffer, vertexBuffer);
        vertexBuffer.Dispose();

        _setToGroundComputeShader.SetTexture(0, ShaderIDs.GroundHeight, _groundDepthTexture);
        _setToGroundComputeShader.SetTexture(1, ShaderIDs.GroundHeight, _groundDepthTexture);
        _setToGroundComputeShader.SetBuffer(0, ShaderIDs.Data, _waterSimulator.GetSimulationData());
        _setToGroundComputeShader.SetBuffer(1, ShaderIDs.Data, _waterSimulator.GetSimulationData());
        _setToGroundComputeShader.SetVector("Size", _size);
        _setToGroundComputeShader.SetInts("Resolution", new int[] { _resolution.x, _resolution.y });
        _setToGroundComputeShader.SetFloats("StepSize", new float[] { _size.x / (_resolution.x - 1), _size.z / (_resolution.y - 1) });
        _setToGroundComputeShader.SetFloats("StepSizeInv", new float[] { (_resolution.x - 1) / _size.x, (_resolution.y - 1) / _size.z });

        _setToGroundComputeShader.Dispatch(0, _resolution.x / KERNEL_SIZE, _resolution.y / KERNEL_SIZE, 1);

        _setToGroundComputeShader.Dispatch(1, _resolution.x / KERNEL_SIZE, _resolution.y / KERNEL_SIZE, 1);
    }

    void InitHeight() {
        _waterSimulator = _useSimple ? new SimpleWaterSimulator() : new ComplexWaterSimulator();
        _waterSimulator.Init(_waterSimulator is SimpleWaterSimulator ? _simulationComputeShader : _complexSimulationComputeShader, _size, _resolution, _groundDepthTexture, _manipulationBuffer);
        _waterSimulator.SetGravity(_gravity);
    }

    bool _initSwapShader = true;
    void SwapAccordingToHeight() {
        if (_initSwapShader) {
            _initSwapShader = false;

            if (_meshFilter.mesh.indexFormat == UnityEngine.Rendering.IndexFormat.UInt32) {
                _swapComputeShader.EnableKeyword("INDEX_UINT32");
            }
            else {
                _swapComputeShader.DisableKeyword("INDEX_UINT32");
            }

            // _meshFilter.mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
            GraphicsBuffer buffer = _meshFilter.mesh.GetIndexBuffer();
            _swapComputeShader.SetBuffer(0, ShaderIDs.IndexBuffer, buffer);
            buffer.Dispose();

            GraphicsBuffer vertexBuffer = _meshFilter.mesh.GetVertexBuffer(0);
            _swapComputeShader.SetBuffer(0, ShaderIDs.VertexBuffer, vertexBuffer);
            vertexBuffer.Dispose();

            _swapComputeShader.SetInts("Resolution", new int[] { _resolution.x, _resolution.y });
        }

        _swapComputeShader.Dispatch(0,
            (_resolution.x - 1 + KERNEL_SIZE - 1) / KERNEL_SIZE,
            (_resolution.y - 1 + KERNEL_SIZE - 1) / KERNEL_SIZE,
            1
        );
    }

    void UpdateManipulationBuffer() {
        if (!_manipulators.IsDirty()) {
            return;
        }
        _manipulators.UpdateBuffer();

        _manipulationBakeShader.SetVector("Size", _size);
        _manipulationBakeShader.SetFloats("StepSize", new float[] { _size.x / (_resolution.x - 1), _size.z / (_resolution.y - 1) });
        _manipulationBakeShader.SetInts("Resolution", new int[] { _resolution.x, _resolution.y });
        _manipulationBakeShader.SetInt(ShaderIDs.ManipulatorsCount, _manipulators.Count);
        _manipulationBakeShader.SetBuffer(0, ShaderIDs.Manipulators, _manipulators.Buffer);
        _manipulationBakeShader.SetBuffer(0, ShaderIDs.Manipulation, _manipulationBuffer);
        _manipulationBakeShader.Dispatch(0, _resolution.x / KERNEL_SIZE, _resolution.y / KERNEL_SIZE, 1);
    }

    Vector4 ConvertToManipulatorData(Vector3 worldPosition, float radius) {
        // Vector3 localPosition = Divide(transform.InverseTransformPoint(worldPosition), _size) + 0.5f * Vector3.one;
        Vector3 localPosition = transform.InverseTransformPoint(worldPosition) + 0.5f * _size;
        // Debug.Log(localPosition);
        return new Vector4(localPosition.x, localPosition.y, localPosition.z, radius);
    }

    public void UpdateManipulator(WaterManipulator manipulator, Vector3 worldPosition, float radius) {
        _manipulators.SetValue(manipulator, ConvertToManipulatorData(worldPosition, radius));
    }

    public void AddManipulator(WaterManipulator manipulator, Vector3 worldPosition, float radius) {
        _manipulators.Add(manipulator, ConvertToManipulatorData(worldPosition, radius));
    }

    public void RemoveManipulator(WaterManipulator manipulator) {
        _manipulators.Remove(manipulator);
    }

    void Awake() {
        if (!SystemInfo.supportsComputeShaders) {
            Debug.LogError("Compute shaders are not supported");
            enabled = false;
            return;
        }
        _meshRenderer = GetComponent<MeshRenderer>();
        _meshRenderer.bounds = new Bounds(transform.position, _size);

        _meshFilter = GetComponent<MeshFilter>();

        SetupCamera();

        int size = _resolution.x * _resolution.y;

        _manipulationBuffer = new ComputeBuffer(size, 4);
        _manipulationBuffer.SetData(Enumerable.Repeat(-1f, size).ToArray());
        _manipulators = new PackedComputeBuffer<WaterManipulator, Vector4>(8, 4);

        _meshFilter.mesh = CreateMesh(_resolution, _size);
        _meshFilter.mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        _meshFilter.mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;

        SetupGroundDepthTexture();

        InitHeight();

        UpdateManipulationBuffer();

#if UNITY_EDITOR
        _customComponents = new UnityEngine.Object[] { _cameraHolder, _meshFilter };
        SetHideComponents(true);
#endif

        StartCoroutine(Sim());
    }

    void OnDestroy() {
        _groundDepthTexture.Release();
        _groundDepthCamera.targetTexture = null;
        DestroyImmediate(_groundDepthTexture);
        Destroy(_meshFilter.mesh);

        _waterSimulator.Release();
        
        _manipulationBuffer.Release();
    }

    // TODO Should this be inaccessible?
    public void SetBounds(Vector3 center, Vector3 size) {
        if (transform.position == center && _size == size) {
            return;
        }

        transform.position = center;
        _size = size;
        // CalibrateCamera();
    }
    
    public Vector3 GetCenter() {
        return transform.position;        
    }

    public Vector3 GetSize() {
        return _size;
    }

    void SetupCamera() {
        _cameraHolder = new GameObject("WaterSimulator_CameraHolder", typeof(Camera), typeof(URPCameraData));
        _cameraHolder.transform.SetParent(transform, false);
        _cameraHolder.transform.rotation = Quaternion.Euler(Vector3.right * 90f);

        _groundDepthCamera = _cameraHolder.GetComponent<Camera>();
        _groundDepthCamera.nearClipPlane = 0f;
        _groundDepthCamera.orthographic = true;
        _groundDepthCamera.depthTextureMode = DepthTextureMode.Depth;
        _groundDepthCamera.clearFlags = CameraClearFlags.Depth;
        _groundDepthCamera.cullingMask = _groundLayer;
        
        CalibrateCamera();

        var cameraData = _cameraHolder.GetComponent<URPCameraData>();
        cameraData.renderPostProcessing = false;
        cameraData.renderShadows = false;
        // cameraData.SetRenderer(_depthRendererIndex);
    }
    
    void SetupGroundDepthTexture() {
        _groundDepthTexture = new RenderTexture(_resolution.x, _resolution.y, 32, RenderTextureFormat.Depth) {
            filterMode = FilterMode.Point
        };
        _groundDepthTexture.Create();
        _groundDepthCamera.forceIntoRenderTexture = true;
        _groundDepthCamera.targetTexture = _groundDepthTexture;
    }
    
    void CalibrateCamera() {
        Vector3 position = transform.position;
        position.y += 0.5f * _size.y;
        _groundDepthCamera.transform.position = position;
        float dimZ = _size.z * (1f + 1f / _resolution.y);
        float dimX = _size.x * (1f + 1f / _resolution.x);
        _groundDepthCamera.orthographicSize = 0.5f * dimZ;
        _groundDepthCamera.aspect = dimX / dimZ;
        _groundDepthCamera.farClipPlane = _size.y;
    }
    
    [SerializeField]
    bool _indexFormat32 = true;

    Mesh CreateMesh() {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] {
            new Vector3(0,0,0),
            new Vector3(1,0,0),
            new Vector3(0,0,1),
            new Vector3(1,0,1),
            new Vector3(0,-1,0),
        };
        if (_indexFormat32) {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
            _swapComputeShader.EnableKeyword("INDEX_UINT32");
        }
        else {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt16;
            _swapComputeShader.DisableKeyword("INDEX_UINT32");
        }
        mesh.triangles = new int[] {0, 2, 1, 1, 2, 3};
        // mesh.triangles = new int[] {4,4,4,4,4,4};
        return mesh;
    }
    
    public static Mesh CreateMesh(Vector2Int resolution, Vector3 scale) {
        Mesh mesh = new Mesh();
        Vector3[] vertices = new Vector3[resolution.x * resolution.y];
        Vector2[] uvs = new Vector2[resolution.x * resolution.y];
        int[] indices = new int[(resolution.x - 1) * (resolution.y - 1) * 2 * 3];

        if (vertices.Length > 1 << 16) {
            mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        }

        int i = 0;
        float xOffset = 0.5f / resolution.x;
        float yOffset = 0.5f / resolution.y;
        for (int y = 0; y < resolution.y; y++) {
            for (int x = 0; x < resolution.x; x++) {
                float xc = (float)x / resolution.x + xOffset;
                float yc = (float)y / resolution.y + yOffset;
                vertices[i] = new Vector3(xc, 0f, yc) - new Vector3(0.5f, 0f, 0.5f);
                vertices[i].Scale(scale);
                uvs[i] = new Vector2(xc, yc);
                i++;
            }
        }
        i = 0;
        int index = 0;
        while (i < indices.Length)
        {
            // clockwise
            indices[i++] = index;
            indices[i++] = index + resolution.x;
            indices[i++] = index + 1;

            indices[i++] = index + 1;
            indices[i++] = index + resolution.x;
            indices[i++] = index + resolution.x + 1;

            index++;
            if (index % resolution.x == resolution.x - 1)
                index++;
        }

        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = indices;
        mesh.RecalculateNormals();
        return mesh;
    }

#if UNITY_EDITOR
    void SetHideComponents(bool hide) {
        _hideCustomComponents = hide;
        HideFlags flags = hide
            ? HideFlags.HideInInspector | HideFlags.HideInHierarchy
            : HideFlags.None;
        // TODO MeshFilter doesn't always respect hideflags
        foreach (var component in _customComponents) {
            component.hideFlags = flags;
        }
    }

    [ContextMenu("Toggle Components Visibility")]
    void ToggleComponentsVisibility() {
        SetHideComponents(!_hideCustomComponents);
    }

    void OnDrawGizmosSelected() {
        Gizmos.color = GIZMO_COLOR;
        Gizmos.DrawWireCube(GetCenter(), GetSize());
    }

    void OnValidate() {
        _waterSimulator?.SetGravity(_gravity);
        // if (_size != _oldSize) {
        //     SetBounds(GetCenter(), _size);
        //     _oldSize = _size;
        // }

        // _groundDepthCamera.cullingMask = _groundLayer;
    }
#endif

    // TODO should I do it like this or differently
    void SupplySize(ComputeShader shader) {
        shader.SetVector(ShaderIDs.Size, _size);
    }
    void SupplyResolution(ComputeShader shader) {
        shader.SetInts(ShaderIDs.Resolution, new int[] { _resolution.x, _resolution.y });
    }
    void SupplyStepSize(ComputeShader shader) {
        shader.SetFloats(ShaderIDs.StepSize, new float[] { _size.x / (_resolution.x - 1), _size.z / (_resolution.y - 1) });
    }
    void SupplyStepSizeInv(ComputeShader shader) {
        shader.SetFloats(ShaderIDs.StepSizeInv, new float[] { (_resolution.x - 1) / _size.x, (_resolution.y - 1) / _size.z });
    }
}
