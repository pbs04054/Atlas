using UnityEngine;

public static class LineRendererExtension
{
    public static void DrawArc(this LineRenderer lineRenderer, Transform transform, float radius, float angle)
    {
        Vector3 position = transform.position;
        float theta_scale = 0.05f; //Delta
        int size = (int)((angle / 180f * Mathf.PI) / theta_scale); //Total number of points in circle.

        lineRenderer.material = Resources.Load<Material>("Materials/BulletLine");
        lineRenderer.startWidth = 0.2f;
        lineRenderer.endWidth = 0.2f;
        lineRenderer.positionCount = size + 2;

        int i = 1;
        lineRenderer.SetPosition(0, new Vector3(position.x, 0, position.z));
        for (float theta = (90-angle*0.5f)/180f * Mathf.PI; theta < (90+angle*0.5f)/180f * Mathf.PI && i < size + 1; theta += theta_scale)
        {
            float x = radius * Mathf.Cos(theta);
            float z = radius * Mathf.Sin(theta);

            Vector3 pos = transform.rotation * new Vector3(x, 0, z) + new Vector3(position.x, 0, position.z);
            lineRenderer.SetPosition(i, pos);
            i += 1;
        }
        lineRenderer.SetPosition(i, new Vector3(position.x, 0, position.z));
    }
}
