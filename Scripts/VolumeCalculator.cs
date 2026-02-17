using UnityEngine;

public static class VolumeCalculator {
    public static float Volume(SphereCollider collider) {
        return SphereVolume(collider.radius);
    }

    public static float Volume(CapsuleCollider collider) {
        return SphereVolume(collider.radius) + CylinderVolume(collider.radius, collider.height);
    }

    public static float Volume(BoxCollider collider) {
        Vector3 size = collider.size;
        return size.x * size.y * size.z;
    }

    public static float Volume(MeshCollider collider) {
        return Volume(collider.sharedMesh);
    }

    public static float SphereVolume(float radius) {
        return 4f / 3f * Mathf.PI * Sq(radius);
    }

    public static float CircleArea(float radius) {
        return Mathf.PI * Sq(radius);
    }

    public static float CylinderVolume(float radius, float height) {
        return height * CircleArea(radius);
    }

    public static float Volume(Mesh mesh) {
        float volume = 0f;
        Vector3[] vertices = mesh.vertices;
        int[] indices = mesh.triangles;
        for (int i = 0; i < indices.Length; i += 3) {
            Vector3 p1 = vertices[indices[i + 0]];
            Vector3 p2 = vertices[indices[i + 1]];
            Vector3 p3 = vertices[indices[i + 2]];
            volume += SignedVolumeOfTriangle(p1, p2, p3);
        }
        return Mathf.Abs(volume);
    }

    // Source: https://stackoverflow.com/questions/1406029/how-to-calculate-the-volume-of-a-3d-mesh-object-the-surface-of-which-is-made-up
    public static float SignedVolumeOfTriangle(Vector3 p1, Vector3 p2, Vector3 p3) {
        var v321 = p3.x * p2.y * p1.z;
        var v231 = p2.x * p3.y * p1.z;
        var v312 = p3.x * p1.y * p2.z;
        var v132 = p1.x * p3.y * p2.z;
        var v213 = p2.x * p1.y * p3.z;
        var v123 = p1.x * p2.y * p3.z;
        return (-v321 + v231 + v312 - v132 - v213 + v123) / 6.0f;
    }

    static float Sq(float x) {
        return x * x;
    }
}
