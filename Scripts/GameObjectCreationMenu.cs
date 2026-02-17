#if UNITY_EDITOR
using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

namespace JonasWischeropp.Unity.WaterSimulation.Editor {

public class GameObjectCreationMenu {
    const string PATH = "GameObject/WaterSimulation/";
    const float MAX_SPAWN_DISTANCE = 100f;
    const int MENU_PRIORITY = 100;

    static Vector3 SpawnPosition() {
        var cameraTransform = SceneView.lastActiveSceneView.camera.transform;
        if (Physics.Raycast(cameraTransform.position, cameraTransform.forward, out var hit, MAX_SPAWN_DISTANCE)) {
            return hit.point;
        }
        return Vector3.zero;
    }
    
    static GameObject SpawnGameObject(string name, params Type[] components) {
        var go = new GameObject(name, components);
        go.transform.position = SpawnPosition();
        Selection.objects = new UnityEngine.Object[]{go};
        Undo.RegisterCreatedObjectUndo(go, $"Create WaterSimulator:{name}");
        return go;
    }

    [MenuItem(PATH + "Simulator", priority = MENU_PRIORITY + 1)]
    static void CreateSimulator() {
        GameObject go = SpawnGameObject("Simulator", typeof(WaterSimulator), typeof(WaterSimulatorSampler));
        go.layer = LayerMask.NameToLayer("Water");
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Not using AssetDatabase.LoadAssetByGUID because it is not supported in old versions.
        string path = AssetDatabase.GUIDToAssetPath(new GUID("b768d0c7e99c8660d83b040a5ceef93c"));
        renderer.material = AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    [MenuItem(PATH + "Manipulator", priority = MENU_PRIORITY + 2)]
    static void CreateManipulator() {
        GameObject go = SpawnGameObject("Manipulator", typeof(WaterManipulator));
        go.GetComponent<WaterManipulator>().SetSimulator(GetClosestOfType<WaterSimulator>());
    }

    [MenuItem(PATH + "Floater", priority = MENU_PRIORITY + 3)]
    static void CreateFloater() {
        GameObject go = SpawnGameObject("Floater", typeof(WaterSimulationFloater));
        go.GetComponent<WaterSimulationFloater>().SetSimulatorSampler(GetClosestOfType<WaterSimulatorSampler>());
    }

    static T GetClosestOfType<T>() where T : UnityEngine.Object {
        T[] objects = SceneView.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Vector3 spawnPosition = SpawnPosition();

        float minDistance = float.PositiveInfinity;
        T closest = null;
        foreach (T ob in objects) {
            float distance = (spawnPosition - ob.GameObject().transform.position).sqrMagnitude;
            if (distance < minDistance) {
                minDistance = distance;
                closest = ob;
            }
        }
        return closest;
    }
}
#endif

} // namespace JonasWischeropp.Unity.WaterSimulation.Editor
