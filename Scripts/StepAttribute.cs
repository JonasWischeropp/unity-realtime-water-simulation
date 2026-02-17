using UnityEngine;
using UnityEditor;
using System;

namespace JonasWischeropp.Unity.WaterSimulation {

public class StepAttribute : PropertyAttribute {
    readonly int _stepSize;
    readonly int _min;
    readonly int _max;

    public StepAttribute(int stepSize) {
        _stepSize = stepSize;
        _min = int.MinValue;
        _max = int.MaxValue;
    }
    public StepAttribute(int stepSize, int min) {
        _stepSize = stepSize;
        _min = RoundUp(min, min % stepSize);
        _max = int.MaxValue;
    }
    public StepAttribute(int stepSize, int min, int max) {
        _stepSize = stepSize;
        _min = RoundUp(min, min % stepSize);
        _max = RoundDown(max, max % stepSize);
    }
    
    int RoundUp(int x, int mod) {
        return x - mod + Math.Sign(mod) * _stepSize;
    }

    int RoundDown(int x, int mod) {
        return x - mod;
    }

    public int CorrectValue(int x) {
        int halfStepSize = _stepSize / 2;
        x = Math.Min(_max, Math.Max(_min, x));
        int mod = x % _stepSize;
        if (mod == 0)
            return x;
        else if (Math.Abs(mod) < halfStepSize)
            return RoundUp(x, mod);
        else
            return RoundDown(x, mod);
    }
}

namespace Editor {
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(StepAttribute))]
public class StepDrawer : PropertyDrawer {
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label) {
        StepAttribute step = (StepAttribute)attribute;
        EditorGUI.BeginChangeCheck();
        EditorGUI.PropertyField(position, property, label);
        if (EditorGUI.EndChangeCheck()) {
            switch (property.propertyType) {
                case SerializedPropertyType.Integer:
                    property.intValue = step.CorrectValue(property.intValue);
                    break;
                case SerializedPropertyType.Vector2Int:
                    var vec2 = property.vector2IntValue;
                    property.vector2IntValue = new Vector2Int(
                        step.CorrectValue(vec2.x),
                        step.CorrectValue(vec2.y)
                    );
                    break;
                case SerializedPropertyType.Vector3Int:
                    var vec3 = property.vector3IntValue;
                    property.vector3IntValue = new Vector3Int(
                        step.CorrectValue(vec3.x),
                        step.CorrectValue(vec3.y),
                        step.CorrectValue(vec3.z)
                    );
                    break;
                default:
                    Debug.LogError("Step Attribute can only be applied to int, Vector2Int and Vector3Int");
                    break;
            }
        }
    }
}
#endif
} // namespace Editor

} // namespace JonasWischeropp.Unity.WaterSimulation
