using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackArea : MonoBehaviour
{
    

    Mesh fillMesh, outlineMesh;
    MeshFilter fillMeshFilter, outlineMeshFilter;

    void Awake()
    {
        fillMesh = new Mesh();
        outlineMesh = new Mesh();
        fillMeshFilter = transform.Find("Fill").GetComponent<MeshFilter>();
        outlineMeshFilter = transform.Find("Outline").GetComponent<MeshFilter>();
        fillMeshFilter.mesh = fillMesh;
        outlineMeshFilter.mesh = outlineMesh;
    }
    
    public static Coroutine CreateArc(Transform transform, float radius, float angle, float time)
    {
        AttackArea attackArea = Instantiate(Resources.Load<AttackArea>("Prefabs/AttackArea"), transform);
        return attackArea.StartCoroutine(attackArea.ArcAttackAreaUpdator(transform, radius, angle, time));
    }

    IEnumerator ArcAttackAreaUpdator(Transform transform, float radius, float angle, float time)
    {
        float timer = 0;
        while (true)
        {
            if (timer >= time)
                break;

            timer += Time.deltaTime;
            fillMesh.DrawArc(transform, Mathf.Lerp(0, radius, timer/time), angle);
            outlineMesh.DrawArc(transform, radius, angle);
            yield return null;
        }
        EndUpdator();
    }

    public static Coroutine CreateBox(Transform transform, float legnth, float width, float time)
    {
        AttackArea attackArea = Instantiate(Resources.Load<AttackArea>("Prefabs/AttackArea"), transform);
        return attackArea.StartCoroutine(attackArea.BoxAttackAreaUpdator(legnth, width, time));
    }

    IEnumerator BoxAttackAreaUpdator(float length, float width, float time)
    {
        float timer = 0;
        while (true)
        {
            if (timer >= time)
                break;

            timer += Time.deltaTime;
            fillMesh.DrawPlane(Mathf.Lerp(0, length, timer/time), width);
            outlineMesh.DrawPlane(length, width);
            yield return null;
        }
        EndUpdator();
    }
    
    public static Coroutine CreateCircle(Transform transform, float radius, float time)
    {
        AttackArea attackArea = Instantiate(Resources.Load<AttackArea>("Prefabs/AttackArea"), transform);
        return attackArea.StartCoroutine(attackArea.CircleAttackAreaUpdator(radius, time));
    }
    
    public static Coroutine CreateCircle(Vector3 position, float radius, float time)
    {
        AttackArea attackArea = Instantiate(Resources.Load<AttackArea>("Prefabs/AttackArea"), position, Quaternion.identity);
        return attackArea.StartCoroutine(attackArea.CircleAttackAreaUpdator(radius, time));
    }

    IEnumerator CircleAttackAreaUpdator(float radius, float time)
    {
        float timer = 0;
        while (true)
        {
            if (timer >= time)
                break;
            
            timer += Time.deltaTime;
            fillMesh.DrawCircle(Mathf.Lerp(0, radius, timer/time), (int)radius * 100);
            outlineMesh.DrawCircle(radius, 100);
            yield return null;
        }
        EndUpdator();
    }

    void EndUpdator()
    {
        Destroy(gameObject);
    }

}