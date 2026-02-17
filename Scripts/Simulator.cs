// TODO refactor code
using System.Collections;
using System.Linq;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

using URPCameraData = UnityEngine.Rendering.Universal.UniversalAdditionalCameraData;

namespace JonasWischeropp.Unity.WaterSimulation {

[AddComponentMenu(SIM_MENU_GROUP + "Water Simulator")]
[RequireComponent(typeof(MeshRenderer), typeof(MeshFilter))]
[DisallowMultipleComponent]
[DefaultExecutionOrder(EXECUTION_ORDER)]
public class Simulator : MonoBehaviour {
    public const int EXECUTION_ORDER = 100;
    public const string SIM_MENU_GROUP = "Water Simulation/";
    public readonly static Color GIZMO_COLOR = Color.green;
    public const int KERNEL_SIZE = 8;

    [SerializeField]
    Vector3 _size = Vector3.one * 10f;

    [SerializeField, Step(KERNEL_SIZE, 2)]
    Vector2Int _resolution = Vector2Int.one * 64;
    [SerializeField]
    LayerMask _groundLayer = 1; // Default layer
    
    Camera _groundDepthCamera;
    GameObject _cameraHolder;
    MeshRenderer _meshRenderer;
    MeshFilter _meshFilter;

    RenderTexture _groundDepthTexture;

    [field: SerializeField]
    public float Gravity {get; private set; } = 9.81f;
    
#if UNITY_EDITOR
    [SerializeField, HideInInspector]
    bool _hideCustomComponents = true;
    UnityEngine.Object[] _customComponents;
#endif

    [SerializeField, HideInInspector]
    ComputeShader _simulationComputeShader;
    [SerializeField, HideInInspector]
    ComputeShader _complexSimulationComputeShader;

    [SerializeField, HideInInspector]
    ComputeShader _manipulatorBakerShader;
    ComputeBuffer _manipulationBuffer;
    PackedComputeBuffer<Manipulator, Vector4> _manipulators;

    [SerializeField, HideInInspector]
    ComputeShader _quadDiagonalSwapperShader;
    [SerializeField, HideInInspector] 
    ComputeShader _vertexAdjusterShader;

    [SerializeField]
    bool _postProcessMesh = true;

    IWaterSimulator _waterSimulator;
    [SerializeField]
    bool _useSimple = false;

    [SerializeField]
    int _updatesPerFixedUpdate = 2;
    [SerializeField]
    float _shaderTimeStep = 0.02f;
    bool _simulate = false;

    MaterialPropertyBlock _materialPropertyBlock;

    void Update() {
        if (!_simulate) {
            return;
        }

        _materialPropertyBlock.SetBuffer("WaterSimulator_Data", _waterSimulator.GetSimulationData());
        _meshRenderer.SetPropertyBlock(_materialPropertyBlock);

        UpdateManipulationBuffer();

        if (_postProcessMesh) {
            DispatchVertexAdjuster();
            DispatchQuadDiagonalSwapper();
        }
    }

    IEnumerator EnableSimulationNextFrame() {
        yield return null;
        _simulate = true;
    }

    void FixedUpdate() {
        if (!_simulate) {
            return;
        }

        for (int i = 0; i < _updatesPerFixedUpdate; i++) {
            Dispatch();
        }
    }

    void Dispatch() {
        _waterSimulator.Dispatch(_shaderTimeStep);
    }

    Vector3 Divide(Vector3 A, Vector3 B) {
        return new Vector3(A.x / B.x, A.y / B.y, A.z / B.z);
    }

    void InitVertexAdjuster() {
        ComputeShader shader = _vertexAdjusterShader;
        Debug.Assert((_meshFilter.mesh.vertexBufferTarget | GraphicsBuffer.Target.Structured) != 0);
        GraphicsBuffer vertexBuffer = _meshFilter.mesh.GetVertexBuffer(0);
        shader.SetBuffer(0, ShaderIDs.VertexBuffer, vertexBuffer);
        shader.SetBuffer(1, ShaderIDs.VertexBuffer, vertexBuffer);
        vertexBuffer.Dispose();

        shader.SetTexture(0, ShaderIDs.GroundHeight, _groundDepthTexture);
        shader.SetTexture(1, ShaderIDs.GroundHeight, _groundDepthTexture);

        SetShaderSimSize(shader);
        SetShaderSimResolution(shader);
        SetShaderSimStepSize(shader);
        SetShaderSimStepSizeInv(shader);
    }

    void DispatchVertexAdjuster() {
        ComputeShader shader = _vertexAdjusterShader;
        shader.SetBuffer(0, ShaderIDs.Data, _waterSimulator.GetSimulationData());
        shader.SetBuffer(1, ShaderIDs.Data, _waterSimulator.GetSimulationData());

        shader.Dispatch(0, _resolution.x / KERNEL_SIZE, _resolution.y / KERNEL_SIZE, 1);
        shader.Dispatch(1, _resolution.x / KERNEL_SIZE, _resolution.y / KERNEL_SIZE, 1);
    }

    public void SetShaderSimSize(ComputeShader shader) {
        shader.SetVector(ShaderIDs.Size, _size);
    }
    public void SetShaderSimResolution(ComputeShader shader) {
        shader.SetInts(ShaderIDs.Resolution, new int[] { _resolution.x, _resolution.y });
    }
    public void SetShaderSimStepSize(ComputeShader shader) {
        shader.SetFloats(ShaderIDs.StepSize, new float[] { _size.x / (_resolution.x - 1), _size.z / (_resolution.y - 1) });
    }
    public void SetShaderSimStepSizeInv(ComputeShader shader) {
        shader.SetFloats(ShaderIDs.StepSizeInv, new float[] { (_resolution.x - 1) / _size.x, (_resolution.y - 1) / _size.z });
    }

    public ComputeBuffer GetSimulationData() {
        return _waterSimulator.GetSimulationData();
    }

    public RenderTexture GetGroundTexture() {
        return _groundDepthTexture;
    }

    void InitMaterial() {
        _materialPropertyBlock = new MaterialPropertyBlock();
        _materialPropertyBlock.SetVector("WaterSimulator_Size", _size);
        _materialPropertyBlock.SetVector("WaterSimulator_Resolution", new Vector4(_resolution.x, _resolution.y));
        _materialPropertyBlock.SetVector("WaterSimulator_StepSize", new Vector4(_size.x / (_resolution.x - 1), _size.z / (_resolution.y - 1)));
        _materialPropertyBlock.SetVector("WaterSimulator_StepSizeInv", new Vector4((_resolution.x - 1) / _size.x, (_resolution.y - 1) / _size.z));
        _materialPropertyBlock.SetBuffer("WaterSimulator_Data", _waterSimulator.GetSimulationData());
        _meshRenderer.SetPropertyBlock(_materialPropertyBlock);
    }

    void InitHeight() {
        _waterSimulator = _useSimple ? new SimpleWaterSimulator() : new ComplexWaterSimulator();
        _waterSimulator.Init(_waterSimulator is SimpleWaterSimulator ? _simulationComputeShader : _complexSimulationComputeShader, _size, _resolution, _groundDepthTexture, _manipulationBuffer);
        _waterSimulator.SetGravity(Gravity);
    }

    void InitQuadDiagonalSwapper() {
        ComputeShader shader = _quadDiagonalSwapperShader;
        if (_meshFilter.mesh.indexFormat == UnityEngine.Rendering.IndexFormat.UInt32) {
            shader.EnableKeyword("INDEX_UINT32");
        }
        else {
            shader.DisableKeyword("INDEX_UINT32");
        }

        Debug.Assert((_meshFilter.mesh.indexBufferTarget | GraphicsBuffer.Target.Raw) != 0);
        using(GraphicsBuffer indexBuffer = _meshFilter.mesh.GetIndexBuffer()) {
            shader.SetBuffer(0, ShaderIDs.IndexBuffer, indexBuffer);
        }

        using (GraphicsBuffer vertexBuffer = _meshFilter.mesh.GetVertexBuffer(0)) {
            shader.SetBuffer(0, ShaderIDs.VertexBuffer, vertexBuffer);
        }

        SetShaderSimResolution(shader);
    }

    void DispatchQuadDiagonalSwapper() {
        _quadDiagonalSwapperShader.Dispatch(0,
            (_resolution.x - 1 + KERNEL_SIZE - 1) / KERNEL_SIZE,
            (_resolution.y - 1 + KERNEL_SIZE - 1) / KERNEL_SIZE,
            1
        );
    }

    void InitManipulatorBaker() {
        ComputeShader shader = _manipulatorBakerShader;
        SetShaderSimResolution(shader);
        SetShaderSimSize(shader);
        SetShaderSimStepSize(shader);
    }

    void DispatchManipulatorBaker() {
        ComputeShader shader = _manipulatorBakerShader;

        shader.SetInt(ShaderIDs.ManipulatorsCount, _manipulators.Count);
        shader.SetBuffer(0, ShaderIDs.Manipulators, _manipulators.Buffer);
        shader.SetBuffer(0, ShaderIDs.Manipulation, _manipulationBuffer);

        shader.Dispatch(0, _resolution.x / KERNEL_SIZE, _resolution.y / KERNEL_SIZE, 1);
    }

    void UpdateManipulationBuffer() {
        if (!_manipulators.IsDirty()) {
            return;
        }
        _manipulators.UpdateBuffer();
        DispatchManipulatorBaker();
    }

    Vector4 ConvertToManipulatorData(Vector3 worldPosition, float radius) {
        Vector3 localPosition = GlobalToSimulationSpace(worldPosition);
        return new Vector4(localPosition.x, localPosition.y, localPosition.z, radius);
    }

    // SimulationSpace is [(0,0,0), _size]
    public Vector3 GlobalToSimulationSpace(Vector3 worldPosition) {
        return transform.InverseTransformPoint(worldPosition) + 0.5f * _size;
    }

    public void UpdateManipulator(Manipulator manipulator, Vector3 worldPosition, float radius) {
        _manipulators.SetValue(manipulator, ConvertToManipulatorData(worldPosition, radius));
    }

    public void AddManipulator(Manipulator manipulator, Vector3 worldPosition, float radius) {
        _manipulators.Add(manipulator, ConvertToManipulatorData(worldPosition, radius));
    }

    public void RemoveManipulator(Manipulator manipulator) {
        _manipulators.Remove(manipulator);
    }

    void Awake() {
        if (!SystemInfo.supportsComputeShaders) {
            Debug.LogError("Compute shaders are not supported");
            enabled = false;
            return;
        }
#if UNITY_EDITOR
        if ((gameObject.layer & _groundLayer) != 0) {
            Debug.LogError($"The layer of the simulator ({LayerMask.LayerToName(gameObject.layer)}) should not be included in the LayerMask \"{ObjectNames.NicifyVariableName(nameof(_groundLayer))}\"");
        }
#endif

        _meshRenderer = GetComponent<MeshRenderer>();
        _meshRenderer.bounds = new Bounds(transform.position, _size);

        _meshFilter = GetComponent<MeshFilter>();

        SetupCamera();

        int size = _resolution.x * _resolution.y;

        _manipulationBuffer = new ComputeBuffer(size, 4);
        _manipulationBuffer.SetData(Enumerable.Repeat(-1f, size).ToArray());
        _manipulators = new PackedComputeBuffer<Manipulator, Vector4>(8, 4);

        _meshFilter.mesh = CreateMesh(_resolution, _size);
        _meshFilter.mesh.indexBufferTarget |= GraphicsBuffer.Target.Raw;
        _meshFilter.mesh.vertexBufferTarget |= GraphicsBuffer.Target.Structured;

        SetupGroundDepthTexture();

        InitQuadDiagonalSwapper();
        InitVertexAdjuster();
        InitManipulatorBaker();
    
        InitHeight();

        InitMaterial();

        UpdateManipulationBuffer();

#if UNITY_EDITOR
        _customComponents = new UnityEngine.Object[] { _cameraHolder, _meshFilter };
        SetHideComponents(true);
#endif

        StartCoroutine(EnableSimulationNextFrame());
    }

    void OnDestroy() {
        _groundDepthTexture.Release();
        _groundDepthCamera.targetTexture = null;
        DestroyImmediate(_groundDepthTexture);
        Destroy(_meshFilter.mesh);

        _waterSimulator.Release();
        
        _manipulationBuffer.Release();
        _manipulators.Release();
    }

    // TODO Should this be inaccessible?
    public void SetBounds(Vector3 center, Vector3 size) {
        transform.position = center;
        _size = size;
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
        // TODO what if HDRP or build-in is used?
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
        // Increase size of camera to align the center of a pixel with the vertex
        float dimZ = _size.z * (1f + 1f / _resolution.y);
        float dimX = _size.x * (1f + 1f / _resolution.x);
        _groundDepthCamera.orthographicSize = 0.5f * dimZ;
        _groundDepthCamera.aspect = dimX / dimZ;
        _groundDepthCamera.farClipPlane = _size.y;
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
        for (int y = 0; y < resolution.y; y++) {
            for (int x = 0; x < resolution.x; x++) {
                float xc = (float)x / (resolution.x - 1);
                float yc = (float)y / (resolution.y - 1);
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
        // TODO MeshFilter doesn't always respect hideFlags
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
        _waterSimulator?.SetGravity(Gravity);

        if (_groundDepthCamera) {
            _groundDepthCamera.cullingMask = _groundLayer;
        }
    }
#endif
}

} // namespace JonasWischeropp.Unity.WaterSimulation
