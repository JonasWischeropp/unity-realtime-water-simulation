using UnityEngine;

namespace JonasWischeropp.Unity.WaterSimulation {

public static class ShaderIDs {
    public static readonly int Source = Shader.PropertyToID("Source");
    public static readonly int Target = Shader.PropertyToID("Target");

    public static readonly int Data = Shader.PropertyToID("Data");
    public static readonly int GroundHeight = Shader.PropertyToID("GroundHeight");
    public static readonly int Cells = Shader.PropertyToID("Cells");

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

    public static readonly int Gravity = Shader.PropertyToID("Gravity");
    public static readonly int DeltaTime = Shader.PropertyToID("DeltaTime");
    public static readonly int ManningRoughness = Shader.PropertyToID("ManningRoughness");
    public static readonly int MaxVelocity = Shader.PropertyToID("MaxVelocity");
    public static readonly int FoamDissipation = Shader.PropertyToID("FoamDissipation");
    public static readonly int FoamAirTrapMul = Shader.PropertyToID("FoamAirTrapMul");
    public static readonly int FoamSteepMul = Shader.PropertyToID("FoamSteepMul");
    public static readonly int FoamVanishing = Shader.PropertyToID("FoamVanishing");
}

public static class MaterialIDs {
    static string Prefix(string s) => $"JW_WaterSimulator_{s}";
    static int PropertyWithPrefixToID(string s) => Shader.PropertyToID(Prefix(s));

    public static readonly int Data = PropertyWithPrefixToID("Data");
    public static readonly int Size = PropertyWithPrefixToID("Size");
    public static readonly int StepSize = PropertyWithPrefixToID("StepSize");
    public static readonly int StepSizeInv = PropertyWithPrefixToID("StepSizeInv");
    public static readonly int Resolution = PropertyWithPrefixToID("Resolution");
}

} // namespace JonasWischeropp.Unity.WaterSimulation
