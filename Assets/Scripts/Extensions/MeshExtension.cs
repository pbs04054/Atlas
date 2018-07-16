using System.Collections.Generic;
using UnityEngine;

public static class MeshExtension
{

    public static void DrawArc(this Mesh mesh, Transform transform, float radius, float angle)
    {
        List<Vector3> viewPoints = new List<Vector3>();
        
        float theta_scale = 0.05f; //Delta
        int size = (int)((angle / 180f * Mathf.PI) / theta_scale);
        int index = 1;
        for (float theta = (90-angle*0.5f)/180f * Mathf.PI; theta < (90+angle*0.5f)/180f * Mathf.PI && index < size + 1; theta += theta_scale)
        {
            float x = radius * Mathf.Cos(theta);
            float z = radius * Mathf.Sin(theta);
            viewPoints.Add(transform.rotation * new Vector3(x, 0, z) + transform.position);
            index += 1;
        }


        int vertexCount = viewPoints.Count + 1;
        Vector3[] vertices = new Vector3[vertexCount];
        int[] triangles = new int[(vertexCount - 2) * 3];
        vertices[0] = transform.InverseTransformPoint(transform.position);
        for (int i = 0; i < vertexCount - 1; i++)
        {
            vertices[i + 1] = transform.InverseTransformPoint(viewPoints[i]);

            if (i >= vertexCount - 2) continue;
            triangles[i * 3] = 0;
            triangles[i * 3 + 1] = i + 1;
            triangles[i * 3 + 2] = i + 2;
        }


        mesh.Clear();
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

    public static void DrawCircle(this Mesh mesh, float radius, int segments)
    {
        if(segments < 3) segments = 3;

        float step = (2 * Mathf.PI) / segments; // forward angle
        float tanStep = Mathf.Tan(step);
        float radStep = Mathf.Cos(step);

        float x = radius;
        float y = 0;

        Vector3[] verts = new Vector3[segments + 1];
        Vector2[] uvs = new Vector2[segments + 1];

        verts[0] = new Vector3(0, 0, 0); // center of circle
        uvs[0] = new Vector2(0.5f, 0.5f);
        for(int i = 1; i < (segments + 1); i++)
        {
            float tx = -y;
            float ty = x;
            x += tx * tanStep;
            y += ty * tanStep;
            x *= radStep;
            y *= radStep;
            verts[i] = new Vector3(x, 0, y);
            uvs[i] = new Vector2(0.5f + x / (2 * radius), 0.5f + y / (2 * radius));
        }

        int idx = 1;
        int indices = (segments) * 3;

        int[] tris = new int[indices]; // one triagle for each section (3 verts)
        for(int i = 0; i < (indices); i += 3)
        {
            tris[i + 1] = 0;         //center of circle
            tris[i] = idx;           //next vertex
            if(i >= (indices - 3))
            {
                tris[i + 2] = 1;     // loop on last
            }
            else
            {
                tris[i + 2] = idx + 1; // next vertex	
            }
            idx++;
        }

        mesh.vertices = verts;
        mesh.triangles = tris;
        mesh.uv = uvs;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    public static void DrawPlane(this Mesh mesh, float length, float width)
    {
 
        const int resX = 2; // 2 minimum
        const int resZ = 2;
 
        #region Vertices		
        Vector3[] vertices = new Vector3[ resX * resZ ];
        for(int z = 0; z < resZ; z++)
        {
            // [ -length / 2, length / 2 ]
            float zPos = ((float)z / (resZ - 1)) * length;
            for(int x = 0; x < resX; x++)
            {
                // [ -width / 2, width / 2 ]
                float xPos = ((float)x / (resX - 1) - .5f) * width;
                vertices[ x + z * resX ] = new Vector3( xPos, 0f, zPos );
            }
        }
        #endregion
 
        #region Normales
        Vector3[] normales = new Vector3[ vertices.Length ];
        for( int n = 0; n < normales.Length; n++ )
            normales[n] = Vector3.up;
        #endregion
 
        #region UVs		
        Vector2[] uvs = new Vector2[ vertices.Length ];
        for(int v = 0; v < resZ; v++)
        {
            for(int u = 0; u < resX; u++)
            {
                uvs[ u + v * resX ] = new Vector2( (float)u / (resX - 1), (float)v / (resZ - 1) );
            }
        }
        #endregion
 
        #region Triangles
        int nbFaces = (resX - 1) * (resZ - 1);
        int[] triangles = new int[ nbFaces * 6 ];
        int t = 0;
        for(int face = 0; face < nbFaces; face++ )
        {
            // Retrieve lower left corner from face ind
            int i = face % (resX - 1) + (face / (resZ - 1) * resX);
 
            triangles[t++] = i + resX;
            triangles[t++] = i + 1;
            triangles[t++] = i;
 
            triangles[t++] = i + resX;	
            triangles[t++] = i + resX + 1;
            triangles[t++] = i + 1; 
        }
        #endregion
 
        mesh.vertices = vertices;
        mesh.normals = normales;
        mesh.uv = uvs;
        mesh.triangles = triangles;
 
        mesh.RecalculateBounds();
    }
    
}
