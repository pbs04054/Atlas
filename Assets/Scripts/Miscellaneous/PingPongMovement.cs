using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PingPongMovement : MonoBehaviour {

    Vector3 oriPositon;
    [SerializeField]
    float shakeAmount;
    void Awake()
    {
        oriPositon = transform.position;
    }

    void Update () {
        transform.position = oriPositon + Vector3.up * (Mathf.PingPong(Time.time * 0.1f, 0.5f) - 0.25f);
        DynamicGI.SetEmissive(GetComponent<MeshRenderer>(), Color.blue * Mathf.PingPong(Time.time, 10));
	}
}
