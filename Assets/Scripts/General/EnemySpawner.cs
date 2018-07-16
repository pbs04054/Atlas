using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour {

    public int radius;

	public GameObject Spawn(GameObject obj)
    {
        Vector2 rand = Random.insideUnitCircle * radius;
        return Instantiate(obj, transform.position + new Vector3(rand.x, 0, rand.y), transform.rotation);
    }

    void OnDrawGizmos()
    {
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}
