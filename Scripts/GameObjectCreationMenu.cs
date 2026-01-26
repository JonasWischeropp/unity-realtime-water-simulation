using System;
using UnityEditor;
using UnityEngine;

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
    
    static GameObject SpawnGameObject(string name, Type component) {
        return SpawnGameObject(name, new Type[]{component});
    }

    static GameObject SpawnGameObject(string name, Type[] components) {
        var go = new GameObject(name, components);
        go.transform.position = SpawnPosition();
        Selection.objects = new UnityEngine.Object[]{go};
        return go;
    }

    [MenuItem(PATH + "Simulator", priority = MENU_PRIORITY + 1)]
    static void CreateSimulator() {
        GameObject go = SpawnGameObject("Simulator", typeof(WaterSimulator));
        go.layer = LayerMask.NameToLayer("Water");
        var renderer = go.GetComponent<MeshRenderer>();
        renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;

        // Not using AssetDatabase.LoadAssetByGUID because it is not supported in old versions.
        string path = AssetDatabase.GUIDToAssetPath(new GUID("a0c1e526ebbaf0af2b220c4747114b56")); // TODO replace with correct GUID
        renderer.material = AssetDatabase.LoadAssetAtPath<Material>(path);
    }

    [MenuItem(PATH + "Manipulator", priority = MENU_PRIORITY + 2)]
    static void CreateManipulator() {
        SpawnGameObject("Manipulator", typeof(WaterManipulator));
    }

    [MenuItem(PATH + "Floater", priority = MENU_PRIORITY + 3)]
    static void CreateFloater() {
        SpawnGameObject("Floater", typeof(WaterSimulationFloater));
    }
}
