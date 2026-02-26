using System;
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
        GameObject go = SpawnGameObject("Simulator", typeof(Simulator), typeof(Sampler));
        go.layer = LayerMask.NameToLayer("Water");
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Not using AssetDatabase.LoadAssetByGUID because it is not supported in old versions.
        string path = AssetDatabase.GUIDToAssetPath(new GUID("b768d0c7e99c8660d83b040a5ceef93c"));
        renderer.material = AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    [MenuItem(PATH + "Manipulator", priority = MENU_PRIORITY + 2)]
    static void CreateManipulator() {
        GameObject go = SpawnGameObject("Manipulator", typeof(Manipulator));
        Manipulator manipulator = go.GetComponent<Manipulator>();
        manipulator.SetSimulator(GetClosestOfType<Simulator>());
        manipulator.enabled = true;
    }

    [MenuItem(PATH + "Floater", priority = MENU_PRIORITY + 3)]
    static void CreateFloater() {
        GameObject go = SpawnGameObject("Floater", typeof(Floater));
        go.GetComponent<Floater>().SetSimulatorSampler(GetClosestOfType<Sampler>());
    }

    static T GetClosestOfType<T>() where T : UnityEngine.Component {
        T[] objects = SceneView.FindObjectsByType<T>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        Vector3 spawnPosition = SpawnPosition();

        float minDistance = float.PositiveInfinity;
        T closest = null;
        foreach (T ob in objects) {
            float distance = (spawnPosition - ob.transform.position).sqrMagnitude;
            if (distance < minDistance) {
                minDistance = distance;
                closest = ob;
            }
        }
        return closest;
    }
}

} // namespace JonasWischeropp.Unity.WaterSimulation.Editor
