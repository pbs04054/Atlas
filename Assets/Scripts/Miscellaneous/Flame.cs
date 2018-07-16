using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using System.Linq;

public class Flame : MonoBehaviour
{
    private NetworkInstanceId id;

    public void Init(NetworkInstanceId id, float damage, float radius, float time)
    {
        Debug.Log("Init");
        this.id = id;
        StartCoroutine(FlameUpdator(damage, radius, time));
    }

    IEnumerator FlameUpdator(float damage, float radius, float time)
    {
        float timer = 0;
        while (true)
        {
            if (timer >= time)
                break;

            foreach (Enemy enemy in GetObjectsByCircle<Enemy>(radius))
            {
                 enemy.GetDamaged(damage, id);   
            }

            timer += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
    
    public T[] GetObjectsByCircle<T>(float radius)
    {
        Collider[] cols = Physics.OverlapSphere(transform.position, radius);
        return (from col in cols let t = col.GetComponent<T>() where t != null && col.gameObject != gameObject select t).ToArray();
    }
    
}
