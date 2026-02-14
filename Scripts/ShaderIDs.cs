using UnityEngine;

public static class ShaderIDs {
    public static readonly int Source = Shader.PropertyToID("Source");
    public static readonly int Target = Shader.PropertyToID("Target");

    public static readonly int Data = Shader.PropertyToID("Data");
    public static readonly int GroundHeight = Shader.PropertyToID("GroundHeight");

    public static readonly int VertexBuffer = Shader.PropertyToID("VertexBuffer");
    public static readonly int IndexBuffer = Shader.PropertyToID("IndexBuffer");

    public static readonly int Size = Shader.PropertyToID("Size");
    public static readonly int StepSize = Shader.PropertyToID("StepSize");
    public static readonly int StepSizeInv = Shader.PropertyToID("StepSizeInv");
    public static readonly int Resolution = Shader.PropertyToID("Resolution");

    public static readonly int Manipulators = Shader.PropertyToID("Manipulators");
    public static readonly int ManipulatorsCount = Shader.PropertyToID("ManipulatorsCount");
    public static readonly int Manipulation = Shader.PropertyToID("Manipulation");

    public static readonly int Positions = Shader.PropertyToID("Positions");
    public static readonly int SampledData = Shader.PropertyToID("SampledData");
}
