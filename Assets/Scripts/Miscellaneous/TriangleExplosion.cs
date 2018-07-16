using UnityEngine;
 using System.Collections;
 using System.Collections.Generic;
using UnityEngine.Rendering;


public class TriangleExplosion : MonoBehaviour
{

    public IEnumerator SplitMesh()
    {
        if (GetComponent<MeshFilter>() == null || GetComponent<SkinnedMeshRenderer>() == null) yield return null;

        if (GetComponent<Collider>()) GetComponent<Collider>().enabled = false;

        Mesh originMesh = new Mesh();
        if (GetComponent<MeshFilter>())
            originMesh = GetComponent<MeshFilter>().mesh;
        else if (GetComponent<SkinnedMeshRenderer>())
            originMesh = GetComponent<SkinnedMeshRenderer>().sharedMesh;

        Material[] materials = new Material[0];
        if (GetComponent<MeshRenderer>())
            materials = GetComponent<MeshRenderer>().materials;
        else if (GetComponent<SkinnedMeshRenderer>())
            materials = GetComponent<SkinnedMeshRenderer>().materials;

        Vector3[] verts = originMesh.vertices;
        Vector3[] normals = originMesh.normals;
        Vector2[] uvs = originMesh.uv;
        for (int submesh = 0; submesh < originMesh.subMeshCount; submesh++)
        {
            int[] indices = originMesh.GetTriangles(submesh);
            for (int i = 0; i < indices.Length; i += 3)
            {
                Vector3[] newVerts = new Vector3[3];
                Vector3[] newNormals = new Vector3[3];
                Vector2[] newUvs = new Vector2[3];
                for (int n = 0; n < 3; n++)
                {
                    int index = indices[i + n];
                    newVerts[n] = verts[index];
                    newUvs[n] = uvs[index];
                    newNormals[n] = normals[index];
                }

                Mesh mesh = new Mesh
                {
                    vertices = newVerts,
                    normals = newNormals,
                    uv = newUvs,
                    triangles = new int[] {0, 1, 2, 2, 1, 0}
                };
                
                mesh.RecalculateNormals();
                mesh.RecalculateBounds();
                
                GameObject triangle = new GameObject("Triangle " + i / 3);
                triangle.layer = LayerMask.NameToLayer("Ragdoll");
                triangle.transform.position = transform.position;
                triangle.transform.rotation = transform.rotation;
                MeshRenderer meshRenderer = triangle.AddComponent<MeshRenderer>();
                meshRenderer.material = materials[submesh];
                meshRenderer.shadowCastingMode = ShadowCastingMode.Off;
                meshRenderer.receiveShadows = false;
                triangle.AddComponent<MeshFilter>().mesh = mesh;
                triangle.AddComponent<BoxCollider>();
                Vector3 explosionPos = transform.forward * -3f + transform.position;
                triangle.AddComponent<Rigidbody>().AddExplosionForce(Random.Range(800, 1500), explosionPos, 5);
                Destroy(triangle, 3f);
            }
        }
    }
}